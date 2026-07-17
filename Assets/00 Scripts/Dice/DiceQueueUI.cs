using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiceQueueUI : Singleton<DiceQueueUI>
{
    [Header("Prefab")]
    public DiceQueueUIItem itemPrefab;

    [Header("Root")]
    public RectTransform contentRoot;
    public RectTransform flyRoot;
    public RectTransform consumePoint;
    public Canvas canvas;
    public Camera worldCamera;
    public bool debugSpawnPosition = true;

    [Header("Preview")]
    public InventoryItemPreview itemPreviewPrefab;
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

    [Header("Combat")]
    public float multiAttackDelay = 0.45f;
    public float fastFlushItemMoveDuration = 0.03f;
    public float fastFlushShiftMoveDuration = 0.03f;
    public float fastFlushStepDelay;
    public float fastFlushDestroyDelay;

    readonly List<DiceQueueUIItem> items = new();

    bool processing;
    bool flushingPendingItems;
    bool fastFlushRequested;
    RectTransform runtimeFlyRoot;

    struct PendingQueueItem
    {
        public DiceData data;
        public Vector2 spawnPosition;
        public bool hasSpawnPosition;
        public bool isScreenPosition;
    }

    readonly List<PendingQueueItem> pendingItems = new();

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
        AddDice(data, Vector2.zero, false);
    }

    public void AddDice(DiceData data, Vector2 spawnPosition)
    {
        AddDice(data, spawnPosition, true);
    }

    public void AddDice(DiceData data, Vector3 worldSpawnPosition)
    {
        RectTransform targetRoot = GetFlyRoot();
        AddDice(data, WorldToRootPosition(worldSpawnPosition, targetRoot), true);
    }

    public void AddDiceFromScreenPosition(DiceData data, Vector2 screenSpawnPosition)
    {
        AddDice(data, screenSpawnPosition, true, true);
    }

    void AddDice(DiceData data, Vector2 spawnPosition, bool hasSpawnPosition, bool isScreenPosition = false)
    {
        if (data == null)
            return;

        pendingItems.Add(new PendingQueueItem
        {
            data = data,
            spawnPosition = spawnPosition,
            hasSpawnPosition = hasSpawnPosition,
            isScreenPosition = isScreenPosition
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
                Destroy(first.gameObject);

                if (enemyManager != null && gameplay != null && !gameplay.skillSkipAttack)
                {
                    int attackCount = gameplay.GetAttackCount();
                    for (int attackIndex = 0; attackIndex < attackCount; attackIndex++)
                    {
                        float attackDuration = enemyManager.PlayerAttack(gameplay.skillDamage);

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

                if (items.Count > 0)
                {
                    yield return ShiftItems();
                    float delay = GetStepDelay();
                    if (delay > 0f)
                        yield return new WaitForSeconds(delay);
                }
            }
        }

        if (EnemyManager.Instance != null)
        {
            if (EnemyManager.Instance.activeProjectiles > 0)
                yield return new WaitUntil(() => EnemyManager.Instance.activeProjectiles <= 0);

            if (EnemyManager.Instance.HasAliveEnemies())
                yield return EnemyManager.Instance.EnemyTurn();
        }

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

            RectTransform queueRoot = GetQueueRoot();
            bool shouldFlyFromSpawn = flyFromMergePosition && pendingItem.hasSpawnPosition;
            RectTransform spawnRoot = shouldFlyFromSpawn ? GetFlyRoot() : queueRoot;

            DiceQueueUIItem item = Instantiate(itemPrefab, spawnRoot != null ? spawnRoot : transform);
            item.Setup(pendingItem.data, CaptureDicePreview(pendingItem.data));

            int index = items.Count;
            RectTransform rectTransform = item.transform as RectTransform;
            Vector2 targetPosition = GetPosition(index);
            Vector2 animationTargetPosition = spawnRoot != queueRoot
                ? ConvertLocalPoint(queueRoot, targetPosition, spawnRoot)
                : targetPosition;

            if (rectTransform != null)
            {
                PrepareItemRect(rectTransform);
                if (shouldFlyFromSpawn && pendingItem.isScreenPosition)
                    rectTransform.position = pendingItem.spawnPosition;
                else
                    rectTransform.anchoredPosition = shouldFlyFromSpawn ? pendingItem.spawnPosition : targetPosition;

                rectTransform.localScale = Vector3.one;

                if (debugSpawnPosition)
                {
                    Canvas itemCanvas = rectTransform.GetComponentInParent<Canvas>();
                    Debug.Log(
                        $"[DiceQueueUI Spawn Debug] dice={pendingItem.data.name} " +
                        $"hasSpawn={pendingItem.hasSpawnPosition} isScreen={pendingItem.isScreenPosition} " +
                        $"input={pendingItem.spawnPosition} rect.position={rectTransform.position} " +
                        $"rect.anchored={rectTransform.anchoredPosition} " +
                        $"spawnRoot={GetDebugName(spawnRoot)} queueRoot={GetDebugName(queueRoot)} " +
                        $"canvas={GetDebugName(itemCanvas != null ? itemCanvas.transform : null)} " +
                        $"renderMode={(itemCanvas != null ? itemCanvas.renderMode.ToString() : "null")} " +
                        $"screenSize={Screen.width}x{Screen.height}");
                }
            }

            if (shouldFlyFromSpawn)
                yield return MoveItemWithArc(rectTransform, animationTargetPosition, spawnFlyDuration, spawnFlyArcHeight);

            if (rectTransform != null && spawnRoot != queueRoot && queueRoot != null)
            {
                rectTransform.SetParent(queueRoot, false);
                PrepareItemRect(rectTransform);
                rectTransform.anchoredPosition = targetPosition;
            }

            items.Add(item);
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
        return WorldToRootPosition(worldPosition, GetQueueRoot());
    }

    Vector2 WorldToRootPosition(Vector3 worldPosition, RectTransform targetRoot)
    {
        CacheCanvasRefs();

        if (targetRoot == null)
            return startAnchoredPosition;

        Camera camera = worldCamera != null
            ? worldCamera
            : Camera.main;

        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(camera, worldPosition);
        return ScreenToRootPosition(screenPosition, targetRoot);
    }

    Vector2 ScreenToRootPosition(Vector2 screenPosition, RectTransform targetRoot)
    {
        CacheCanvasRefs();

        if (targetRoot == null)
            return startAnchoredPosition;

        Camera uiCamera = GetUICamera(targetRoot);
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetRoot,
            screenPosition,
            uiCamera,
            out Vector2 localPoint)
                ? localPoint
                : startAnchoredPosition;
    }

    void PrepareItemRect(RectTransform rect)
    {
        if (rect == null)
            return;

        RectTransform root = rect.parent as RectTransform;
        if (root == null)
            root = GetQueueRoot();

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
    Camera GetUICamera(RectTransform rect)
    {
        Canvas targetCanvas = rect != null ? rect.GetComponentInParent<Canvas>() : null;
        if (targetCanvas == null)
            return GetUICamera();

        if (targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return targetCanvas.worldCamera != null
            ? targetCanvas.worldCamera
            : Camera.main;
    }

    Camera GetUICameraForWorldPosition(RectTransform rect, Camera sourceWorldCamera)
    {
        Canvas targetCanvas = rect != null ? rect.GetComponentInParent<Canvas>() : null;
        if (targetCanvas == null)
            return GetUICamera();

        if (targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        if (targetCanvas.renderMode == RenderMode.ScreenSpaceCamera)
            return targetCanvas.worldCamera != null ? targetCanvas.worldCamera : sourceWorldCamera;

        return sourceWorldCamera;
    }
    RectTransform GetQueueRoot()
    {
        return contentRoot != null
            ? contentRoot
            : transform as RectTransform;
    }

    RectTransform GetFlyRoot()
    {
        if (flyRoot != null)
            return flyRoot;

        if (runtimeFlyRoot != null)
            return runtimeFlyRoot;

        CacheCanvasRefs();

        Transform parent = canvas != null
            ? canvas.transform
            : transform;

        GameObject rootObject = new GameObject("DiceQueueFlyRoot", typeof(RectTransform));
        rootObject.transform.SetParent(parent, false);
        rootObject.transform.SetAsLastSibling();

        runtimeFlyRoot = rootObject.transform as RectTransform;
        if (runtimeFlyRoot != null)
        {
            runtimeFlyRoot.anchorMin = Vector2.zero;
            runtimeFlyRoot.anchorMax = Vector2.one;
            runtimeFlyRoot.offsetMin = Vector2.zero;
            runtimeFlyRoot.offsetMax = Vector2.zero;
            runtimeFlyRoot.pivot = new Vector2(0.5f, 0.5f);
            runtimeFlyRoot.localRotation = Quaternion.identity;
            runtimeFlyRoot.localScale = Vector3.one;
        }

        return runtimeFlyRoot != null
            ? runtimeFlyRoot
            : GetQueueRoot();
    }

    string GetDebugName(Object target)
    {
        return target != null ? target.name : "null";
    }

    Vector2 ConvertLocalPoint(RectTransform fromRoot, Vector2 localPoint, RectTransform toRoot)
    {
        if (fromRoot == null || toRoot == null)
            return localPoint;

        Vector3 worldPoint = fromRoot.TransformPoint(localPoint);
        Camera uiCamera = GetUICamera(toRoot);
        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(uiCamera, worldPoint);

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            toRoot,
            screenPosition,
            uiCamera,
            out Vector2 convertedPoint)
                ? convertedPoint
                : localPoint;
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

        Camera uiCamera = GetUICamera(root);
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




