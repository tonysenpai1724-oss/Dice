
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

    public List<DiceData> diceDatabase;

    [Header("Board")]
    public Collider boardCollider;

    [Header("Start Spawn")]
    public int startSpawnCount = 10;

    public int minStartLevel = 1;
    public int maxStartLevel = 3;
    public float spawnPadding = 1.25f;
    [Range(0.1f, 1f)]
    public float topSpawnPercent = 0.6f;
    public float diceSpacingRadius = 0.95f;
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

    Dice currentHover;
    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        SpawnStartBoard();
    }
    void Update()
    {
        //HandleHover();
    }
    void HandleHover()
    {
        if (Mouse.current == null)
            return;

        Ray ray =
            Camera.main.ScreenPointToRay(
                Mouse.current.position.ReadValue()
            );

        Dice hitDice = null;

        if (
            Physics.Raycast(
                ray,
                out RaycastHit hit
            )
        )
        {
            hitDice =
                hit.collider.GetComponent<Dice>();
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
    #region SPAWN
    [Button]

    public void SpawnStartBoard()
    {
        if (boardCollider == null)
            return;

        Bounds b = boardCollider.bounds;
        float boardY = GetBoardSurfaceY();
        int maxAttempts = startSpawnCount * 12;
        int spawned = 0;
        int attempts = 0;
        float minX =
            Mathf.Min(
                b.min.x + spawnPadding,
                b.max.x - spawnPadding
            );
        float maxX =
            Mathf.Max(
                b.min.x + spawnPadding,
                b.max.x - spawnPadding
            );
        float minZ =
            Mathf.Lerp(
                b.min.z + spawnPadding,
                b.max.z - spawnPadding,
                1f - topSpawnPercent
            );
        float maxZ =
            Mathf.Max(
                b.min.z + spawnPadding,
                b.max.z - spawnPadding
            );

        while (spawned < startSpawnCount &&
            attempts < maxAttempts)
        {
            attempts++;

            Vector3 candidate =
                new Vector3(
                    Random.Range(
                        minX,
                        maxX
                    ),
                    boardY,
                    Random.Range(
                        minZ,
                        maxZ
                    )
                );

            Vector3 pos =
                FindClearPosition(candidate);

            int level =
                Random.Range(
                    minStartLevel,
                    maxStartLevel + 1
                );

            if (IsOccupied(pos, null))
                continue;

            SpawnDice(level, pos);
            spawned++;
        }
    }

    public Dice SpawnDice(
        int level,
        Vector3 pos,
        bool registerOnBoard = true
    )
    {
        Quaternion rotation =
            Quaternion.Euler(
                0,
                Random.Range(0, 360),
                0
            );

        GameObject obj =
            ObjectPool.Instance.Get(
                pos,
                rotation
            );

        Dice d = obj.GetComponent<Dice>();

        d.Setup(
            diceDatabase[level - 1]
        );

        if (registerOnBoard)
        {
            RegisterBoardDice(d);
        }

        return d;
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

    public bool IsBoardStable(
        float velocityThreshold,
        float angularVelocityThreshold
    )
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

    void ReturnBoardDice(
        Dice dice
    )
    {
        if (dice == null)
            return;

        boardDices.Remove(dice);

        ObjectPool.Instance.Return(
            dice.gameObject
        );
    }

    #endregion

    #region MERGE

    // public void TryMerge(
    //     Dice a,
    //     Dice b
    // )
    // {
    //     if (a == null || b == null)
    //         return;

    //     if (a == b)
    //         return;

    //     if (a.state == DiceState.Merging)
    //         return;

    //     if (b.state == DiceState.Merging)
    //         return;
    //     if (!a.gameObject.activeInHierarchy)
    //         return;

    //     if (!b.gameObject.activeInHierarchy)
    //         return;

    //     StartCoroutine(
    //         MergeRoutine(a, b)
    //     );
    // }
    public void TryMerge(
    Dice a,
    Dice b
)
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

        // LOCK IMMEDIATELY
        a.isMerging = true;
        b.isMerging = true;

        StartCoroutine(
            MergeRoutine(a, b)
        );
    }

    IEnumerator MergeRoutine(
        Dice a,
        Dice b
    )
    {
        a.FreezeForMerge();
        b.FreezeForMerge();

        diceQueue.AddDice(
    diceDatabase[a.Level - 1]
);

        diceQueue.AddDice(
            diceDatabase[b.Level - 1]
        );

        Vector3 mergePos =
            (a.transform.position +
            b.transform.position) * 0.5f;
        mergePos.y = GetBoardSurfaceY();

        int nextLevel =
            Mathf.Clamp(
                a.Level + 1,
                1,
                diceDatabase.Count
            );
        int chain = 1;

        if (comboChainMap.ContainsKey(a))
        {
            chain = comboChainMap[a];
        }

        ReturnBoardDice(a);

        ReturnBoardDice(b);


        Dice merged =
            SpawnDice(
                nextLevel,
                FindClearPosition(mergePos)
            );
        comboChainMap[merged] = chain;
        comboChainMap[merged] = 1;

        merged.PlaceUpright(
            merged.transform.position
        );
        // =========================
        // HIT EFFECT
        // =========================

        if (
    merged.data != null &&
    merged.data.hitEffectPrefab != null
)
        {
            Vector3 fxPos =
                merged.transform.position;

            if (
                merged.cachedCollider != null
            )
            {
                fxPos.y =
                    merged.cachedCollider.bounds.min.y +
                    0.02f;
            }

            GameObject fx =
                Instantiate(
                    merged.data.hitEffectPrefab,
                    fxPos,
                    Quaternion.identity
                );

            Destroy(fx, 1f);
        }



        TryComboChain(merged);
        yield break;
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
                FindRandomClearPositionWithinRadius(
                    dice.transform.position,
                    maxComboDistance,
                    dice
                );

            Vector3 randomDir =
                randomTargetPos -
                dice.transform.position;

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
                    randomDir
                )
            );

            return;
        }

        Vector3 dir =
            (
                target.transform.position -
                dice.transform.position
            ).normalized;
        int comboCount = 1;

        if (comboChainMap.ContainsKey(dice))
        {
            comboCount = comboChainMap[dice];
        }

        float dynamicMaxComboDistance =
            Mathf.Min(
                maxComboDistance +
                comboCount * comboDistancePerChain,
                maxComboDistanceLimit
            );

        float dist =
            Vector3.Distance(
                dice.transform.position,
                target.transform.position
            );

        Vector3 targetPos;

        // OUT OF RANGE
        // if (dist > maxComboDistance)
        // {
        //     targetPos =
        //         dice.transform.position +
        //         dir * maxComboDistance;
        // }
        float overshoot = 1f + comboCount * 0.15f;

        if (dist > dynamicMaxComboDistance)
        {
            targetPos =
                dice.transform.position +
                dir * dynamicMaxComboDistance * overshoot;
        }
        else
        {
            // stop BEFORE target center
            targetPos =
                target.transform.position -
                dir * 0.7f;
        }

        StartCoroutine(
            ComboJumpRoutine(
                dice,
                target,
                targetPos,
                dir
            )
        );
    }

    //     IEnumerator ComboJumpRoutine(
    //      Dice dice,
    //      Dice target,
    //      Vector3 targetPos,
    //      Vector3 dir
    //  )
    //     {
    //         if (dice == null)
    //             yield break;

    //         dice.state =
    //             DiceState.FlyingCombo;

    //         dice.canMerge = true;

    //         dice.rb.isKinematic = true;
    //         dice.rb.linearVelocity =
    //             Vector3.zero;
    //         dice.rb.angularVelocity =
    //             Vector3.zero;
    //         dice.SetCollisionEnabled(false);

    //         Vector3 start =
    //             dice.transform.position;
    //         Vector3 finalPos =
    //             targetPos;
    //         Quaternion startRotation =
    //             dice.transform.rotation;
    //         Quaternion endRotation =
    //             startRotation *
    //             Quaternion.Euler(
    //                 Random.Range(
    //                     comboSpinTurnsX.x,
    //                     comboSpinTurnsX.y
    //                 ) * 360f,
    //                 Random.Range(
    //                     comboSpinTurnsY.x,
    //                     comboSpinTurnsY.y
    //                 ) * 360f,
    //                 Random.Range(
    //                     comboSpinTurnsZ.x,
    //                     comboSpinTurnsZ.y
    //                 ) * 360f
    //             );
    //         Vector3 sideOffset =
    //             Vector3.Cross(
    //                 Vector3.up,
    //                 dir.sqrMagnitude > 0.001f
    //                     ? dir.normalized
    //                     : Vector3.forward
    //             ) * Random.Range(
    //                 -comboSideScatter,
    //                 comboSideScatter
    //             );

    //         float t = 0f;

    //         while (t < 1f)
    //         {
    //             if (dice == null)
    //                 yield break;

    //             if (target != null &&
    //                 !target.gameObject.activeInHierarchy)
    //             {
    //                 break;
    //             }

    //             t +=
    //                 Time.deltaTime /
    //                 comboDuration;

    //             Vector3 end =
    //                 targetPos;

    //             finalPos = end;

    //             Vector3 pos =
    //                 Vector3.Lerp(
    //                     start,
    //                     end,
    //                     t
    //                 );

    //             // ARC
    //             pos.y +=
    //                 Mathf.Sin(
    //                     t * Mathf.PI
    //                 ) * comboArcHeight;
    //             pos +=
    //                 sideOffset *
    //                 Mathf.Sin(
    //                     t * Mathf.PI
    //                 );

    //             dice.transform.position = pos;
    //             dice.transform.rotation =
    //                 Quaternion.SlerpUnclamped(
    //                     startRotation,
    //                     endRotation,
    //                     t
    //                 );

    //             yield return null;
    //         }

    //         if (target != null && target.gameObject.activeInHierarchy)
    //         {
    //             float distToTarget =
    //                 Vector3.Distance(
    //                     dice.transform.position,
    //                     target.transform.position
    //                 );

    //             if (distToTarget <= 1f)
    //             {
    //                 TryMerge(
    //                     dice,
    //                     target
    //                 );

    //                 yield break;
    //             }
    //         }

    //         dice.transform.rotation =
    //             endRotation;

    //         // if (Physics.Raycast(
    //         //     finalPos +
    //         //     Vector3.up * 2f,
    //         //     Vector3.down,
    //         //     out RaycastHit hit,
    //         //     5f
    //         // ))
    //         // {
    //         //     finalPos =
    //         //         new Vector3(
    //         //             finalPos.x,
    //         //             hit.point.y + 0.5f,
    //         //             finalPos.z
    //         //         );
    //         // }

    //         // finalPos =
    //         //     FindClearPosition(
    //         //         finalPos,
    //         //         dice
    //         //     );
    //         // dice.PlaceUpright(
    //         //     finalPos
    //         // );

    //         // dice.state =
    //         //     DiceState.Idle;
    //         Collider col =
    //      dice.GetComponent<Collider>();

    //         float bottomOffset =
    //             dice.transform.position.y -
    //             col.bounds.min.y;

    //         if (Physics.Raycast(
    //             finalPos + Vector3.up * 3f,
    //             Vector3.down,
    //             out RaycastHit hit,
    //             10f
    //         ))
    //         {
    //             finalPos.y =
    //                 hit.point.y +
    //                 bottomOffset +
    //                 1f;
    //         }

    //         dice.transform.position = finalPos;

    //         dice.rb.isKinematic = false;

    //         dice.rb.linearVelocity =
    //             Vector3.down * 1.5f;

    //         dice.rb.angularVelocity =
    //             Vector3.zero;

    //         dice.SetCollisionEnabled(true);

    //         dice.state = DiceState.Idle;

    //         StartCoroutine(
    //             RecoverUprightRoutine(dice)
    //         );
    //     }
    IEnumerator ComboJumpRoutine(
        Dice dice,
        Dice target,
        Vector3 targetPos,
        Vector3 dir
    )
    {
        if (dice == null)
            yield break;

        dice.state =
            DiceState.FlyingCombo;

        dice.canMerge = true;

        dice.rb.isKinematic = true;

        dice.rb.linearVelocity =
            Vector3.zero;

        dice.rb.angularVelocity =
            Vector3.zero;

        dice.SetCollisionEnabled(false);

        Vector3 start =
            dice.transform.position;

        Vector3 finalPos =
            targetPos;
        int comboCount = 1;

        if (comboChainMap.ContainsKey(dice))
        {
            comboCount = comboChainMap[dice] + 1;
        }

        comboChainMap[dice] = comboCount;
        comboLastTime[dice] = Time.time;

        // RANDOM SIDE ARC
        Vector3 sideOffset =
            Vector3.Cross(
                Vector3.up,
                dir.sqrMagnitude > 0.001f
                    ? dir.normalized
                    : Vector3.forward
            ) * Random.Range(
                -comboSideScatter,
                comboSideScatter
            );

        // NATURAL SPIN
        Vector3 angularSpin =
     new Vector3(
         Random.Range(1265f, 2530f),
         Random.Range(210f, 630f),
         Random.Range(1265f, 2530f)
     );
        // OCCASIONAL CRAZY SPIN
        if (Random.value < 0.2f)
        {
            angularSpin *= 1.8f;
        }

        float t = 0f;
        float dynamicDuration =
    Mathf.Min(
        comboDuration +
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

            Vector3 end =
                targetPos;

            finalPos = end;

            // POSITION
            Vector3 pos =
                Vector3.Lerp(
                    start,
                    end,
                    t
                );

            // NATURAL ARC
            // =========================
            // COMBO CHAIN
            // =========================



            // if (comboChainMap.ContainsKey(dice))
            // {
            //     comboCount =
            //         comboChainMap[dice] + 1;
            // }

            // comboChainMap[dice] =
            //     comboCount;

            // =========================
            // DYNAMIC ARC
            // =========================

            float dynamicArcHeight =
                Mathf.Min(
                    comboArcHeight +
                    comboCount *
                    comboArcPerChain,
                    maxComboArcHeight
                );

            // NATURAL ARC
            float arc =
     Mathf.Sin(t * Mathf.PI);

            arc = Mathf.Clamp01(arc);

            arc = Mathf.Pow(arc, 0.7f);

            pos.y +=
                arc * dynamicArcHeight;

            // SIDE MOTION
            pos +=
                sideOffset *
                Mathf.Sin(
                    t * Mathf.PI
                );

            dice.transform.position =
                pos;

            // NATURAL SPIN DECAY
            float spinDamping =
                1f -
                Mathf.Pow(
                    t,
                    1.8f
                );

            Vector3 currentSpin =
                angularSpin *
                spinDamping;

            dice.transform.Rotate(
                currentSpin *
                Time.deltaTime,
                Space.Self
            );

            yield return null;
        }

        // TRY MERGE
        if (target != null && target.gameObject.activeInHierarchy)
        {
            float distToTarget =
                Vector3.Distance(
                    dice.transform.position,
                    target.transform.position
                );

            if (distToTarget <= 1f)
            {
                comboChainMap[target] =
    comboCount;

                TryMerge(
                    dice,
                    target
                );
                yield break;
            }
        }
        comboChainMap.Remove(dice);
        if (target != null)
        {
            comboChainMap.Remove(target);
        }


        // GROUND ALIGN
        Collider col =
            dice.GetComponent<Collider>();

        float bottomOffset =
            dice.transform.position.y -
            col.bounds.min.y;

        if (
            Physics.Raycast(
                finalPos + Vector3.up * 3f,
                Vector3.down,
                out RaycastHit hit,
                10f
            )
        )
        {
            finalPos.y =
                11.5f;
        }

        dice.transform.position =
     finalPos;

        dice.rb.position =
            finalPos;

        Physics.SyncTransforms();

        // UPRIGHT RECOVERY
        Quaternion targetRot =
            Quaternion.Euler(
                0f,
                dice.transform.eulerAngles.y,
                0f
            );

        float recover = 0f;

        Quaternion startRot =
            dice.transform.rotation;

        while (recover < 1f)
        {
            recover +=
                Time.deltaTime * 6f;

            float eased =
                1f -
                Mathf.Pow(
                    1f - recover,
                    3f
                );

            dice.transform.rotation =
                Quaternion.Slerp(
                    startRot,
                    targetRot,
                    eased
                );

            yield return null;
        }

        dice.transform.rotation =
            targetRot;

        dice.rb.isKinematic = false;

        dice.rb.linearVelocity =
            Vector3.down * 1.5f;

        dice.rb.angularVelocity =
            Vector3.zero;

        dice.SetCollisionEnabled(true);

        dice.state =
            DiceState.Idle;

        StartCoroutine(
            RecoverUprightRoutine(dice)
        );
    }
    IEnumerator RecoverUprightRoutine(
    Dice dice
)
    {
        float duration = 0.35f;

        Rigidbody rb = dice.rb;

        float t = 0f;

        Quaternion startRot =
            dice.transform.rotation;

        Quaternion targetRot =
            Quaternion.Euler(
                0f,
                dice.transform.eulerAngles.y,
                0f
            );

        while (t < 1f)
        {
            if (dice == null)
                yield break;

            t += Time.deltaTime / duration;

            // smooth rotation
            dice.transform.rotation =
                Quaternion.Slerp(
                    startRot,
                    targetRot,
                    1f - Mathf.Pow(1f - t, 3f)
                );

            // giảm velocity
            rb.linearVelocity =
                Vector3.Lerp(
                    rb.linearVelocity,
                    Vector3.zero,
                    Time.deltaTime * 8f
                );

            rb.angularVelocity =
                Vector3.Lerp(
                    rb.angularVelocity,
                    Vector3.zero,
                    Time.deltaTime * 10f
                );

            yield return null;
        }

        dice.transform.rotation =
            targetRot;

        rb.linearVelocity =
            Vector3.zero;

        rb.angularVelocity =
            Vector3.zero;
    }
    #endregion

    #region SEARCH

    public Dice FindNearestSameLevelDice(
        Dice source
    )
    {
        Dice nearest = null;

        float best =
            Mathf.Infinity;

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

            if (d.state == DiceState.Merging)
                continue;

            float dist =
                Vector3.Distance(
                    source.transform.position,
                    d.transform.position
                );

            if (dist < best)
            {
                best = dist;
                nearest = d;
            }
        }

        return nearest;
    }

    float GetBoardSurfaceY()
    {
        if (boardCollider == null)
            return 0.5f;

        return boardCollider.bounds.max.y + 1f;
    }

    bool IsOccupied(
        Vector3 position,
        Dice ignore
    )
    {
        Collider[] hits =
            Physics.OverlapSphere(
                position,
                diceSpacingRadius
            );

        foreach (Collider hit in hits)
        {
            Dice d =
                hit.GetComponent<Dice>();

            if (d == null)
                continue;

            if (d == ignore)
                continue;

            if (!d.gameObject.activeInHierarchy)
                continue;

            if (d.state == DiceState.Merging)
                continue;

            return true;
        }

        return false;
    }

    Vector3 FindClearPosition(
        Vector3 center,
        Dice ignore = null
    )
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

                Vector3 candidate =
                    center +
                    new Vector3(
                        Mathf.Cos(angle) * radius,
                        0f,
                        Mathf.Sin(angle) * radius
                    );

                candidate.x =
                    Mathf.Clamp(
                        candidate.x,
                        b.min.x + spawnPadding,
                        b.max.x - spawnPadding
                    );

                candidate.z =
                    Mathf.Clamp(
                        candidate.z,
                        b.min.z + spawnPadding,
                        b.max.z - spawnPadding
                    );

                candidate.y =
                    GetBoardSurfaceY();

                if (!IsOccupied(candidate, ignore))
                    return candidate;
            }
        }

        return center;
    }

    Vector3 FindRandomClearPositionWithinRadius(
        Vector3 origin,
        float maxRadius,
        Dice ignore = null
    )
    {
        origin.y = GetBoardSurfaceY();

        if (boardCollider == null)
            return origin;

        Bounds b = boardCollider.bounds;
        Vector3 fallback =
            origin;
        float bestScore = float.MinValue;

        for (int i = 0; i < 24; i++)
        {
            Vector2 circle = Random.insideUnitCircle.normalized * Random.Range(0.4f, 1f);
            // Vector2 circle =
            //     Random.insideUnitCircle;

            if (circle.sqrMagnitude < 0.001f)
                continue;

            Vector3 candidate =
                origin +
                new Vector3(
                    circle.x,
                    0f,
                    circle.y
                ) * maxRadius;

            candidate.x =
                Mathf.Clamp(
                    candidate.x,
                    b.min.x + spawnPadding,
                    b.max.x - spawnPadding
                );

            candidate.z =
                Mathf.Clamp(
                    candidate.z,
                    b.min.z + spawnPadding,
                    b.max.z - spawnPadding
                );

            candidate.y =
                GetBoardSurfaceY();

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

        // return FindClearPosition(
        //     fallback,
        //     ignore
        // );
    }
    #endregion



}
