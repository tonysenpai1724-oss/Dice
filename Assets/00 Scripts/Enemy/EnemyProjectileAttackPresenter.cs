using System;
using System.Collections;
using DG.Tweening;
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

    public void PlayPlayerAttack(Enemy target, int damage)
    {
        PlayerController player = getPlayer?.Invoke();
        if (target == null || player == null)
            return;

        incrementActiveProjectiles?.Invoke();
        player.PlayAnimation(player.attackAnim, false);
        coroutineHost.StartCoroutine(SpawnProjectileDelayed(target, damage));

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