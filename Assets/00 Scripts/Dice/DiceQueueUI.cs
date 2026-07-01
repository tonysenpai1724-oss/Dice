using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiceQueueUI : Singleton<DiceQueueUI>
{
    [Header("Prefab")]
    public DiceQueueUIItem itemPrefab;

    [Header("Root")]
    public RectTransform contentRoot;
    public RectTransform consumePoint;
    public Canvas canvas;
    public Camera worldCamera;

    [Header("Preview")]
    public InventoryItem itemPreviewPrefab;
    public ItemPreviewGenerator previewGenerator;

    [Header("Layout")]
    public RectTransform startPoint;
    public RectTransform nextPoint;
    public Vector2 itemSize = new Vector2(100f, 100f);
    public bool useHorizontalLayout = true;
    public Vector2 horizontalOffset = new Vector2(140f, 0f);
    public Vector2 verticalOffset = new Vector2(0f, -140f);
    public Vector2 stackOffset = new Vector2(140f, 0f);
    public Vector2 startAnchoredPosition = new Vector2(-720f, 420f);

    [Header("Spawn Fly")]
    public bool flyFromMergePosition = true;
    public float spawnFlyDuration = 0.25f;
    public float spawnFlyArcHeight = 60f;

    public float itemMoveDuration = 0.25f;
    public float shiftMoveDuration = 0.2f;
    public float stepDelay = 0.05f;
    public float delayDestroyTime;
    public float fastFlushItemMoveDuration = 0.03f;
    public float fastFlushShiftMoveDuration = 0.03f;
    public float fastFlushStepDelay;
    public float fastFlushDestroyDelay;

    readonly List<DiceQueueUIItem> items = new();

    bool processing;
    bool flushingPendingItems;
    bool fastFlushRequested;

    struct PendingQueueItem
    {
        public DiceData data;
        public Vector2 spawnPosition;
        public bool hasSpawnPosition;
    }

    readonly List<PendingQueueItem> pendingItems = new();

    public bool IsBusy => processing || flushingPendingItems || items.Count > 0 || pendingItems.Count > 0;

    public void RequestFastFlush()
    {
        fastFlushRequested = true;
    }

    public void AddDice(DiceData data)
    {
        AddDice(data, Vector2.zero, false);
    }

    public void AddDice(DiceData data, Vector2 spawnPosition)
    {
        AddDice(data, spawnPosition, true);
    }

    public void AddDice(DiceData data, Vector3 worldSpawnPosition)
    {
        AddDice(data, WorldToQueuePosition(worldSpawnPosition), true);
    }

    void AddDice(DiceData data, Vector2 spawnPosition, bool hasSpawnPosition)
    {
        if (data == null)
            return;

        pendingItems.Add(new PendingQueueItem
        {
            data = data,
            spawnPosition = spawnPosition,
            hasSpawnPosition = hasSpawnPosition
        });

        if (!flushingPendingItems)
        {
            flushingPendingItems = true;
            StartCoroutine(FlushPendingItems());
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

            DiceQueueUIItem first = items[0];
            items.RemoveAt(0);

            if (first != null)
            {
                DiceData diceData = first.data;
                EnemyManager enemyManager = EnemyManager.Instance;
                PlayerController player = enemyManager != null ? enemyManager.player : null;
                Enemy targetEnemy = enemyManager != null ? enemyManager.GetNearestAliveEnemy() : null;

                GameplayManager gameplay = GameplayManager.Instance;
                gameplay?.BeginDiceSkill(diceData, null, null, enemyManager, player, targetEnemy);

                if (diceData == null)
                    gameplay?.CancelAttack();

                diceData?.ExecuteSkill();

                yield return MoveItem(first.transform as RectTransform, GetConsumeTargetPosition(), GetItemMoveDuration());

                if (enemyManager != null && gameplay != null && !gameplay.skillSkipAttack)
                {
                    int attackCount = gameplay.GetAttackCount();
                    for (int attackIndex = 0; attackIndex < attackCount; attackIndex++)
                        enemyManager.PlayerAttack(gameplay.skillDamage);
                }

                gameplay?.RunAfterAttackActions();
                gameplay?.ClearDiceSkillState();

                float destroyDelay = GetDestroyDelay();
                if (destroyDelay > 0f)
                    yield return new WaitForSeconds(destroyDelay);

                Destroy(first.gameObject);

                if (items.Count > 0)
                {
                    yield return ShiftItems();
                    float delay = GetStepDelay();
                    if (delay > 0f)
                        yield return new WaitForSeconds(delay);
                }
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

            PendingQueueItem pendingItem = pendingItems[0];
            pendingItems.RemoveAt(0);

            DiceQueueUIItem item = Instantiate(itemPrefab, contentRoot != null ? contentRoot : transform);
            item.Setup(pendingItem.data, CaptureDicePreview(pendingItem.data));

            int index = items.Count;
            RectTransform rectTransform = item.transform as RectTransform;
            Vector2 targetPosition = GetPosition(index);
            bool shouldFlyFromSpawn = flyFromMergePosition && pendingItem.hasSpawnPosition;

            if (rectTransform != null)
            {
                PrepareItemRect(rectTransform);
                rectTransform.anchoredPosition = shouldFlyFromSpawn ? pendingItem.spawnPosition : targetPosition;
                rectTransform.localScale = Vector3.one;
            }

            items.Add(item);

            if (shouldFlyFromSpawn)
                yield return MoveItemWithArc(rectTransform, targetPosition, spawnFlyDuration, spawnFlyArcHeight);
        }

        flushingPendingItems = false;
    }

    Texture2D CaptureDicePreview(DiceData diceData)
    {
        if (diceData == null)
            return null;

        CachePreviewRefs();

        if (previewGenerator == null || itemPreviewPrefab == null)
            return null;

        return previewGenerator.Capture(itemPreviewPrefab, diceData);
    }

    void CachePreviewRefs()
    {
        if (previewGenerator == null)
            previewGenerator = FindFirstObjectByType<ItemPreviewGenerator>();
    }

    Vector2 WorldToQueuePosition(Vector3 worldPosition)
    {
        CacheCanvasRefs();

        RectTransform targetRoot = contentRoot != null
            ? contentRoot
            : transform as RectTransform;

        if (targetRoot == null)
            return startAnchoredPosition;

        Camera camera = worldCamera != null
            ? worldCamera
            : Camera.main;

        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(camera, worldPosition);
        Camera uiCamera = GetUICamera();

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                targetRoot,
                screenPosition,
                uiCamera,
                out Vector2 localPoint))
        {
            return localPoint;
        }

        return startAnchoredPosition;
    }

    void PrepareItemRect(RectTransform rect)
    {
        if (rect == null)
            return;

        RectTransform root = contentRoot != null
            ? contentRoot
            : transform as RectTransform;

        Vector2 anchor = root != null
            ? root.pivot
            : new Vector2(0.5f, 0.5f);

        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);

        if (itemSize.x > 0f)
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, itemSize.x);

        if (itemSize.y > 0f)
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, itemSize.y);

        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
    }

    void CacheCanvasRefs()
    {
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();
    }

    Camera GetUICamera()
    {
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera != null
            ? canvas.worldCamera
            : Camera.main;
    }

    IEnumerator ShiftItems()
    {
        float duration = Mathf.Max(0.01f, GetShiftMoveDuration());
        float timer = 0f;
        int itemCount = items.Count;
        Vector2[] starts = new Vector2[itemCount];
        Vector2[] targets = new Vector2[itemCount];
        RectTransform[] rects = new RectTransform[itemCount];

        for (int i = 0; i < itemCount; i++)
        {
            DiceQueueUIItem item = items[i];
            if (item == null)
                continue;

            RectTransform rect = item.transform as RectTransform;
            rects[i] = rect;
            if (rect == null)
                continue;

            starts[i] = rect.anchoredPosition;
            targets[i] = GetPosition(i);
        }

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);

            for (int i = 0; i < itemCount; i++)
            {
                RectTransform rect = rects[i];
                if (rect == null)
                    continue;

                rect.anchoredPosition = Vector2.Lerp(starts[i], targets[i], t);
            }

            yield return null;
        }
    }

    IEnumerator MoveItem(RectTransform item, Vector2 target, float duration)
    {
        if (item == null)
            yield break;

        Vector2 start = item.anchoredPosition;
        float timer = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (timer < duration)
        {
            if (item == null)
                yield break;

            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            item.anchoredPosition = Vector2.Lerp(start, target, t);
            yield return null;
        }
    }

    IEnumerator MoveItemWithArc(RectTransform item, Vector2 target, float duration, float arcHeight)
    {
        if (item == null)
            yield break;

        Vector2 start = item.anchoredPosition;
        float timer = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (timer < duration)
        {
            if (item == null)
                yield break;

            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);

            Vector2 position = Vector2.Lerp(start, target, t);
            position.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
            item.anchoredPosition = position;

            yield return null;
        }

        item.anchoredPosition = target;
    }

    Vector2 GetPosition(int index)
    {
        Vector2 startPosition = GetStartPosition();
        Vector2 offset = GetQueueOffset();

        return startPosition + offset * index;
    }

    Vector2 GetStartPosition()
    {
        return GetRectPositionInRoot(startPoint, startAnchoredPosition);
    }

    Vector2 GetQueueOffset()
    {
        if (startPoint != null && nextPoint != null)
            return GetRectPositionInRoot(nextPoint, startAnchoredPosition) - GetStartPosition();

        return useHorizontalLayout
            ? horizontalOffset
            : stackOffset != Vector2.zero
                ? stackOffset
                : verticalOffset;
    }

    Vector2 GetRectPositionInRoot(RectTransform rect, Vector2 fallback)
    {
        RectTransform root = contentRoot != null
            ? contentRoot
            : transform as RectTransform;

        if (rect == null || root == null)
            return fallback;

        CacheCanvasRefs();

        Camera uiCamera = GetUICamera();
        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(uiCamera, rect.position);

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            root,
            screenPosition,
            uiCamera,
            out Vector2 localPoint)
                ? localPoint
                : fallback;
    }

    Vector2 GetConsumeTargetPosition()
    {
        if (consumePoint != null)
            return GetRectPositionInRoot(consumePoint, GetStartPosition());

        if (contentRoot != null)
            return Vector2.zero;

        return GetStartPosition();
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
        return fastFlushRequested ? fastFlushDestroyDelay : delayDestroyTime;
    }



}


