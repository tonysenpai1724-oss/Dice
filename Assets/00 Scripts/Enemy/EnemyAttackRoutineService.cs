using System.Collections;
using System;
using Spine;

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

        bool damageApplied = false;
        Spine.TrackEntry attackTrack = enemy.PlayAnimation(enemy.attackAnim, false);

        if (attackTrack == null)
            yield break;

        attackTrack.TimeScale = UnityEngine.Mathf.Max(0.1f, enemy.enemyAttackAnimSpeed);
        bool resolved = false;

        void ApplyAttackDamage()
        {
            if (damageApplied || enemy == null || !enemy.IsAlive() || player == null)
                return;

            damageApplied = true;
            int attackDamage = enemy.damage;
            ExhaustEffect exhaustEffect = enemy.effectManager?.GetEffect<ExhaustEffect>();
            if (exhaustEffect != null)
                attackDamage = exhaustEffect.ApplyToDamage(attackDamage);

            int finalDamage = CombatSystem.ApplyDefenseToPlayer(player, attackDamage);
            player.OnTakeDamage(finalDamage);
        }

        void ResolveAttack()
        {
            if (resolved)
                return;

            resolved = true;

            if (!damageApplied)
                ApplyAttackDamage();
        }

        void OnAttackEvent(TrackEntry trackEntry, Event spineEvent)
        {
            if (damageApplied || !SpineEventUtility.IsAttackEvent(spineEvent))
                return;

            ApplyAttackDamage();
        }

        void OnTrackFinished(TrackEntry trackEntry)
        {
            ResolveAttack();
        }

        attackTrack.Event += OnAttackEvent;
        attackTrack.End += OnTrackFinished;
        attackTrack.Interrupt += OnTrackFinished;
        attackTrack.Dispose += OnTrackFinished;

        float duration = SpineEventUtility.GetTrackDuration(attackTrack);
        float timer = 0f;

        while (enemy != null && enemy.IsAlive() && !resolved)
        {
            timer += UnityEngine.Time.deltaTime;

            if (attackTrack.IsComplete || timer >= duration)
                ResolveAttack();

            yield return null;
        }

        attackTrack.Event -= OnAttackEvent;
        attackTrack.End -= OnTrackFinished;
        attackTrack.Interrupt -= OnTrackFinished;
        attackTrack.Dispose -= OnTrackFinished;

        if (!resolved)
            ResolveAttack();

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
