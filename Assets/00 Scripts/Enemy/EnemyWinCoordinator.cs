using System.Collections;
using UnityEngine;

public class EnemyWinCoordinator
{
    readonly EnemyManager enemyManager;
    readonly System.Action cleanupEnemies;
    readonly System.Func<bool> hasRemainingEnemies;
    readonly System.Func<bool> tryAdvanceToNextWave;
    readonly System.Action endGameAsWin;

    Coroutine pendingCheckCoroutine;

    public EnemyWinCoordinator(
        EnemyManager enemyManager,
        System.Action cleanupEnemies,
        System.Func<bool> hasRemainingEnemies,
        System.Func<bool> tryAdvanceToNextWave,
        System.Action endGameAsWin)
    {
        this.enemyManager = enemyManager;
        this.cleanupEnemies = cleanupEnemies;
        this.hasRemainingEnemies = hasRemainingEnemies;
        this.tryAdvanceToNextWave = tryAdvanceToNextWave;
        this.endGameAsWin = endGameAsWin;
    }

    public void CheckWinGame()
    {
        cleanupEnemies?.Invoke();

        if (hasRemainingEnemies() || GameplayManager.Instance == null || GameplayManager.Instance.IsGameEnded)
            return;

        DiceQueueUI queueUI = DiceManager.Instance != null && DiceManager.Instance.diceQueueUI != null
            ? DiceManager.Instance.diceQueueUI
            : DiceQueueUI.Instance;
        if (queueUI != null && queueUI.IsBusy)
        {
            queueUI.RequestFastFlush();
            return;
        }

        DiceQueueManager queue = DiceManager.Instance != null ? DiceManager.Instance.diceQueue : null;
        if (queue != null && queue.IsBusy)
        {
            queue.RequestFastFlush();
            return;
        }

        if (tryAdvanceToNextWave())
            return;

        endGameAsWin?.Invoke();
    }

    public void RequestDeferredWinCheck()
    {
        if (pendingCheckCoroutine != null)
            return;

        pendingCheckCoroutine = enemyManager.StartCoroutine(DeferredWinCheckRoutine());
    }

    IEnumerator DeferredWinCheckRoutine()
    {
        yield return null;
        yield return null;
        pendingCheckCoroutine = null;
        CheckWinGame();
    }
}