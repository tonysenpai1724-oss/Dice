using System;
using System.Collections;
using DG.Tweening;
using Spine;
using UnityEngine;

public class EnemyProjectileAttackPresenter
{
    const string ComboAttackEventName = "Attack";

    readonly MonoBehaviour coroutineHost;
    readonly Func<PlayerController> getPlayer;
    readonly Func<RectTransform> getProjectilePrefab;
    readonly Func<Transform> getProjectileRoot;
    readonly Func<float> getProjectileSpeed;
    readonly Func<Vector3> getProjectileOffset;
    readonly Func<float> getProjectileRotationOffset;
    readonly Action incrementActiveProjectiles;
    readonly Action decrementActiveProjectiles;

    public EnemyProjectileAttackPresenter(
        MonoBehaviour coroutineHost,
        Func<PlayerController> getPlayer,
        Func<RectTransform> getProjectilePrefab,
        Func<Transform> getProjectileRoot,
        Func<float> getProjectileSpeed,
        Func<Vector3> getProjectileOffset,
        Func<float> getProjectileRotationOffset,
        Action incrementActiveProjectiles,
        Action decrementActiveProjectiles)
    {
        this.coroutineHost = coroutineHost;
        this.getPlayer = getPlayer;
        this.getProjectilePrefab = getProjectilePrefab;
        this.getProjectileRoot = getProjectileRoot;
        this.getProjectileSpeed = getProjectileSpeed;
        this.getProjectileOffset = getProjectileOffset;
        this.getProjectileRotationOffset = getProjectileRotationOffset;
        this.incrementActiveProjectiles = incrementActiveProjectiles;
        this.decrementActiveProjectiles = decrementActiveProjectiles;
    }

    public float PlayPlayerAttack(Enemy target, int damage)
    {
        PlayerController player = getPlayer?.Invoke();
        if (target == null || player == null)
            return 0f;

        incrementActiveProjectiles?.Invoke();
        string attackAnimation = player.GetNextAttackAnimation();
        bool useComboAttack = attackAnimation == player.comboAttackAnim;
        TrackEntry attackTrack = player.PlayAnimation(attackAnimation, false);
        float attackDuration = GetTrackDuration(attackTrack);

        if (useComboAttack && attackTrack != null)
        {
            Debug.Log($"[EnemyProjectileAttackPresenter] Playing combo attack animation: {attackAnimation}");
            coroutineHost.StartCoroutine(SpawnProjectileOnAttackEvent(player, attackTrack, target, damage));
        }
        else
        {
            coroutineHost.StartCoroutine(SpawnProjectileDelayed(target, damage));
        }

        if (player.skeletonGraphic != null)
        {
            player.skeletonGraphic.AnimationState.AddAnimation(
                0,
                AnimationNameUtility.ResolveAnimationName(
                    player.skeletonGraphic.Skeleton?.Data?.Animations,
                    player.idleAnim
                ),
                true,
                0
            );
        }

        return attackDuration;
    }

    float GetTrackDuration(TrackEntry trackEntry)
    {
        if (trackEntry == null)
            return 0f;

        float timeScale = Mathf.Max(0.01f, trackEntry.TimeScale);
        return Mathf.Max(0f, trackEntry.AnimationEnd - trackEntry.AnimationStart) / timeScale;
    }

    IEnumerator SpawnProjectileOnAttackEvent(PlayerController player, TrackEntry attackTrack, Enemy target, int damage)
    {
        if (player == null || player.skeletonGraphic == null || player.skeletonGraphic.AnimationState == null)
        {
            Debug.Log("[EnemyProjectileAttackPresenter] Missing skeletonGraphic/AnimationState, fallback spawn");
            SpawnProjectile(target, damage);
            yield break;
        }

        bool eventTriggered = false;

        void OnAttackTrackEvent(TrackEntry trackEntry, Spine.Event spineEvent)
        {
            string eventName = spineEvent != null && spineEvent.Data != null ? spineEvent.Data.Name : "null";
            //  Debug.Log($"[EnemyProjectileAttackPresenter] Attack track event received name={eventName}");

            if (eventTriggered || spineEvent == null || spineEvent.Data == null)
                return;

            if (!string.Equals(spineEvent.Data.Name, ComboAttackEventName, StringComparison.OrdinalIgnoreCase))
                return;

            eventTriggered = true;
            //            Debug.Log($"[EnemyProjectileAttackPresenter] Matched combo event {ComboAttackEventName}, spawning projectile");
            SpawnProjectile(target, damage);
        }

        float fallbackDelay = Mathf.Max(attackTrack.AnimationEnd - attackTrack.AnimationStart, 0.5f) + 0.1f;
        //  Debug.Log($"[EnemyProjectileAttackPresenter] Subscribed attack TrackEntry.Event waiting for {ComboAttackEventName}, fallbackDelay={fallbackDelay:0.###}");
        attackTrack.Event += OnAttackTrackEvent;
        float elapsed = 0f;
        while (!eventTriggered && elapsed < fallbackDelay)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        attackTrack.Event -= OnAttackTrackEvent;

        if (!eventTriggered)
        {
            Debug.Log($"[EnemyProjectileAttackPresenter] Combo event {ComboAttackEventName} not received in time, fallback spawn");
            SpawnProjectile(target, damage);
        }
    }

    IEnumerator SpawnProjectileDelayed(Enemy target, int damage)
    {
        yield return new WaitForSeconds(0.35f);
        SpawnProjectile(target, damage);
    }

    void SpawnProjectile(Enemy target, int damage)
    {
        PlayerController player = getPlayer?.Invoke();
        RectTransform projectilePrefab = getProjectilePrefab?.Invoke();
        Transform projectileRoot = getProjectileRoot?.Invoke();
        float projectileSpeed = getProjectileSpeed != null ? getProjectileSpeed() : 0f;
        Vector3 projectileOffset = getProjectileOffset != null ? getProjectileOffset() : Vector3.zero;
        float projectileRotationOffset = getProjectileRotationOffset != null ? getProjectileRotationOffset() : 0f;

        if (projectilePrefab == null || target == null || player == null)
        {
            decrementActiveProjectiles?.Invoke();
            return;
        }

        RectTransform projectile = UnityEngine.Object.Instantiate(projectilePrefab, projectileRoot);
        RectTransform playerRect = player.GetComponent<RectTransform>();
        RectTransform targetRect = target.GetComponent<RectTransform>();

        projectile.position = playerRect.position + projectileOffset;

        Vector3 targetPos = targetRect.position;
        targetPos.y += projectileRotationOffset;
        Vector3 dir = targetPos - projectile.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        projectile.rotation = Quaternion.Euler(0, 0, angle - 90f);
        projectile.DOMove(targetPos, Mathf.Max(1f, projectileSpeed))
            .SetSpeedBased(true)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                if (target != null)
                    target.OnTakeDamage(damage);

                UnityEngine.Object.Destroy(projectile.gameObject);
                decrementActiveProjectiles?.Invoke();
            });
    }
}
