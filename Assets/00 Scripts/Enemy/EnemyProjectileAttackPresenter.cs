using System;
using System.Collections;
using DG.Tweening;
using Spine;
using UnityEngine;

public class EnemyProjectileAttackPresenter
{
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

    public float PlayPlayerAttack(Enemy target, int damage, bool isCritical = false)
    {
        PlayerController player = getPlayer?.Invoke();
        if (target == null || player == null)
            return 0f;

        incrementActiveProjectiles?.Invoke();
        string attackAnimation = player.GetNextAttackAnimation();
        TrackEntry attackTrack = player.PlayAnimation(attackAnimation, false);

        if (attackTrack != null)
            attackTrack.TimeScale = Mathf.Max(0.1f, player.aimAttackSpeed);

        float attackDuration = SpineEventUtility.GetTrackDuration(attackTrack);

        if (attackTrack != null)
        {
            coroutineHost.StartCoroutine(SpawnProjectileOnAttackEvent(player, attackTrack, target, damage, isCritical));
        }
        else
        {
            coroutineHost.StartCoroutine(SpawnProjectileDelayed(target, damage, isCritical));
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

    IEnumerator SpawnProjectileOnAttackEvent(PlayerController player, TrackEntry attackTrack, Enemy target, int damage, bool isCritical)
    {
        if (player == null || player.skeletonGraphic == null || player.skeletonGraphic.AnimationState == null)
        {
            Debug.Log("[EnemyProjectileAttackPresenter] Missing skeletonGraphic/AnimationState, fallback spawn");
            SpawnProjectile(target, damage, isCritical);
            yield break;
        }

        bool eventTriggered = false;
        bool resolved = false;

        void Resolve(bool shouldSpawn)
        {
            if (resolved)
                return;

            resolved = true;

            if (shouldSpawn)
                SpawnProjectile(target, damage, isCritical);
            else
                decrementActiveProjectiles?.Invoke();
        }

        void OnAttackTrackEvent(TrackEntry trackEntry, Spine.Event spineEvent)
        {
            if (eventTriggered || spineEvent == null || spineEvent.Data == null)
                return;

            if (!SpineEventUtility.IsAttackEvent(spineEvent))
                return;

            eventTriggered = true;
            Resolve(true);
        }

        void OnTrackFinished(TrackEntry trackEntry)
        {
            Resolve(!eventTriggered);
        }

        float fallbackDelay = Mathf.Max(SpineEventUtility.GetTrackDuration(attackTrack), 0.35f) + 0.1f;
        attackTrack.Event += OnAttackTrackEvent;
        attackTrack.End += OnTrackFinished;
        attackTrack.Interrupt += OnTrackFinished;
        attackTrack.Dispose += OnTrackFinished;
        float elapsed = 0f;
        while (!resolved && elapsed < fallbackDelay)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        attackTrack.Event -= OnAttackTrackEvent;
        attackTrack.End -= OnTrackFinished;
        attackTrack.Interrupt -= OnTrackFinished;
        attackTrack.Dispose -= OnTrackFinished;

        if (!resolved)
            Resolve(!eventTriggered);
    }

    IEnumerator SpawnProjectileDelayed(Enemy target, int damage, bool isCritical)
    {
        yield return new WaitForSeconds(0.35f);
        SpawnProjectile(target, damage, isCritical);
    }

    void SpawnProjectile(Enemy target, int damage, bool isCritical)
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
        bool projectileReleased = false;

        void ReleaseProjectile()
        {
            if (projectileReleased)
                return;

            projectileReleased = true;
            decrementActiveProjectiles?.Invoke();
        }

        projectile.rotation = Quaternion.Euler(0, 0, angle - 90f);
        projectile.DOMove(targetPos, Mathf.Max(1f, projectileSpeed))
            .SetSpeedBased(true)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                if (target != null)
                {
                    target.OnTakeDamage(damage, isCritical);
                    RelicManager.Instance?.NotifyPlayerDealtDamage(player, damage);
                }

                UnityEngine.Object.Destroy(projectile.gameObject);
                ReleaseProjectile();
            })
            .OnKill(ReleaseProjectile);
    }
}
