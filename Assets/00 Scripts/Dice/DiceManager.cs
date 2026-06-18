
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

    [Header("Start Spawn")] public int minStartSpawnCount = 14;
    public int maxStartSpawnCount = 16;
    [Range(0.1f, 0.5f)] public float maxSingleDiceShare = 0.5f;

    public int minStartLevel = 1;
    public int maxStartLevel = 3;
    public float spawnPadding = 1.25f;
    [Range(0.1f, 1f)]
    public float sideSpawnPercent = 0.6f;
    [Range(0.1f, 1f)]
    public float topSpawnPercent = 0.6f;
    public float diceSpacingRadius = 1.35f;
    public float startSpawnGridJitter = 0.35f;
    public int spawnSearchSteps = 18;
    public float spawnSearchRadiusStep = 0.6f;

    [Header("Combo")]
    public float comboArcHeight = 2.5f;
    public float comboDuration = 0.4f;
    public float comboSideScatter = 0.45f;
    public Vector2 comboSpinTurnsX = new Vector2(1.5f, 3f);
    public Vector2 comboSpinTurnsY = new Vector2(0.5f, 1.5f);
    public Vector2 comboSpinTurnsZ = new Vector2(1.5f, 3f);

    [Header("Combo Distance Scaling")]
    public float comboDistancePerChain = 1.5f;
    public float maxComboDistanceLimit = 12f;
    Dictionary<Dice, float> comboLastTime = new Dictionary<Dice, float>();
    //  public Transform point;
    [Header("Bomb Explosion")]
    public float bombExplosionRadius = 12f;
    public float bombExplosionForce = 18f;
    public float bombExplosionMinForce = 5f;
    public GameObject bombExplosionPrefab;

    [Header("Stack")]

    List<Dice> boardDices =
        new List<Dice>();

    public DiceQueue diceQueue;
    [Header("Combo Scaling")]
    public float comboArcPerChain = 0.45f;
    public float maxComboArcHeight = 6f;

    public float comboDurationPerChain = 0.03f;
    public float maxComboDuration = 0.75f;

    Dictionary<Dice, int> comboChainMap =
        new Dictionary<Dice, int>();
    Dictionary<Dice, int> bindTurnsMap =
        new Dictionary<Dice, int>();

    Dice currentHover;

    [SerializeField] GameObject floatingTextPrefab;
    [SerializeField] Vector3 floatingTextOffset = new Vector3(0f, 1.5f, 0f);
    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
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
            {
                releasedDices = new List<Dice>();
            }

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

        Ray ray =
            Camera.main.ScreenPointToRay(
                Mouse.current.position.ReadValue()
            );

        Dice hitDice = null;
        float nearestDistance = float.MaxValue;

        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            Mathf.Infinity,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null || hit.distance >= nearestDistance)
                continue;

            Dice dice = hit.collider.GetComponentInParent<Dice>();
            if (dice == null)
                continue;

            if (!dice.gameObject.activeInHierarchy)
                continue;

            if (dice.state == DiceState.Merging ||
                dice.state == DiceState.FlyingCombo)
                continue;

            hitDice = dice;
            nearestDistance = hit.distance;
        }

        if (currentHover != hitDice)
        {
            if (currentHover != null)
            {
                currentHover.SetHovered(false);
            }

            currentHover = hitDice;

            if (currentHover != null)
            {
                currentHover.SetHovered(true);
            }
        }
    }
    public DiceData GetDiceDataByLevel(int level)
    {
        return diceDatabase != null
            ? diceDatabase.GetDiceDataByLevel(level)
            : null;
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
            ? FindClearPosition(targetDice.transform.position, targetDice)
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
        if (boardCollider == null)
            return;

        Bounds b = boardCollider.bounds;
        float boardY = GetBoardSurfaceY();
        int targetSpawnCount = Random.Range(minStartSpawnCount, maxStartSpawnCount + 1);
        float sideMarginPercent = (1f - sideSpawnPercent) * 0.5f;

        float minX = Mathf.Lerp(b.min.x + spawnPadding, b.max.x - spawnPadding, sideMarginPercent);
        float maxX = Mathf.Lerp(b.min.x + spawnPadding, b.max.x - spawnPadding, 1f - sideMarginPercent);
        float minZ = Mathf.Lerp(b.min.z + spawnPadding, b.max.z - spawnPadding, 1f - topSpawnPercent);
        float maxZ = Mathf.Max(b.min.z + spawnPadding, b.max.z - spawnPadding);

        List<DiceData> plannedStartDice = BuildBalancedStartDicePlan(targetSpawnCount);
        List<Vector3> plannedPositions = BuildSpreadSpawnPositions(targetSpawnCount, minX, maxX, minZ, maxZ, boardY);

        int spawnCount = Mathf.Min(plannedStartDice.Count, plannedPositions.Count);
        for (int i = 0; i < spawnCount; i++)
        {
            DiceData data = plannedStartDice[i];
            if (data == null)
                continue;

            Vector3 position = FindClearPosition(plannedPositions[i]);
            if (IsOccupied(position, null))
                continue;

            SpawnDice(data, position);
        }

        SpawnPlayerStartDiceDatas(minX, maxX, minZ, maxZ, boardY, targetSpawnCount);
    }

    List<Vector3> BuildSpreadSpawnPositions(int targetSpawnCount, float minX, float maxX, float minZ, float maxZ, float boardY)
    {
        List<Vector3> result = new List<Vector3>();
        if (targetSpawnCount <= 0)
            return result;

        int columns = Mathf.CeilToInt(Mathf.Sqrt(targetSpawnCount));
        int rows = Mathf.CeilToInt(targetSpawnCount / (float)columns);

        float width = Mathf.Max(0.1f, maxX - minX);
        float depth = Mathf.Max(0.1f, maxZ - minZ);
        float cellWidth = width / columns;
        float cellDepth = depth / rows;

        List<Vector2Int> cells = new List<Vector2Int>();
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                cells.Add(new Vector2Int(col, row));
            }
        }

        for (int i = 0; i < cells.Count; i++)
        {
            int swapIndex = Random.Range(i, cells.Count);
            Vector2Int temp = cells[i];
            cells[i] = cells[swapIndex];
            cells[swapIndex] = temp;
        }

        int count = Mathf.Min(targetSpawnCount, cells.Count);
        for (int i = 0; i < count; i++)
        {
            Vector2Int cell = cells[i];
            float centerX = minX + (cell.x + 0.5f) * cellWidth;
            float centerZ = minZ + (cell.y + 0.5f) * cellDepth;

            float jitterX = Random.Range(-cellWidth * startSpawnGridJitter, cellWidth * startSpawnGridJitter);
            float jitterZ = Random.Range(-cellDepth * startSpawnGridJitter, cellDepth * startSpawnGridJitter);

            Vector3 candidate = new Vector3(
                Mathf.Clamp(centerX + jitterX, minX, maxX),
                boardY,
                Mathf.Clamp(centerZ + jitterZ, minZ, maxZ)
            );

            result.Add(candidate);
        }

        return result;
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

    bool TrySpawnStartDice(DiceData data, float minX, float maxX, float minZ, float maxZ, float boardY)
    {
        if (data == null)
            return false;

        Vector3 candidate =
            new Vector3(Random.Range(minX, maxX), boardY, Random.Range(minZ, maxZ));

        Vector3 pos =
            FindClearPosition(candidate);

        if (IsOccupied(pos, null))
            return false;

        SpawnDice(data, pos);
        return true;
    }

    void SpawnPlayerStartDiceDatas(float minX, float maxX, float minZ, float maxZ, float boardY, int targetSpawnCount)
    {
        PlayerController player = GetPlayerController();
        if (player == null)
            return;

        if (player.diceDatas == null || player.diceDatas.Count == 0)
            player.InitializeDiceDatas();

        if (player.diceDatas == null)
            return;

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

                if (TrySpawnStartDice(data, minX, maxX, minZ, maxZ, boardY))
                    break;
            }
        }
    }

    PlayerController GetPlayerController()
    {
        if (EnemyManager.Instance != null && EnemyManager.Instance.player != null)
            return EnemyManager.Instance.player;

        return FindFirstObjectByType<PlayerController>();
    }

    public Dice SpawnDice(DiceData data, Vector3 pos, bool registerOnBoard = true)
    {
        Quaternion rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

        Dice d = ObjectPooler.Spawn(dicePrefab, pos, rotation);
        d.Setup(data);

        // FORCE STABLE
        d.transform.position = pos;

        d.rb.linearVelocity = Vector3.zero;
        d.rb.angularVelocity = Vector3.zero;

        d.rb.position = pos;
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

    public void TryMerge(Dice a, Dice b)
    {
        if (a == null || b == null)
            return;

        if (a == b)
            return;

        if (a.isMerging || b.isMerging)
            return;

        if (!a.gameObject.activeInHierarchy)
            return;

        if (!b.gameObject.activeInHierarchy)
            return;

        if (a.Level != b.Level)
            return;

        if (a.state == DiceState.Merging ||
            b.state == DiceState.Merging)
            return;

        // LOCK IMMEDIATELY
        a.isMerging = true;
        b.isMerging = true;

        StartCoroutine(
            MergeRoutine(a, b)
        );
    }

    IEnumerator MergeRoutine(Dice a, Dice b)
    {
        a.FreezeForMerge();
        b.FreezeForMerge();

        Vector3 mergePos =
            (a.transform.position +
            b.transform.position) * 0.5f;
        mergePos.y = GetBoardSurfaceY();

        diceQueue.AddDice(a.data, mergePos);
        diceQueue.AddDice(b.data, mergePos);


        int nextLevel = a.Level + 1;
        int chain = 1;

        if (comboChainMap.ContainsKey(a))
        {
            chain = comboChainMap[a];
        }

        ReturnBoardDice(a);

        ReturnBoardDice(b);

        DiceData bombData = GetBombDiceData(a.data, b.data);
        if (bombData != null)
            ExplodeBoardDice(mergePos, bombData);

        DiceData nextData = GetDiceData(a.Level + 1, DiceType.Normal);

        if (nextData == null)
        {
            Debug.LogError(
                $"KhÃ´ng tÃ¬m tháº¥y data Level {a.Level + 1} Type {a.type}"
            );
            yield break;
        }

        Dice merged = SpawnDice(nextData, FindClearPosition(mergePos));

        SpawnMergeFloatingText(
            merged != null ? merged.transform.position : mergePos,
            merged.data.level.ToString(), merged.data.diceColor
        );
        if (merged.data != null && merged.data.hitEffectPrefab != null)
        {
            Vector3 fxPos = merged.transform.position;

            if (merged.cachedCollider != null)
            {
                fxPos.y = 11.5f;
            }

            GameObject fx = Instantiate(merged.data.hitEffectPrefab, fxPos, Quaternion.identity);

            Destroy(fx, 1f);
        }

        comboChainMap[merged] = chain;

        merged.PlaceUpright(
            merged.transform.position
        );
        // =========================
        // HIT EFFECT
        // =========================



        TryComboChain(merged);
        yield break;
    }

    DiceData GetBombDiceData(DiceData first, DiceData second)
    {
        if (first != null && first.type == DiceType.Bomb)
            return first;

        if (second != null && second.type == DiceType.Bomb)
            return second;

        return null;
    }
    public void ClearBoard()
    {

        foreach (Dice dice in boardDices)
        {
            ObjectPooler.Despawn(dice);
        }
    }

    void ExplodeBoardDice(Vector3 position, DiceData sourceData)
    {
        GameObject prefab = sourceData != null && sourceData.hitEffectPrefab != null
            ? sourceData.hitEffectPrefab
            : bombExplosionPrefab;

        if (prefab != null)
        {
            GameObject fx = Instantiate(prefab, position, Quaternion.identity);
            Destroy(fx, 1f);
        }

        float radius = Mathf.Max(0.01f, bombExplosionRadius);

        for (int i = boardDices.Count - 1; i >= 0; i--)
        {
            Dice dice = boardDices[i];
            if (dice == null)
            {
                boardDices.RemoveAt(i);
                continue;
            }

            if (!dice.gameObject.activeInHierarchy ||
                dice.rb == null ||
                dice.rb.isKinematic ||
                dice.state == DiceState.Merging ||
                dice.state == DiceState.FlyingCombo)
            {
                continue;
            }

            Vector3 direction = dice.transform.position - position;
            direction.y = 0f;

            float distance = direction.magnitude;
            if (distance > radius || distance <= 0.001f)
                continue;

            float forceMultiplier = 1f - Mathf.Clamp01(distance / radius);
            float impulse = Mathf.Max(bombExplosionMinForce, bombExplosionForce * forceMultiplier);
            Vector3 force = direction.normalized * impulse;

            dice.ApplyBoardMoveConstraints();
            dice.rb.WakeUp();
            dice.rb.AddForce(force, ForceMode.VelocityChange);
        }
    }

    void SpawnMergeFloatingText(Vector3 position, string value, Color color)
    {
        if (floatingTextPrefab == null)
            return;

        Vector3 spawnPosition = position + floatingTextOffset;
        GameObject textObject = Instantiate(
            floatingTextPrefab,
            spawnPosition,
            Quaternion.identity
        );

        FloatingText floatingText = textObject.GetComponent<FloatingText>();
        if (floatingText != null)
        {
            floatingText.SetWorldPosition(spawnPosition);
            floatingText.SetText(value, color);
        }
    }

    #endregion

    #region COMBO
    public float maxComboDistance = 4f;

    void TryComboChain(Dice dice)
    {
        if (dice == null)
            return;

        Dice target =
            FindNearestSameLevelDice(dice);

        // no target
        if (target == null)
        {
            Vector3 randomTargetPos =
                FindRandomClearPositionWithinRadius(dice.transform.position, maxComboDistance, dice);

            Vector3 randomDir = randomTargetPos - dice.transform.position;

            randomDir.y = 0f;

            if (randomDir.sqrMagnitude < 0.001f)
            {
                randomDir = Vector3.forward;
            }
            else
            {
                randomDir.Normalize();
            }

            StartCoroutine(
                ComboJumpRoutine(
                    dice,
                    null,
                    randomTargetPos,
                    randomDir,
                    true
                )
            );

            return;
        }

        Vector3 dir = (target.transform.position - dice.transform.position).normalized;
        int comboCount = 1;

        if (comboChainMap.ContainsKey(dice))
        {
            comboCount = comboChainMap[dice];
        }

        float dynamicMaxComboDistance = Mathf.Min(maxComboDistance + comboCount * comboDistancePerChain,
                maxComboDistanceLimit
            );

        float dist = Vector3.Distance(dice.transform.position, target.transform.position);

        Vector3 targetPos;

        if (dist > dynamicMaxComboDistance)
        {
            targetPos = dice.transform.position + dir * dynamicMaxComboDistance;
        }
        else
        {
            targetPos = target.transform.position;
        }

        targetPos.y = GetBoardSurfaceY();

        StartCoroutine(
            ComboJumpRoutine(
                dice,
                target,
                targetPos,
                dir,
                dist > dynamicMaxComboDistance
            )
        );
    }


    IEnumerator ComboJumpRoutine(Dice dice, Dice target, Vector3 targetPos, Vector3 dir,
        bool shouldFullBounce
    )
    {
        if (dice == null)
            yield break;

        dice.state = DiceState.FlyingCombo;

        dice.canMerge = true;

        dice.rb.isKinematic = true;

        dice.rb.linearVelocity = Vector3.zero;

        dice.rb.angularVelocity = Vector3.zero;

        // Keep collider enabled so jumping dice can collide with other dice
        // dice.SetCollisionEnabled(false);

        Vector3 start = dice.transform.position;

        // If we have a valid merge target, land on it directly.
        // Otherwise, keep the landing position clear so the combo can finish safely.
        Vector3 finalDestination = targetPos;
        finalDestination.y = GetBoardSurfaceY();

        bool canAimForTarget =
            target != null &&
            target.gameObject.activeInHierarchy &&
            target.Level == dice.Level &&
            !target.isMerging;

        if (!canAimForTarget)
        {
            finalDestination = FindClearPosition(finalDestination, dice);
        }

        // Bounces will carry forward momentum in the jump direction (dir)
        Vector3 jumpDir = dir.sqrMagnitude > 0.001f ? dir.normalized : Vector3.forward;
        float originalDist = Vector3.Distance(start, finalDestination);
        float actualBounceDistance = Mathf.Min(2.8f, originalDist * 0.55f);

        // The main jump ends slightly before the finalDestination
        Vector3 mainJumpEnd = finalDestination - jumpDir * actualBounceDistance;
        mainJumpEnd.y = GetBoardSurfaceY();

        Vector3 finalPos =
            mainJumpEnd;
        int comboCount = 1;

        if (comboChainMap.ContainsKey(dice))
        {
            comboCount = comboChainMap[dice] + 1;
        }

        comboChainMap[dice] = comboCount;
        comboLastTime[dice] = Time.time;

        // RANDOM SIDE ARC
        Vector3 sideOffset = Vector3.Cross(Vector3.up, dir.sqrMagnitude > 0.001f ? dir.normalized : Vector3.forward
            ) * Random.Range(
                -comboSideScatter,
                comboSideScatter
            );

        // NATURAL SPIN
        Vector3 angularSpin = new Vector3(
         Random.Range(comboSpinTurnsX.x, comboSpinTurnsX.y),
         Random.Range(comboSpinTurnsY.x, comboSpinTurnsY.y),
         Random.Range(comboSpinTurnsZ.x, comboSpinTurnsZ.y)
         );
        // OCCASIONAL CRAZY SPIN
        if (Random.value < 0.2f)
        {
            angularSpin *= 1.8f;
        }

        float t = 0f;
        float dynamicDuration = Mathf.Min(comboDuration +
        comboCount * comboDurationPerChain,
        maxComboDuration
        );

        while (t < 1f)
        {
            if (dice == null)
                yield break;

            if (
                target != null &&
                !target.gameObject.activeInHierarchy
            )
            {
                break;
            }

            t += Time.deltaTime / dynamicDuration;

            Vector3 end = mainJumpEnd;

            finalPos = end;

            // POSITION
            Vector3 pos = Vector3.Lerp(start, end, t);

            // =========================
            // DYNAMIC ARC
            // =========================

            float dynamicArcHeight = Mathf.Min(
                    comboArcHeight +
                    comboCount *
                    comboArcPerChain,
                    maxComboArcHeight
                );

            // NATURAL ARC
            float arc = Mathf.Sin(t * Mathf.PI);
            arc = Mathf.Clamp01(arc);
            arc = Mathf.Pow(arc, 0.7f);

            pos.y += arc * dynamicArcHeight;

            // SIDE MOTION
            pos += sideOffset * Mathf.Sin(t * Mathf.PI);
            dice.transform.position = pos;

            // NATURAL SPIN DECAY
            float spinDamping = 1f - Mathf.Pow(t, 1.8f);

            Vector3 currentSpin = angularSpin * spinDamping;

            dice.transform.Rotate(currentSpin * Time.deltaTime, Space.Self);

            yield return null;
        }

        // TRY MERGE
        if (target != null && target.gameObject.activeInHierarchy)
        {
            float distToTarget = Vector3.Distance(
                    dice.transform.position,
                    target.transform.position
                );

            float mergeDistance = Mathf.Max(1.2f, diceSpacingRadius * 1.25f);

            if (distToTarget <= mergeDistance && target.Level == dice.Level &&
                !target.isMerging && !dice.isMerging)
            {
                comboChainMap[target] = comboCount;

                TryMerge(dice, target);
                yield break;
            }
        }
        comboChainMap.Remove(dice);
        if (target != null)
        {
            comboChainMap.Remove(target);
        }

        Quaternion targetRot = Quaternion.Euler(0f, dice.transform.eulerAngles.y, 0f);

        Quaternion startRot = dice.transform.rotation;

        int numBounces = shouldFullBounce ? 3 : 1;

        float[] bounceHeights = shouldFullBounce ? new float[] { 1.2f, 0.6f, 0.25f } : new float[] { 0.9f };

        float[] bounceDurations = shouldFullBounce ? new float[] { 0.35f, 0.25f, 0.18f } : new float[] { 0.3f };

        float totalBounceDuration = 0f;
        foreach (float bd in bounceDurations) totalBounceDuration += bd;

        float elapsedBounceTime = 0f;
        Vector3 horizontalStart = mainJumpEnd;
        Vector3 horizontalEnd = finalDestination;

        for (int bIndex = 0; bIndex < numBounces; bIndex++)
        {
            float bounceDuration = bounceDurations[bIndex];
            float bounceHeight = bounceHeights[bIndex];
            float bt = 0f;

            while (bt < 1f)
            {
                if (dice == null)
                    yield break;

                bt += Time.deltaTime / bounceDuration;
                elapsedBounceTime += Time.deltaTime;
                float currentBounceT = Mathf.Clamp01(elapsedBounceTime / totalBounceDuration);

                float forwardEase = 1f - Mathf.Pow(1f - currentBounceT, 2f);
                Vector3 horizontalPos = Vector3.Lerp(horizontalStart, horizontalEnd, forwardEase);

                float heightSin = Mathf.Sin(Mathf.Clamp01(bt) * Mathf.PI);
                float currentY = GetBoardSurfaceY() + heightSin * bounceHeight;

                Vector3 pos = new Vector3(horizontalPos.x, currentY, horizontalPos.z);
                dice.transform.position = pos;

                dice.transform.rotation = Quaternion.Slerp(
                    startRot,
                    targetRot,
                    1f - Mathf.Pow(1f - currentBounceT, 3f)
                );

                yield return null;
            }
        }

        if (dice != null)
        {
            dice.transform.position = finalDestination;
            dice.transform.rotation = targetRot;

            dice.rb.position = finalDestination;
            dice.rb.rotation = targetRot;
            Physics.SyncTransforms();

            dice.rb.isKinematic = false;
            dice.rb.linearVelocity = Vector3.zero;
            dice.rb.angularVelocity = Vector3.zero;
            dice.rb.Sleep();
            dice.rb.angularVelocity = Vector3.zero;
            dice.SetCollisionEnabled(true);
            dice.state = DiceState.Idle;

            StartCoroutine(
                RecoverUprightRoutine(dice)
            );
        }
    }
    IEnumerator RecoverUprightRoutine(Dice dice)
    {
        float duration = 0.35f;

        Rigidbody rb = dice.rb;

        float t = 0f;

        Quaternion startRot =
            dice.transform.rotation;

        Quaternion targetRot = Quaternion.Euler(0f, dice.transform.eulerAngles.y, 0f);

        while (t < 1f)
        {
            if (dice == null)
                yield break;

            t += Time.deltaTime / duration;

            dice.transform.rotation = Quaternion.Slerp(startRot, targetRot, 1f - Mathf.Pow(1f - t, 3f));

            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.deltaTime * 8f);

            rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, Time.deltaTime * 10f);

            yield return null;
        }

        dice.transform.rotation = targetRot;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
    #endregion

    #region SEARCH

    public Dice FindNearestSameLevelDice(Dice source)
    {
        Dice nearest = null;

        float best = Mathf.Infinity;

        foreach (Dice d in boardDices)
        {
            if (d == null)
                continue;

            if (d == source)
                continue;

            if (!d.gameObject.activeInHierarchy)
                continue;

            if (d.Level != source.Level)
                continue;

            if (d.state == DiceState.Merging ||
                d.state == DiceState.FlyingCombo)
                continue;

            float dist = Vector3.Distance(source.transform.position, d.transform.position);

            if (dist < best)
            {
                best = dist;
                nearest = d;
            }
        }

        return nearest;
    }

    public float GetBoardSurfaceY()
    {
        if (boardCollider == null)
            return 0.5f;

        return boardCollider.bounds.max.y + 1.5f;
    }

    bool IsOccupied(Vector3 position, Dice ignore)
    {
        Vector3 halfExtents = new Vector3(1f, 0.45f, 1f);

        Collider[] hits = Physics.OverlapBox(
                position,
                halfExtents,
                Quaternion.identity
            );

        foreach (Collider hit in hits)
        {
            Dice d = hit.GetComponent<Dice>();

            if (d == null)
                continue;

            if (d == ignore)
                continue;

            if (!d.gameObject.activeInHierarchy)
                continue;

            return true;
        }

        return false;
    }
    Vector3 FindClearPosition(Vector3 center, Dice ignore = null)
    {
        center.y = GetBoardSurfaceY();

        if (!IsOccupied(center, ignore))
            return center;

        if (boardCollider == null)
            return center;

        Bounds b = boardCollider.bounds;

        for (int ring = 1; ring <= spawnSearchSteps; ring++)
        {
            float radius =
                ring * spawnSearchRadiusStep;

            for (int i = 0; i < 16; i++)
            {
                float angle =
                    i / 16f * Mathf.PI * 2f;

                Vector3 candidate = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

                candidate.x = Mathf.Clamp(candidate.x, b.min.x + spawnPadding, b.max.x - spawnPadding);

                candidate.z = Mathf.Clamp(candidate.z, b.min.z + spawnPadding, b.max.z - spawnPadding);

                candidate.y = GetBoardSurfaceY();

                if (!IsOccupied(candidate, ignore))
                    return candidate;
            }
        }

        return center;
    }

    Vector3 FindRandomClearPositionWithinRadius(Vector3 origin, float maxRadius, Dice ignore = null)
    {
        origin.y = GetBoardSurfaceY();

        if (boardCollider == null)
            return origin;

        Bounds b = boardCollider.bounds;
        Vector3 fallback = origin;
        float bestScore = float.MinValue;

        for (int i = 0; i < 24; i++)
        {
            Vector2 circle = Random.insideUnitCircle.normalized * Random.Range(0.4f, 1f);

            if (circle.sqrMagnitude < 0.001f)
                continue;

            Vector3 candidate = origin + new Vector3(circle.x, 0f, circle.y) * maxRadius;

            candidate.x = Mathf.Clamp(candidate.x, b.min.x + spawnPadding, b.max.x - spawnPadding);

            candidate.z = Mathf.Clamp(candidate.z, b.min.z + spawnPadding, b.max.z - spawnPadding);

            candidate.y = GetBoardSurfaceY();

            if (!IsOccupied(candidate, ignore))
                return candidate;

            float score =
                (candidate - origin).sqrMagnitude;

            if (score > bestScore)
            {
                bestScore = score;
                fallback = candidate;
            }
        }
        return fallback;
    }

    #endregion
}












