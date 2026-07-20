using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class DiceQueueManager : Singleton<DiceQueueManager>
{
    [Header("Prefab")]
    public DiceQueueItem itemPrefab;

    [Header("Root")]
    public Transform contentRoot;

    [Header("Layout")]
    public float spacing = 0.5f;
    public bool useHorizontalLayout = true;
    public Vector3 horizontalOffset = new Vector3(1.5f, 0f, 0f);
    public Vector3 verticalOffset = new Vector3(0.3f, 0f, 1.5f);
    public Vector3 startPosition =
    new Vector3(
        -18.7f,
        11f,
        -11.5f
    );

    public Vector3 stackOffset =
    new Vector3(
        1.5f,
        0f,
        0f
    );

    [Header("Spawn Fly")]
    public bool flyFromMergePosition = true;
    public float spawnFlyDuration = 0.25f;
    public float spawnFlyArcHeight = 2f;

    public float itemMoveDuration = 0.25f;
    public float shiftMoveDuration = 0.2f;
    public float stepDelay = 0.05f;
    public float delayDestoyTime;

    [Header("Combat")]
    public float multiAttackDelay = 0.45f;
    public float fastFlushItemMoveDuration = 0.03f;
    public float fastFlushShiftMoveDuration = 0.03f;
    public float fastFlushStepDelay;
    public float fastFlushDestroyDelay;

    List<DiceQueueItem> items =
        new();

    bool processing;
    struct PendingQueueItem
    {
        public DiceData data;
        public Vector3 spawnPosition;
        public bool hasSpawnPosition;
    }

    List<PendingQueueItem> pendingItems =
        new();
    bool flushingPendingItems;
    bool fastFlushRequested;

    public bool IsBusy => processing || flushingPendingItems || items.Count > 0 || pendingItems.Count > 0;

    public int GetQueuedAttackDamage(PlayerController player)
    {
        int totalDamage = 0;

        for (int i = 0; i < items.Count; i++)
        {
            DiceData diceData = items[i] != null ? items[i].data : null;
            totalDamage += GetDiceAttackDamage(diceData, player);
        }

        for (int i = 0; i < pendingItems.Count; i++)
        {
            totalDamage += GetDiceAttackDamage(pendingItems[i].data, player);
        }

        return totalDamage;
    }

    int GetDiceAttackDamage(DiceData diceData, PlayerController player)
    {
        if (diceData == null || !CanDiceDealAttackDamage(diceData))
            return 0;

        return Mathf.Max(0, diceData.damage);
    }

    bool CanDiceDealAttackDamage(DiceData diceData)
    {
        switch (diceData.type)
        {
            case DiceType.Poison:
            case DiceType.Heal:
            case DiceType.Coin:
                return false;
            default:
                return true;
        }
    }
    public void RequestFastFlush()
    {
        fastFlushRequested = true;
    }

    public void AddDice(DiceData data)
    {
        AddDice(data, Vector3.zero, false);
    }

    public void AddDice(DiceData data, Vector3 spawnPosition)
    {
        AddDice(data, spawnPosition, true);
    }

    void AddDice(DiceData data, Vector3 spawnPosition, bool hasSpawnPosition)
    {
        if (data == null)
            return;

        pendingItems.Add(
            new PendingQueueItem
            {
                data = data,
                spawnPosition = spawnPosition,
                hasSpawnPosition = hasSpawnPosition
            }
        );

        if (!flushingPendingItems)
        {
            flushingPendingItems = true;

            StartCoroutine(
                FlushPendingItems()
            );
        }
    }

    public IEnumerator ProcessQueue()
    {
        if (processing)
            yield break;

        processing = true;

        while (true)
        {
            if (items.Count == 0)
            {
                if (pendingItems.Count > 0)
                {
                    yield return null;
                    continue;
                }

                break;
            }

            DiceQueueItem first =
                items[0];

            items.RemoveAt(0);

            if (first != null)
            {
                DiceData diceData = first.data;
                EnemyManager enemyManager = EnemyManager.Instance;
                PlayerController player =
                    enemyManager != null
                        ? enemyManager.player
                        : null;
                Enemy targetEnemy =
                    enemyManager != null
                        ? enemyManager.GetNearestAliveEnemy()
                        : null;

                GameplayManager gameplay = GameplayManager.Instance;
                // if (gameplay == null)
                // {
                //     gameplay = FindAnyObjectByType<GameplayManager>();
                // }
                gameplay?.BeginDiceSkill(
                    diceData,
                    this,
                    null,
                    enemyManager,
                    player,
                    targetEnemy
                );

                if (diceData == null)
                    gameplay?.CancelAttack();

                diceData?.ExecuteSkill();

                yield return MoveItem(
                    first.transform,
                    GetConsumeTargetPosition(first.transform),
                    GetItemMoveDuration()
                );
                first.Despawn();

                if (enemyManager != null &&
                    gameplay != null &&
                    !gameplay.skillSkipAttack)
                {
                    int attackCount = gameplay.GetAttackCount();

                    for (int attackIndex = 0; attackIndex < attackCount; attackIndex++)
                    {
                        float attackDuration = enemyManager.PlayerAttack(gameplay.skillDamage);

                        if (fastFlushRequested || !enemyManager.HasAliveEnemies())
                            break;

                        if (attackIndex < attackCount - 1)
                        {
                            float waitDuration = attackDuration > 0f ? attackDuration : multiAttackDelay;
                            if (waitDuration > 0f)
                                yield return new WaitForSeconds(waitDuration);
                        }
                    }
                }

                gameplay?.RunAfterAttackActions();
                gameplay?.ClearDiceSkillState();

            }

            yield return ShiftItems();

            float currentStepDelay = GetStepDelay();
            if (currentStepDelay > 0f)
            {
                yield return new WaitForSeconds(
                    currentStepDelay
                );
            }
        }

        if (EnemyManager.Instance != null && EnemyManager.Instance.HasAliveEnemies())
            yield return EnemyManager.Instance.EnemyTurn();

        processing = false;

        if (EnemyManager.Instance != null)
            EnemyManager.Instance.CheckWinGame();

        fastFlushRequested = false;
    }

    IEnumerator FlushPendingItems()
    {
        while (pendingItems.Count > 0)
        {
            yield return new WaitUntil(
                () =>
                    TurnManager.Instance == null ||
                    !TurnManager.Instance.IsResettingBoard
            );

            PendingQueueItem pendingItem =
                pendingItems[0];

            pendingItems.RemoveAt(0);

            DiceQueueItem item =
                ObjectPooler.Spawn(itemPrefab);

            item.SetDice(pendingItem.data);

            int index =
                items.Count;

            Transform t =
                item.transform;

            Vector3 pos =
                GetPosition(index);

            bool shouldFlyFromSpawn =
                flyFromMergePosition &&
                pendingItem.hasSpawnPosition;

            t.position = shouldFlyFromSpawn
                ? pendingItem.spawnPosition
                : pos;

            t.rotation =
                Quaternion.Euler(
                    8f,
                    -406f,
                    -10f
                );

            t.localScale =
                Vector3.one;

            items.Add(item);

            if (shouldFlyFromSpawn)
            {
                yield return MoveItemWithArc(
                    t,
                    pos,
                    spawnFlyDuration,
                    spawnFlyArcHeight
                );
            }
        }

        flushingPendingItems = false;
    }

    IEnumerator ShiftItems()
    {
        float duration =
            Mathf.Max(
                0.01f,
                GetShiftMoveDuration()
            );

        float timer = 0f;
        int itemCount = items.Count;
        Vector3[] starts =
            new Vector3[itemCount];
        Vector3[] targets =
            new Vector3[itemCount];
        DiceQueueItem[] snapshotItems =
            new DiceQueueItem[itemCount];

        for (int i = 0; i < itemCount; i++)
        {
            DiceQueueItem item = items[i];
            snapshotItems[i] = item;

            if (item == null)
                continue;

            starts[i] =
                item.transform.position;

            targets[i] =
                GetPosition(i);
        }

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer / duration
                );

            for (int i = 0; i < itemCount; i++)
            {
                DiceQueueItem item = snapshotItems[i];
                if (item == null)
                    continue;

                item.transform.position =
                    Vector3.Lerp(
                        starts[i],
                        targets[i],
                        t
                    );
            }

            yield return null;
        }
    }

    IEnumerator MoveItem(
        Transform item,
        Vector3 target,
        float duration
    )
    {
        if (item == null)
            yield break;

        Vector3 start =
            item.position;
        float timer = 0f;
        duration =
            Mathf.Max(
                0.01f,
                duration
            );

        while (timer < duration)
        {
            if (item == null)
                yield break;

            timer += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer / duration
                );

            item.position =
                Vector3.Lerp(
                    start,
                    target,
                    t
                );

            yield return null;
        }
    }

    IEnumerator MoveItemWithArc(
        Transform item,
        Vector3 target,
        float duration,
        float arcHeight
    )
    {
        if (item == null)
            yield break;

        Vector3 start =
            item.position;
        float timer = 0f;
        duration =
            Mathf.Max(
                0.01f,
                duration
            );

        while (timer < duration)
        {
            if (item == null)
                yield break;

            timer += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer / duration
                );

            Vector3 position =
                Vector3.Lerp(
                    start,
                    target,
                    t
                );

            position.y +=
                Mathf.Sin(t * Mathf.PI) * arcHeight;

            item.position = position;

            yield return null;
        }

        item.position = target;
    }

    Vector3 GetPosition(int index)
    {
        Vector3 offset = useHorizontalLayout
            ? horizontalOffset
            : stackOffset != Vector3.zero
                ? stackOffset
                : verticalOffset;

        return startPosition +
            offset * index;
    }

    Vector3 GetConsumeTargetPosition(Transform item)
    {
        if (contentRoot != null)
            return contentRoot.position;

        return item != null
            ? item.position
            : startPosition;
    }

    float GetItemMoveDuration()
    {
        return fastFlushRequested ? fastFlushItemMoveDuration : itemMoveDuration;
    }

    float GetShiftMoveDuration()
    {
        return fastFlushRequested ? fastFlushShiftMoveDuration : shiftMoveDuration;
    }

    float GetStepDelay()
    {
        return fastFlushRequested ? fastFlushStepDelay : stepDelay;
    }

    float GetDestroyDelay()
    {
        return fastFlushRequested ? fastFlushDestroyDelay : delayDestoyTime;
    }
}




