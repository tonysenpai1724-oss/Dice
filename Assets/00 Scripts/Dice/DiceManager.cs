using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;

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
    DiceHoverService hoverService;
    DiceSpawnService spawnService;

    [Header("Stack")]
    List<Dice> boardDices = new List<Dice>();
    List<Dice> spawnedDices = new List<Dice>();

    public DiceQueueManager diceQueue;
    public DiceQueueUI diceQueueUI;

    Dictionary<Dice, int> bindTurnsMap = new Dictionary<Dice, int>();

    public bool IsSpawningHeroStartDice { get; private set; }

    [SerializeField] GameObject floatingTextPrefab;
    [SerializeField] GameObject floatingTextPrefab2;

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

        hoverService = new DiceHoverService(
            () => GameplayManager.Instance != null && GameplayManager.Instance.State == EGamePlayState.Pause,
            dice => UIManager.Instance?.ShowPopupDiceDetailTarget(dice != null ? dice.data : null, null),
            () => UIManager.Instance?.HidePopupDiceDetailTarget());

        spawnService = new DiceSpawnService(
            boardService,
            GetStartSpawnSettings,
            GetHeroSpawnSettings,
            GetDiceData,
            GetPlayerController,
            (data, position, registerOnBoard) => SpawnDice(data, position, registerOnBoard),
            RegisterBoardDice,
            routine => StartCoroutine(routine),
            value => IsSpawningHeroStartDice = value);
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
        hoverService?.UpdateHover();
    }

    DiceStartSpawnSettings GetStartSpawnSettings()
    {
        return new DiceStartSpawnSettings
        {
            minStartSpawnCount = minStartSpawnCount,
            maxStartSpawnCount = maxStartSpawnCount,
            maxSingleDiceShare = maxSingleDiceShare,
            minStartLevel = minStartLevel,
            maxStartLevel = maxStartLevel,
            diceSpacingRadius = diceSpacingRadius,
        };
    }

    DiceHeroSpawnSettings GetHeroSpawnSettings()
    {
        return new DiceHeroSpawnSettings
        {
            animateHeroStartDice = animateHeroStartDice,
            heroDiceSpawnPoint = heroDiceSpawnPoint,
            heroDiceSpawnOffset = heroDiceSpawnOffset,
            heroDiceSpawnStartDelay = heroDiceSpawnStartDelay,
            heroDiceFlyDuration = heroDiceFlyDuration,
            heroDiceFlyArcHeight = heroDiceFlyArcHeight,
            heroDiceSpawnStagger = heroDiceSpawnStagger,
            heroDiceFlySpin = heroDiceFlySpin,
        };
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
        if (GameManager.Instance != null && GameManager.Instance.IsCurrentLevelPopupOnlyGameplay())
            return;
        spawnService?.SpawnStartBoard();
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

        if (!spawnedDices.Contains(d))
            spawnedDices.Add(d);

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

    public void RegisterBoardDice( Dice dice)
    {
        if (dice == null)
            return;

        if (!boardDices.Contains(dice))
        {
            boardDices.Add(dice);
        }

        if (!spawnedDices.Contains(dice))
            spawnedDices.Add(dice);
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
        spawnedDices.Remove(dice);

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
        StopAllCoroutines();
        IsSpawningHeroStartDice = false;

        List<Dice> dicesToClear = new List<Dice>();
        AddDicesToClearList(dicesToClear, boardDices);
        AddDicesToClearList(dicesToClear, spawnedDices);
        AddDicesToClearList(dicesToClear, FindObjectsByType<Dice>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));

        for (int i = 0; i < dicesToClear.Count; i++)
        {
            Dice dice = dicesToClear[i];
            if (dice != null)
                dice.Despawn();
        }

        boardDices.Clear();
        spawnedDices.Clear();
        bindTurnsMap.Clear();
        hoverService?.ClearHover();
    }

    void AddDicesToClearList(ICollection<Dice> target, IEnumerable<Dice> source)
    {
        if (target == null || source == null)
            return;

        foreach (Dice dice in source)
        {
            if (dice == null || target.Contains(dice))
                continue;

            target.Add(dice);
        }
    }

    public void SpawnFloatingText(Vector3 position, string value, Color color, Camera camera = null)
    {
        if (floatingTextPrefab == null)
            return;

        GameObject spawned = Instantiate(floatingTextPrefab, position, Quaternion.identity);
        FloatingTextMerge floatingText = spawned.GetComponent<FloatingTextMerge>();
        if (floatingText != null)
        {
            floatingText.SetWorldPosition(position, camera);
            floatingText.SetText(value, color);
        }
    }

    public void SpawnFloatingTextAtScreenPosition(Vector2 screenPosition, string value, Color color)
    {
        if (floatingTextPrefab == null)
            return;

        GameObject spawned = Instantiate(floatingTextPrefab, screenPosition, Quaternion.identity);
        FloatingTextDmg floatingText = spawned.GetComponent<FloatingTextDmg>();
        if (floatingText == null)
            floatingText = spawned.AddComponent<FloatingTextDmg>();

        if (floatingText != null)
        {
            floatingText.SetScreenPosition(screenPosition);
            floatingText.SetText(value, color);
        }
    }
    public void SpawnFloatingTextDmg(Vector2 screenPosition, string value, Color color)
    {
        if (floatingTextPrefab2 == null)
            return;

        GameObject spawned = Instantiate(floatingTextPrefab2, screenPosition, Quaternion.identity);
        FloatingTextDmg floatingText = spawned.GetComponent<FloatingTextDmg>();
        if (floatingText != null)
        {
            floatingText.SetScreenPosition(screenPosition);
            floatingText.SetText(value, color);
        }
    }

    void SpawnMergeFloatingText(Vector3 position, string value, Color color)
    {
        SpawnFloatingText(position + floatingTextOffset, value, color);
    }

    #endregion

}




