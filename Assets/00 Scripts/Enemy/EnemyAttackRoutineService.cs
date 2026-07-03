using System.Collections;
using System;

public class EnemyAttackRoutineService
{
    readonly Func<PlayerController> getPlayer;

    public EnemyAttackRoutineService(Func<PlayerController> getPlayer)
    {
        this.getPlayer = getPlayer;
    }

    public IEnumerator AttackPlayerRoutine(Enemy enemy)
    {
        PlayerController player = getPlayer?.Invoke();
        if (player == null || enemy == null)
            yield break;

        bool attackCompleted = false;
        Spine.TrackEntry attackTrack = enemy.PlayAnimation(enemy.attackAnim, false);

        if (attackTrack == null)
            yield break;

        attackTrack.TimeScale = UnityEngine.Mathf.Max(0.1f, enemy.enemyAttackAnimSpeed);
        attackTrack.Complete += _ => attackCompleted = true;

        float duration = attackTrack.Animation.Duration / attackTrack.TimeScale;
        float halfTime = duration * 0.5f;
        float timer = 0f;

        while (enemy != null && enemy.IsAlive())
        {
            timer += UnityEngine.Time.deltaTime;

            if (!attackCompleted && (attackTrack.TrackTime >= halfTime || timer >= halfTime))
            {
                attackCompleted = true;
                int finalDamage = CombatSystem.ApplyDefenseToPlayer(player, enemy.damage);
                player.OnTakeDamage(finalDamage);
            }

            if (attackTrack.IsComplete || timer >= duration)
                break;

            yield return null;
        }

        if (enemy != null && enemy.IsAlive() && enemy.skeletonGraphic != null)
        {
            enemy.skeletonGraphic.AnimationState.AddAnimation(
                0,
                AnimationNameUtility.ResolveAnimationName(
                    enemy.skeletonGraphic.Skeleton?.Data?.Animations,
                    enemy.idleAnim
                ),
                true,
                0
            );
        }
    }
}