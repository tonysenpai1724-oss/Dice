
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;

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
    public Transform point;
    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        SpawnStartBoard();
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
        Vector3 pos
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

        return d;
    }

    #endregion

    #region MERGE

    public void TryMerge(
        Dice a,
        Dice b
    )
    {
        if (a == null || b == null)
            return;

        if (a == b)
            return;

        if (a.state == DiceState.Merging)
            return;

        if (b.state == DiceState.Merging)
            return;
        if (!a.gameObject.activeInHierarchy)
            return;

        if (!b.gameObject.activeInHierarchy)
            return;

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

        ObjectPool.Instance.Return(
            a.gameObject
        );

        ObjectPool.Instance.Return(
            b.gameObject
        );

        Dice merged =
            SpawnDice(
                nextLevel,
                FindClearPosition(mergePos)
            );

        merged.PlaceUpright(
            merged.transform.position
        );

        TryComboChain(merged);
        yield break;
    }

    #endregion

    #region COMBO
    public float maxComboDistance = 4f;
    // void TryComboChain(Dice dice)
    // {
    //     if (dice == null)
    //         return;

    //     Dice target =
    //         FindNearestSameLevelDice(dice);

    //     // NO TARGET
    //     if (target == null)
    //     {
    //         dice.state = DiceState.Idle;

    //         dice.rb.linearVelocity =
    //             Vector3.zero;

    //         dice.rb.angularVelocity =
    //             Vector3.zero;

    //         return;
    //     }

    //     Vector3 targetPos =
    //         target.transform.position;

    //     float dist =
    //         Vector3.Distance(
    //             dice.transform.position,
    //             targetPos
    //         );

    //     // LIMIT RANGE
    //     if (dist > maxComboDistance)
    //     {
    //         Vector3 dir =
    //             (targetPos -
    //             dice.transform.position)
    //             .normalized;

    //         targetPos =
    //             dice.transform.position +
    //             dir * maxComboDistance;
    //     }

    //     StartCoroutine(
    //         ComboJumpRoutine(
    //             dice,
    //             target,
    //             targetPos
    //         )
    //     );
    // }
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
                    maxComboDistance * 0.5f,
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

        float dist =
            Vector3.Distance(
                dice.transform.position,
                target.transform.position
            );

        Vector3 targetPos;

        // OUT OF RANGE
        if (dist > maxComboDistance)
        {
            targetPos =
                dice.transform.position +
                dir * maxComboDistance;
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
    // IEnumerator ComboJumpRoutine(
    //     Dice dice,
    //     Dice target, Vector3 targetPos
    // )
    // {
    //     if (dice == null || target == null)
    //         yield break;

    //     dice.state =
    //         DiceState.FlyingCombo;
    //     dice.canMerge = true;

    //     Vector3 start =
    //         dice.transform.position;

    //     Vector3 end = targetPos;

    //     float t = 0f;

    //     while (t < 1f)
    //     {
    //         if (dice == null || target == null)
    //             yield break;

    //         t +=
    //             Time.deltaTime /
    //             comboDuration;

    //         Vector3 pos =
    //             Vector3.Lerp(
    //                 start,
    //                 end,
    //                 t
    //             );

    //         pos.y +=
    //             Mathf.Sin(
    //                 t * Mathf.PI
    //             ) * comboArcHeight;

    //         dice.transform.position = pos;

    //         yield return null;
    //     }

    //     dice.state = DiceState.Idle;

    //     if (target != null)
    //     {
    //         float distToTarget =
    //             Vector3.Distance(
    //                 dice.transform.position,
    //                 target.transform.position
    //             );

    //         // close enough
    //         if (distToTarget <= 1f)
    //         {
    //             TryMerge(dice, target);
    //         }
    //     }
    // }
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
        Quaternion startRotation =
            dice.transform.rotation;
        Quaternion endRotation =
            startRotation *
            Quaternion.Euler(
                Random.Range(
                    comboSpinTurnsX.x,
                    comboSpinTurnsX.y
                ) * 360f,
                Random.Range(
                    comboSpinTurnsY.x,
                    comboSpinTurnsY.y
                ) * 360f,
                Random.Range(
                    comboSpinTurnsZ.x,
                    comboSpinTurnsZ.y
                ) * 360f
            );
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

        float t = 0f;

        while (t < 1f)
        {
            if (dice == null)
                yield break;

            if (target != null &&
                !target.gameObject.activeInHierarchy)
            {
                break;
            }

            t +=
                Time.deltaTime /
                comboDuration;

            Vector3 end =
                targetPos;

            finalPos = end;

            Vector3 pos =
                Vector3.Lerp(
                    start,
                    end,
                    t
                );

            // ARC
            pos.y +=
                Mathf.Sin(
                    t * Mathf.PI
                ) * comboArcHeight;
            pos +=
                sideOffset *
                Mathf.Sin(
                    t * Mathf.PI
                );

            dice.transform.position = pos;
            dice.transform.rotation =
                Quaternion.SlerpUnclamped(
                    startRotation,
                    endRotation,
                    t
                );

            yield return null;
        }

        if (target != null && target.gameObject.activeInHierarchy)
        {
            float distToTarget =
                Vector3.Distance(
                    dice.transform.position,
                    target.transform.position
                );

            if (distToTarget <= 1f)
            {
                TryMerge(
                    dice,
                    target
                );

                yield break;
            }
        }

        dice.transform.rotation =
            endRotation;

        if (Physics.Raycast(
            finalPos +
            Vector3.up * 2f,
            Vector3.down,
            out RaycastHit hit,
            5f
        ))
        {
            finalPos =
                new Vector3(
                    finalPos.x,
                    hit.point.y + 0.5f,
                    finalPos.z
                );
        }

        finalPos =
            FindClearPosition(
                finalPos,
                dice
            );
        dice.PlaceUpright(
            finalPos
        );

        dice.state =
            DiceState.Idle;
    }
    #endregion

    #region SEARCH

    public Dice FindNearestSameLevelDice(
        Dice source
    )
    {
        Dice[] all =
            FindObjectsOfType<Dice>();

        Dice nearest = null;

        float best =
            Mathf.Infinity;

        foreach (Dice d in all)
        {
            if (d == source)
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
            Vector2 circle =
                Random.insideUnitCircle;

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

        return FindClearPosition(
            fallback,
            ignore
        );
    }
    #endregion

}
