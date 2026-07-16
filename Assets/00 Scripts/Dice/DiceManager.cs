using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine.InputSystem;

public class DiceManager : MonoBehaviour
{
    public static DiceManager Instance;

    [Header("Prefab")]
    public Dice dicePrefab;

    public DiceDatabaseSO diceDatabase;

    [Header("Board")]
    public Collider boardCollider;
    private BoardService boardService;

    [Header("Start Spawn")] public int minStartSpawnCount = 14;
    public int maxStartSpawnCount = 16;
    [Range(0.1f, 0.5f)] public float maxSingleDiceShare = 0.5f;

    public int minStartLevel = 1;
    public int maxStartLevel = 3;
    public float diceSpacingRadius = 1.35f;

    [Header("Hero Dice Spawn")]
    public bool animateHeroStartDice = true;
    public Transform heroDiceSpawnPoint;
    public Vector3 heroDiceSpawnOffset = new Vector3(0f, 1.5f, 0f);
    public float heroDiceSpawnStartDelay = 0.5f;
    public float heroDiceFlyDuration = 0.45f;
    public float heroDiceFlyArcHeight = 4f;
    public float heroDiceSpawnStagger = 0.08f;
    public Vector3 heroDiceFlySpin = new Vector3(540f, 720f, 360f);

    [Header("Combo")]
    [SerializeField] DiceComboConfig comboConfig = new DiceComboConfig();
    DiceComboService comboService;

    [Header("Merge")]
    [SerializeField] DiceMergeConfig mergeConfig = new DiceMergeConfig();
    DiceMergeService mergeService;

    [Header("Stack")]
    List<Dice> boardDices = new List<Dice>();

    public DiceQueueManager diceQueue;
    public DiceQueueUI diceQueueUI;

    Dictionary<Dice, int> bindTurnsMap = new Dictionary<Dice, int>();

    Dice currentHover;

    public bool IsSpawningHeroStartDice { get; private set; }

    [SerializeField] GameObject floatingTextPrefab;
    [SerializeField] Vector3 floatingTextOffset = new Vector3(0f, 1.5f, 0f);

    void Awake()
    {
        Instance = this;
        boardService = new BoardService(boardCollider);

        comboConfig.diceSpacingRadius = diceSpacingRadius;
        comboService = new DiceComboService(
            boardService,
            comboConfig,
            () => GetBoardDices(),
            (a, b) => mergeService != null && mergeService.TryMerge(a, b),
            routine => StartCoroutine(routine));

        mergeService = new DiceMergeService(
            boardService,
            mergeConfig,
            diceQueue,
            () => diceQueueUI != null ? diceQueueUI : DiceQueueUI.Instance,
            () => GetBoardDices(),
            (level, type) => GetDiceData(level, type),
            (data, position) => SpawnDice(data, position),
            dice => ReturnBoardDice(dice),
            (position, value, color) => SpawnMergeFloatingText(position, value, color),
            routine => StartCoroutine(routine),
            dice => comboService.TryComboChain(dice),
            comboService.ComboChainMap);
    }

    void Start()
    {
        if (boardCollider != null)
            SetBoardCollider(boardCollider);

        if (GameManager.Instance == null || !GameManager.Instance.IsCurrentLevelPopupOnlyGameplay())
            SpawnStartBoard();
        TigerForge.EventManager.StartListening(Constant.ON_END_GAME, ClearBoard);
    }

    void OnDestroy()
    {
        TigerForge.EventManager.StopListening(Constant.ON_END_GAME, ClearBoard);
    }

    void Update()
    {
        HandleHover();
    }

    public BoardService GetBoardService()
    {
        return boardService;
    }

    public void SetBoardCollider(Collider newCollider)
    {
        boardCollider = newCollider;
        if (boardService != null)
        {
            boardService.BoardCollider = newCollider;
        }
        else
        {
            boardService = new BoardService(newCollider);
        }
    }

    public void ConsumeBindTurns()
    {
        CleanupBindTurnMap();

        List<Dice> releasedDices = null;

        foreach (KeyValuePair<Dice, int> pair in bindTurnsMap)
        {
            int turnsRemaining = pair.Value - 1;
            bindTurnsMap[pair.Key] = turnsRemaining;

            if (turnsRemaining > 0)
                continue;

            if (releasedDices == null)
                releasedDices = new List<Dice>();

            releasedDices.Add(pair.Key);
        }

        if (releasedDices == null)
            return;

        for (int i = 0; i < releasedDices.Count; i++)
        {
            ReleaseBoundDice(releasedDices[i]);
        }
    }

    void HandleHover()
    {
        if (Mouse.current == null || Camera.main == null)
            return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        Dice hitDice = null;
        float nearestDistance = float.MaxValue;

        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null || hit.distance >= nearestDistance)
                continue;

            Dice dice = hit.collider.GetComponentInParent<Dice>();
            if (dice == null || !dice.gameObject.activeInHierarchy)
                continue;

            if (dice.state == DiceState.Merging || dice.state == DiceState.FlyingCombo)
                continue;

            hitDice = dice;
            nearestDistance = hit.distance;
        }

        if (currentHover != hitDice)
        {
            if (currentHover != null)
                currentHover.SetHovered(false);

            currentHover = hitDice;

            if (currentHover != null)
                currentHover.SetHovered(true);
        }
    }

    public DiceData GetDiceDataByLevel(int level)
    {
        return diceDatabase != null ? diceDatabase.GetDiceDataByLevel(level) : null;
    }

    public DiceData GetDiceDataByLevelAndType(int level, DiceType type)
    {
        return GetDiceData(level, type);
    }

    public List<Dice> GetBoardDices()
    {
        CleanupBoardDiceList();
        return new List<Dice>(boardDices);
    }
    #region Dice Function

    public List<Dice> GetBoardDicesByType(DiceType type)
    {
        CleanupBoardDiceList();

        List<Dice> result = new List<Dice>();

        for (int i = 0; i < boardDices.Count; i++)
        {
            Dice dice = boardDices[i];

            if (dice == null || dice.data == null)
                continue;

            if (!dice.gameObject.activeInHierarchy)
                continue;

            if (dice.type != type)
                continue;

            result.Add(dice);
        }

        return result;
    }

    public Dice GetRandomBoardDice(System.Predicate<Dice> predicate = null)
    {
        CleanupBoardDiceList();

        List<Dice> candidates = new List<Dice>();

        for (int i = 0; i < boardDices.Count; i++)
        {
            Dice dice = boardDices[i];

            if (!IsDiceValidForEnemyInteraction(dice))
                continue;

            if (predicate != null && !predicate(dice))
                continue;

            candidates.Add(dice);
        }

        if (candidates.Count == 0)
            return null;

        return candidates[Random.Range(0, candidates.Count)];
    }

    public bool BindDice(Dice targetDice, int bindTurns, bool canMerge = false, bool zeroVelocity = true)
    {
        if (!IsDiceValidForEnemyInteraction(targetDice))
            return false;

        if (bindTurns <= 0)
        {
            ReleaseBoundDice(targetDice, canMerge);
            return true;
        }

        bindTurnsMap[targetDice] = bindTurns;
        targetDice.canMerge = canMerge;
        targetDice.state = DiceState.Idle;

        if (targetDice.rb != null)
        {
            if (zeroVelocity)
            {
                targetDice.rb.linearVelocity = Vector3.zero;
                targetDice.rb.angularVelocity = Vector3.zero;
            }

            targetDice.rb.isKinematic = false;
            targetDice.ApplyGroundedConstraints();
            targetDice.rb.Sleep();
        }

        return true;
    }

    public bool StealDice(Dice targetDice)
    {
        if (!IsDiceValidForEnemyInteraction(targetDice))
            return false;

        ReturnBoardDice(targetDice);
        return true;
    }

    public Dice StealRandomDice(System.Predicate<Dice> predicate = null)
    {
        Dice targetDice = GetRandomBoardDice(predicate);

        if (targetDice == null)
            return null;

        return StealDice(targetDice) ? targetDice : null;
    }

    public bool TransformDice(Dice targetDice, DiceData newData, bool keepCurrentPosition = true)
    {
        if (!IsDiceValidForEnemyInteraction(targetDice))
            return false;

        if (newData == null)
            return false;

        Vector3 targetPosition = keepCurrentPosition
         ? boardService.FindClearPosition(targetDice.transform.position, targetDice)
         : targetDice.transform.position;

        targetDice.Setup(newData);
        targetDice.PlaceUpright(targetPosition);
        targetDice.canMerge = false;
        return true;
    }

    public bool TransformDiceType(Dice targetDice, DiceType newType)
    {
        if (targetDice == null || targetDice.data == null)
            return false;

        DiceData newData = GetDiceData(targetDice.Level, newType);
        if (newData == null)
            return false;

        return TransformDice(targetDice, newData);
    }

    public bool TransformRandomDice(DiceType newType, System.Predicate<Dice> predicate = null)
    {
        Dice targetDice = GetRandomBoardDice(predicate);

        if (targetDice == null)
            return false;

        return TransformDiceType(targetDice, newType);
    }

    public int TransformAllDiceOfType(DiceType fromType, DiceType toType)
    {
        CleanupBoardDiceList();

        int transformedCount = 0;

        for (int i = 0; i < boardDices.Count; i++)
        {
            Dice dice = boardDices[i];

            if (!IsDiceValidForEnemyInteraction(dice))
                continue;

            if (dice.type != fromType)
                continue;

            if (TransformDiceType(dice, toType))
            {
                transformedCount++;
            }
        }

        return transformedCount;
    }

    public int GetBindTurnsRemaining(Dice dice)
    {
        if (dice == null)
            return 0;

        return bindTurnsMap.TryGetValue(dice, out int turnsRemaining)
            ? Mathf.Max(0, turnsRemaining)
            : 0;
    }

    public bool IsDiceBound(Dice dice)
    {
        return GetBindTurnsRemaining(dice) > 0;
    }

    void ReleaseBoundDice(Dice dice, bool canMerge = false)
    {
        if (dice == null)
            return;

        bindTurnsMap.Remove(dice);

        if (!dice.gameObject.activeInHierarchy)
            return;

        dice.canMerge = canMerge;
        dice.state = DiceState.Idle;
    }

    void CleanupBindTurnMap()
    {
        List<Dice> invalidDices = null;

        foreach (KeyValuePair<Dice, int> pair in bindTurnsMap)
        {
            Dice dice = pair.Key;

            if (dice != null && dice.gameObject.activeInHierarchy)
                continue;

            if (invalidDices == null)
            {
                invalidDices = new List<Dice>();
            }

            invalidDices.Add(dice);
        }

        if (invalidDices == null)
            return;

        for (int i = 0; i < invalidDices.Count; i++)
        {
            bindTurnsMap.Remove(invalidDices[i]);
        }
    }

    bool IsDiceValidForEnemyInteraction(Dice dice)
    {
        if (dice == null)
            return false;

        if (dice.data == null)
            return false;

        if (!dice.gameObject.activeInHierarchy)
            return false;

        if (dice.isMerging ||
            dice.state == DiceState.Merging ||
            dice.state == DiceState.FlyingCombo)
            return false;

        return true;
    }

    void CleanupBoardDiceList()
    {
        for (int i = boardDices.Count - 1; i >= 0; i--)
        {
            Dice dice = boardDices[i];

            if (dice == null)
            {
                boardDices.RemoveAt(i);
                continue;
            }

            if (!dice.gameObject.activeInHierarchy)
            {
                boardDices.RemoveAt(i);
            }
        }
    }
    #endregion
    #region SPAWN
    [Button]

    public void SpawnStartBoard()
    {
        if (boardService == null)
        {
            Debug.LogError("BoardService is null!");
            return;
        }

        int targetSpawnCount = Random.Range(minStartSpawnCount, maxStartSpawnCount + 1);
        List<DiceData> plannedStartDice = BuildBalancedStartDicePlan(targetSpawnCount);

        List<Vector3> plannedPositions = boardService.BuildSpreadSpawnPositions(targetSpawnCount);

        int spawnCount = Mathf.Min(plannedStartDice.Count, plannedPositions.Count);
        for (int i = 0; i < spawnCount; i++)
        {
            DiceData data = plannedStartDice[i];
            if (data == null)
                continue;

            // Sửa: gọi qua boardService
            Vector3 position = boardService.FindClearPosition(plannedPositions[i], null, diceSpacingRadius);
            if (boardService.IsOccupied(position, null, diceSpacingRadius))
                continue;

            SpawnDice(data, position);
        }

        SpawnPlayerStartDiceDatas(targetSpawnCount);
    }

    List<DiceData> BuildBalancedStartDicePlan(int targetSpawnCount)
    {
        List<DiceData> result = new List<DiceData>();
        List<DiceData> normalCandidates = new List<DiceData>();

        for (int level = minStartLevel; level <= maxStartLevel; level++)
        {
            DiceData data = GetDiceData(level, DiceType.Normal);
            if (data != null)
                normalCandidates.Add(data);
        }

        if (normalCandidates.Count == 0)
            return result;

        Dictionary<DiceData, int> counts = new Dictionary<DiceData, int>();
        int maxPerDice = Mathf.Max(1, Mathf.FloorToInt(targetSpawnCount * maxSingleDiceShare));

        List<DiceData> shuffled = new List<DiceData>(normalCandidates);
        for (int i = 0; i < shuffled.Count; i++)
        {
            int swapIndex = Random.Range(i, shuffled.Count);
            DiceData temp = shuffled[i];
            shuffled[i] = shuffled[swapIndex];
            shuffled[swapIndex] = temp;
        }

        int cursor = 0;
        while (result.Count < targetSpawnCount)
        {
            DiceData candidate = shuffled[cursor % shuffled.Count];
            cursor++;

            if (!counts.ContainsKey(candidate))
                counts[candidate] = 0;

            if (counts[candidate] >= maxPerDice)
            {
                bool foundAlternative = false;
                for (int i = 0; i < shuffled.Count; i++)
                {
                    DiceData alternative = shuffled[(cursor + i) % shuffled.Count];
                    if (!counts.ContainsKey(alternative))
                        counts[alternative] = 0;

                    if (counts[alternative] >= maxPerDice)
                        continue;

                    candidate = alternative;
                    foundAlternative = true;
                    break;
                }

                if (!foundAlternative)
                    break;
            }

            counts[candidate]++;
            result.Add(candidate);
        }

        while (result.Count < targetSpawnCount)
        {
            result.Add(shuffled[Random.Range(0, shuffled.Count)]);
        }

        return result;
    }

    private void SpawnPlayerStartDiceDatas(int targetSpawnCount)
    {
        PlayerController player = GetPlayerController();
        if (player == null)
            return;

        if (player.diceDatas == null || player.diceDatas.Count == 0)
            player.InitializeDiceDatas();

        if (player.diceDatas == null || boardService == null)
            return;

        if (animateHeroStartDice)
        {
            IsSpawningHeroStartDice = true;
            StartCoroutine(SpawnPlayerStartDiceDatasRoutine(player, targetSpawnCount));
            return;
        }

        for (int i = 0; i < player.diceDatas.Count; i++)
        {
            DiceData data = player.diceDatas[i];
            if (data == null)
                continue;

            int attempts = 0;
            int maxAttempts = Mathf.Max(12, targetSpawnCount * 12);
            while (attempts < maxAttempts)
            {
                attempts++;
                Vector3 position = boardService.GetRandomPositionOnBoard();
                Vector3 clearPos = boardService.FindClearPosition(position, null, diceSpacingRadius);

                if (!boardService.IsOccupied(clearPos, null, diceSpacingRadius))
                {
                    SpawnDice(data, clearPos);
                    break;
                }
            }
        }
    }

    IEnumerator SpawnPlayerStartDiceDatasRoutine(PlayerController player, int targetSpawnCount)
    {
        if (heroDiceSpawnStartDelay > 0f)
            yield return new WaitForSeconds(heroDiceSpawnStartDelay);

        for (int i = 0; i < player.diceDatas.Count; i++)
        {
            DiceData data = player.diceDatas[i];
            if (data == null)
                continue;

            if (!TryGetHeroDiceBoardPosition(targetSpawnCount, out Vector3 targetPosition))
                continue;

            Dice dice = SpawnDice(data, targetPosition, false);
            if (dice == null)
                continue;

            Vector3 startPosition = GetHeroDiceSpawnPosition(player, targetPosition);
            yield return FlyHeroDiceToBoard(dice, startPosition, targetPosition);

            RegisterBoardDice(dice);

            if (heroDiceSpawnStagger > 0f)
                yield return new WaitForSeconds(heroDiceSpawnStagger);
        }

        IsSpawningHeroStartDice = false;
    }

    bool TryGetHeroDiceBoardPosition(int targetSpawnCount, out Vector3 clearPosition)
    {
        clearPosition = Vector3.zero;

        if (boardService == null)
            return false;

        int attempts = 0;
        int maxAttempts = Mathf.Max(12, targetSpawnCount * 12);
        while (attempts < maxAttempts)
        {
            attempts++;
            Vector3 position = boardService.GetRandomPositionOnBoard();
            clearPosition = boardService.FindClearPosition(position, null, diceSpacingRadius);

            if (!boardService.IsOccupied(clearPosition, null, diceSpacingRadius))
                return true;
        }

        return false;
    }

    Vector3 GetHeroDiceSpawnPosition(PlayerController player, Vector3 targetPosition)
    {
        if (heroDiceSpawnPoint != null)
            return heroDiceSpawnPoint.position;

        if (player != null)
            return player.transform.position + heroDiceSpawnOffset;

        return targetPosition + Vector3.up * heroDiceFlyArcHeight;
    }

    IEnumerator FlyHeroDiceToBoard(Dice dice, Vector3 startPosition, Vector3 targetPosition)
    {
        if (dice == null)
            yield break;

        dice.state = DiceState.FlyingCombo;
        dice.canMerge = false;
        dice.SetCollisionEnabled(false);
        dice.transform.position = startPosition;
        dice.transform.rotation = Random.rotation;

        if (dice.rb != null)
        {
            dice.rb.linearVelocity = Vector3.zero;
            dice.rb.angularVelocity = Vector3.zero;
            dice.rb.isKinematic = true;
            dice.rb.position = startPosition;
            dice.rb.rotation = dice.transform.rotation;
        }

        float duration = Mathf.Max(0.01f, heroDiceFlyDuration);
        Quaternion startRotation = dice.transform.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            Vector3 position = Vector3.Lerp(startPosition, targetPosition, easedT);
            position.y += Mathf.Sin(t * Mathf.PI) * heroDiceFlyArcHeight;

            dice.transform.position = position;
            dice.transform.rotation = startRotation * Quaternion.Euler(heroDiceFlySpin * t);

            if (dice.rb != null)
            {
                dice.rb.position = position;
                dice.rb.rotation = dice.transform.rotation;
            }

            yield return null;
        }

        dice.state = DiceState.Idle;
        dice.PlaceUpright(targetPosition);
        dice.SetCollisionEnabled(true);
    }
    PlayerController GetPlayerController()
    {
        if (EnemyManager.Instance != null && EnemyManager.Instance.player != null)
            return EnemyManager.Instance.player;

        return FindFirstObjectByType<PlayerController>();
    }

    public Dice SpawnDice(DiceData data, Vector3 pos, bool registerOnBoard = true)
    {
        if (data == null || dicePrefab == null)
            return null;

        Quaternion rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

        Dice d = ObjectPooler.Spawn(dicePrefab, pos, rotation);
        if (d == null)
            return null;

        d.Setup(data);

        Vector3 clearPos = boardService != null
            ? boardService.FindClearPosition(pos, d, diceSpacingRadius)
            : pos;

        d.transform.position = clearPos;
        d.rb.linearVelocity = Vector3.zero;
        d.rb.angularVelocity = Vector3.zero;
        d.rb.position = clearPos;
        d.rb.rotation = d.GetUprightRotation();
        d.rb.Sleep();
        d.ApplyGroundedConstraints();

        if (registerOnBoard)
        {
            RegisterBoardDice(d);
        }

        return d;
    }

    public DiceData GetDiceData(int level, DiceType type)
    {
        return diceDatabase != null
            ? diceDatabase.GetDiceData(level, type)
            : null;
    }

    public void RegisterBoardDice(
        Dice dice
    )
    {
        if (dice == null)
            return;

        if (!boardDices.Contains(dice))
        {
            boardDices.Add(dice);
        }
    }

    public void SetBoardMergeEnabled(bool enabled)
    {
        for (int i = boardDices.Count - 1; i >= 0; i--)
        {
            Dice dice = boardDices[i];

            if (dice == null)
            {
                boardDices.RemoveAt(i);
                continue;
            }

            if (!dice.gameObject.activeInHierarchy)
                continue;

            if (dice.isMerging ||
                dice.state == DiceState.Merging ||
                dice.state == DiceState.FlyingCombo)
                continue;

            dice.canMerge = enabled;
        }
    }

    public void ResetBoard()
    {
        StopAllCoroutines();
        IsSpawningHeroStartDice = false;

        for (int i = boardDices.Count - 1; i >= 0; i--)
        {
            if (boardDices[i] == null)
            {
                boardDices.RemoveAt(i);
                continue;
            }

            ReturnBoardDice(
                boardDices[i]
            );
        }

        if (GameManager.Instance == null || !GameManager.Instance.IsCurrentLevelPopupOnlyGameplay())
            SpawnStartBoard();
    }
    public DiceData GetRandomDiceDataByLevel(int level)
    {
        List<DiceData> list = diceDatabase != null
            ? diceDatabase.GetAllByLevel(level)
            : null;

        if (list == null || list.Count == 0)
            return null;

        return list[
            Random.Range(0, list.Count)
        ];
    }

    public bool IsBoardStable(float velocityThreshold, float angularVelocityThreshold)
    {
        for (int i = boardDices.Count - 1; i >= 0; i--)
        {
            Dice dice =
                boardDices[i];

            if (dice == null)
            {
                boardDices.RemoveAt(i);
                continue;
            }

            if (!dice.gameObject.activeInHierarchy)
                continue;

            if (dice.state == DiceState.Merging ||
                dice.state == DiceState.FlyingCombo)
                return false;

            if (dice.rb == null)
                continue;

            if (dice.rb.linearVelocity.sqrMagnitude >
                velocityThreshold * velocityThreshold)
                return false;

            if (dice.rb.angularVelocity.sqrMagnitude >
                angularVelocityThreshold * angularVelocityThreshold)
                return false;
        }

        return true;
    }

    void ReturnBoardDice(Dice dice)
    {
        if (dice == null)
            return;

        boardDices.Remove(dice);

        dice.Despawn();
    }

    #endregion

    #region MERGE

    public bool TryMerge(Dice a, Dice b)
    {
        return mergeService != null && mergeService.TryMerge(a, b);
    }

    public bool ForceMergeInRadius(Vector3 center, float radius)
    {
        return ForceMergeNearCenter(center, radius, radius);
    }

    public bool ForceMergeNearCenter(Vector3 center, float searchRadius, float mergeRadius)
    {
        CleanupBoardDiceList();

        float sqrSearchRadius = searchRadius * searchRadius;
        float sqrMergeRadius = mergeRadius * mergeRadius;

        for (int i = 0; i < boardDices.Count; i++)
        {
            Dice first = boardDices[i];
            if (!CanForceMergeCandidate(first, center, sqrSearchRadius))
                continue;

            for (int j = i + 1; j < boardDices.Count; j++)
            {
                Dice second = boardDices[j];
                if (!CanForceMergeCandidate(second, center, sqrSearchRadius))
                    continue;

                if (first.Level != second.Level)
                    continue;

                Vector3 pairCenter = (first.transform.position + second.transform.position) * 0.5f;
                Vector3 pairOffset = pairCenter - center;
                pairOffset.y = 0f;
                if (pairOffset.sqrMagnitude > sqrMergeRadius)
                    continue;

                if (TryMerge(first, second))
                    return true;
            }
        }

        return false;
    }

    bool CanForceMergeCandidate(Dice dice, Vector3 center, float sqrRadius)
    {
        if (dice == null || dice.data == null)
            return false;

        if (!dice.gameObject.activeInHierarchy)
            return false;

        if (dice.isMerging || dice.state == DiceState.Merging || dice.state == DiceState.FlyingCombo)
            return false;

        Vector3 offset = dice.transform.position - center;
        offset.y = 0f;
        if (offset.sqrMagnitude > sqrRadius)
            return false;

        dice.canMerge = true;
        return true;
    }

    public void ClearBoard()
    {
        IsSpawningHeroStartDice = false;

        foreach (Dice dice in boardDices)
        {
            ObjectPooler.Despawn(dice);
        }
    }

    void SpawnMergeFloatingText(Vector3 position, string value, Color color)
    {
        if (floatingTextPrefab == null)
            return;
        Vector3 spawnPosition = position + floatingTextOffset;
        GameObject spawned = Instantiate(floatingTextPrefab, spawnPosition, Quaternion.identity);
        FloatingText floatingText = spawned.GetComponent<FloatingText>();
        if (floatingText != null)
        {
            floatingText.SetWorldPosition(spawnPosition);
            floatingText.SetText(value, color);
        }
    }

    #endregion

}





