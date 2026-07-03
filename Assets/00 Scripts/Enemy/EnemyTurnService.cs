using System;
using System.Collections;
using System.Collections.Generic;

public class EnemyTurnService
{
    readonly Action cleanupEnemies;
    readonly Action applyPoisonTicks;
    readonly Action checkWinGame;
    readonly Func<bool> hasAliveEnemies;
    readonly Func<bool> tryHandleChestTurn;
    readonly Func<EnemyTurnSkipEffect> getTurnSkipEffect;
    readonly Func<IEnumerator> moveMeleeEnemiesOneGridStep;
    readonly Func<List<Enemy>> getEnemyTurnAttackers;
    readonly Func<Enemy, bool> canMeleeAttack;
    readonly Func<Enemy, IEnumerator> attackPlayerRoutine;
    readonly float enemyActionDelay;

    public EnemyTurnService(
        Action cleanupEnemies,
        Action applyPoisonTicks,
        Action checkWinGame,
        Func<bool> hasAliveEnemies,
        Func<bool> tryHandleChestTurn,
        Func<EnemyTurnSkipEffect> getTurnSkipEffect,
        Func<IEnumerator> moveMeleeEnemiesOneGridStep,
        Func<List<Enemy>> getEnemyTurnAttackers,
        Func<Enemy, bool> canMeleeAttack,
        Func<Enemy, IEnumerator> attackPlayerRoutine,
        float enemyActionDelay)
    {
        this.cleanupEnemies = cleanupEnemies;
        this.applyPoisonTicks = applyPoisonTicks;
        this.checkWinGame = checkWinGame;
        this.hasAliveEnemies = hasAliveEnemies;
        this.tryHandleChestTurn = tryHandleChestTurn;
        this.getTurnSkipEffect = getTurnSkipEffect;
        this.moveMeleeEnemiesOneGridStep = moveMeleeEnemiesOneGridStep;
        this.getEnemyTurnAttackers = getEnemyTurnAttackers;
        this.canMeleeAttack = canMeleeAttack;
        this.attackPlayerRoutine = attackPlayerRoutine;
        this.enemyActionDelay = enemyActionDelay;
    }

    public IEnumerator ExecuteTurn()
    {
        cleanupEnemies?.Invoke();
        applyPoisonTicks?.Invoke();

        checkWinGame?.Invoke();
        if (hasAliveEnemies == null || !hasAliveEnemies())
            yield break;

        if (tryHandleChestTurn != null && tryHandleChestTurn())
            yield break;

        EnemyTurnSkipEffect turnSkipEffect = getTurnSkipEffect?.Invoke();
        if (turnSkipEffect != null && turnSkipEffect.ConsumeTurnSkip())
            yield break;

        if (moveMeleeEnemiesOneGridStep != null)
            yield return moveMeleeEnemiesOneGridStep();

        List<Enemy> attackers = getEnemyTurnAttackers != null
            ? getEnemyTurnAttackers()
            : null;
        if (attackers == null)
            yield break;

        for (int i = 0; i < attackers.Count; i++)
        {
            Enemy attacker = attackers[i];
            if (attacker == null || !attacker.IsAlive())
                continue;

            if (attacker.type == EnemyType.Melee && canMeleeAttack != null && !canMeleeAttack(attacker))
                continue;

            if (attackPlayerRoutine != null)
                yield return attackPlayerRoutine(attacker);

            if (enemyActionDelay > 0f)
                yield return new UnityEngine.WaitForSeconds(enemyActionDelay);
        }
    }
}