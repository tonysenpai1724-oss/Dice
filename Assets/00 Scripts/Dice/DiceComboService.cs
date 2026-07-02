using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DiceComboConfig
{
    public float maxComboDistance = 4f;
    public float comboArcHeight = 4f;
    public float comboDuration = 0.7f;
    public float comboSideScatter = 0.45f;
    public Vector2 comboSpinTurnsX = new Vector2(1265f, 2530f);
    public Vector2 comboSpinTurnsY = new Vector2(210f, 630f);
    public Vector2 comboSpinTurnsZ = new Vector2(1265f, 2530f);
    public float comboDistancePerChain = 4f;
    public float maxComboDistanceLimit = 30f;
    public float comboArcPerChain = 3f;
    public float maxComboArcHeight = 12f;
    public float comboDurationPerChain = 0.03f;
    public float maxComboDuration = 0.75f;
    public float diceSpacingRadius = 0.95f;
}

public class DiceComboService
{
    readonly BoardService boardService;
    readonly DiceComboConfig config;
    readonly Func<List<Dice>> getBoardDices;
    readonly Func<Dice, Dice, bool> tryMerge;
    readonly Action<IEnumerator> runCoroutine;
    readonly Dictionary<Dice, int> comboChainMap = new Dictionary<Dice, int>();
    readonly Dictionary<Dice, float> comboLastTime = new Dictionary<Dice, float>();

    public DiceComboService(
        BoardService boardService,
        DiceComboConfig config,
        Func<List<Dice>> getBoardDices,
        Func<Dice, Dice, bool> tryMerge,
        Action<IEnumerator> runCoroutine)
    {
        this.boardService = boardService;
        this.config = config;
        this.getBoardDices = getBoardDices;
        this.tryMerge = tryMerge;
        this.runCoroutine = runCoroutine;
    }

    public Dictionary<Dice, int> ComboChainMap => comboChainMap;
    public Dictionary<Dice, float> ComboLastTime => comboLastTime;

    public void TryComboChain(Dice dice)
    {
        if (dice == null)
            return;

        Dice target = FindNearestSameLevelDice(dice);

        if (target == null)
        {
            Vector3 randomTargetPos = boardService.FindRandomClearPositionWithinRadius(
                dice.transform.position,
                config.maxComboDistance,
                dice);

            Vector3 randomDir = randomTargetPos - dice.transform.position;
            randomDir.y = 0f;
            randomDir = randomDir.sqrMagnitude < 0.001f ? Vector3.forward : randomDir.normalized;

            runCoroutine?.Invoke(ComboJumpRoutine(dice, null, randomTargetPos, randomDir, true));
            return;
        }

        Vector3 dir = (target.transform.position - dice.transform.position).normalized;
        int comboCount = comboChainMap.TryGetValue(dice, out int chain) ? chain : 1;

        float dynamicMaxComboDistance = Mathf.Min(
            config.maxComboDistance + comboCount * config.comboDistancePerChain,
            config.maxComboDistanceLimit);

        float dist = Vector3.Distance(dice.transform.position, target.transform.position);
        Vector3 targetPos = dist > dynamicMaxComboDistance
            ? dice.transform.position + dir * dynamicMaxComboDistance
            : target.transform.position;

        targetPos.y = boardService.GetBoardSurfaceY();
        runCoroutine?.Invoke(ComboJumpRoutine(dice, target, targetPos, dir, dist > dynamicMaxComboDistance));
    }

    public Dice FindNearestSameLevelDice(Dice source)
    {
        if (source == null)
            return null;

        List<Dice> boardDices = getBoardDices?.Invoke();
        if (boardDices == null)
            return null;

        Dice nearest = null;
        float best = Mathf.Infinity;

        for (int i = 0; i < boardDices.Count; i++)
        {
            Dice dice = boardDices[i];
            if (dice == null || dice == source || !dice.gameObject.activeInHierarchy)
                continue;

            if (dice.Level != source.Level)
                continue;

            if (dice.state == DiceState.Merging || dice.state == DiceState.FlyingCombo)
                continue;

            float dist = Vector3.Distance(source.transform.position, dice.transform.position);
            if (dist < best)
            {
                best = dist;
                nearest = dice;
            }
        }

        return nearest;
    }

    public IEnumerator ComboJumpRoutine(Dice dice, Dice target, Vector3 targetPos, Vector3 dir, bool shouldFullBounce)
    {
        if (dice == null)
            yield break;

        dice.state = DiceState.FlyingCombo;
        dice.canMerge = true;
        //  dice.SetCollisionEnabled(false);
        dice.rb.isKinematic = true;
        //  dice.rb.linearVelocity = Vector3.zero;
        //  dice.rb.angularVelocity = Vector3.zero;

        Vector3 start = dice.transform.position;
        Vector3 finalDestination = targetPos;
        finalDestination.y = boardService.GetBoardSurfaceY();

        bool canAimForTarget =
            target != null &&
            target.gameObject.activeInHierarchy &&
            target.Level == dice.Level &&
            !target.isMerging;

        if (!canAimForTarget)
            finalDestination = boardService.FindClearPosition(finalDestination, dice);

        Vector3 jumpDir = dir.sqrMagnitude > 0.001f ? dir.normalized : Vector3.forward;
        float originalDist = Vector3.Distance(start, finalDestination);
        float actualBounceDistance = Mathf.Min(2.8f, originalDist * 0.55f);

        Vector3 mainJumpEnd = finalDestination - jumpDir * actualBounceDistance;
        mainJumpEnd.y = boardService.GetBoardSurfaceY();

        int comboCount = comboChainMap.TryGetValue(dice, out int currentChain)
            ? currentChain + 1
            : 1;

        comboChainMap[dice] = comboCount;
        comboLastTime[dice] = Time.time;

        Vector3 sideOffset = Vector3.Cross(
            Vector3.up,
            dir.sqrMagnitude > 0.001f ? dir.normalized : Vector3.forward) *
            UnityEngine.Random.Range(-config.comboSideScatter, config.comboSideScatter);

        Vector3 angularSpin = new Vector3(
            UnityEngine.Random.Range(config.comboSpinTurnsX.x, config.comboSpinTurnsX.y),
            UnityEngine.Random.Range(config.comboSpinTurnsY.x, config.comboSpinTurnsY.y),
            UnityEngine.Random.Range(config.comboSpinTurnsZ.x, config.comboSpinTurnsZ.y));

        if (UnityEngine.Random.value < 0.2f)
            angularSpin *= 1.8f;

        float t = 0f;
        float dynamicDuration = Mathf.Min(
            config.comboDuration + comboCount * config.comboDurationPerChain,
            config.maxComboDuration);

        while (t < 1f)
        {
            if (dice == null)
                yield break;

            if (target != null && !target.gameObject.activeInHierarchy)
                break;

            t += Time.deltaTime / dynamicDuration;

            Vector3 pos = Vector3.Lerp(start, mainJumpEnd, t);
            float dynamicArcHeight = Mathf.Min(
                config.comboArcHeight + comboCount * config.comboArcPerChain,
                config.maxComboArcHeight);

            float arc = Mathf.Pow(Mathf.Clamp01(Mathf.Sin(t * Mathf.PI)), 0.7f);
            pos.y += arc * dynamicArcHeight;
            pos += sideOffset * Mathf.Sin(t * Mathf.PI);
            dice.transform.position = pos;

            float spinDamping = 1f - Mathf.Pow(t, 1.8f);
            dice.transform.Rotate(angularSpin * spinDamping * Time.deltaTime, Space.Self);
            yield return null;
        }

        if (target != null && target.gameObject.activeInHierarchy)
        {
            float distToTarget = Vector3.Distance(dice.transform.position, target.transform.position);
            float mergeDistance = Mathf.Max(1.2f, config.diceSpacingRadius * 1.25f);

            if (distToTarget <= mergeDistance && target.Level == dice.Level && !target.isMerging && !dice.isMerging)
            {
                comboChainMap[target] = comboCount;
                if (tryMerge != null && tryMerge.Invoke(dice, target))
                    yield break;
            }
        }

        comboChainMap.Remove(dice);
        if (target != null)
            comboChainMap.Remove(target);

        finalDestination = boardService.FindClearPosition(
            finalDestination,
            dice,
            config.diceSpacingRadius);

        Quaternion targetRot = Quaternion.Euler(0f, dice.transform.eulerAngles.y, 0f);
        Quaternion startRot = dice.transform.rotation;

        int numBounces = shouldFullBounce ? 3 : 1;
        float[] bounceHeights = shouldFullBounce ? new[] { 1.2f, 0.6f, 0.25f } : new[] { 0.9f };
        float[] bounceDurations = shouldFullBounce ? new[] { 0.35f, 0.25f, 0.18f } : new[] { 0.3f };

        float totalBounceDuration = 0f;
        for (int i = 0; i < bounceDurations.Length; i++)
            totalBounceDuration += bounceDurations[i];

        float elapsedBounceTime = 0f;
        Vector3 horizontalStart = mainJumpEnd;
        Vector3 horizontalEnd = finalDestination;

        for (int bounceIndex = 0; bounceIndex < numBounces; bounceIndex++)
        {
            float bounceDuration = bounceDurations[bounceIndex];
            float bounceHeight = bounceHeights[bounceIndex];
            float bounceTime = 0f;

            while (bounceTime < 1f)
            {
                if (dice == null)
                    yield break;

                bounceTime += Time.deltaTime / bounceDuration;
                elapsedBounceTime += Time.deltaTime;
                float currentBounceT = Mathf.Clamp01(elapsedBounceTime / totalBounceDuration);

                float forwardEase = 1f - Mathf.Pow(1f - currentBounceT, 2f);
                Vector3 horizontalPos = Vector3.Lerp(horizontalStart, horizontalEnd, forwardEase);

                float currentY = boardService.GetBoardSurfaceY() + Mathf.Sin(Mathf.Clamp01(bounceTime) * Mathf.PI) * bounceHeight;
                dice.transform.position = new Vector3(horizontalPos.x, currentY, horizontalPos.z);
                dice.transform.rotation = Quaternion.Slerp(startRot, targetRot, 1f - Mathf.Pow(1f - currentBounceT, 3f));
                yield return null;
            }
        }

        dice.transform.position = finalDestination;
        dice.transform.rotation = targetRot;
        dice.rb.position = finalDestination;
        dice.rb.rotation = targetRot;
        dice.SetCollisionEnabled(true);
        dice.ApplyGroundedConstraints();
        dice.rb.linearVelocity = Vector3.zero;
        dice.rb.angularVelocity = Vector3.zero;
        Physics.SyncTransforms();
        dice.rb.isKinematic = false;
        dice.rb.Sleep();
        dice.state = DiceState.Idle;

        runCoroutine?.Invoke(RecoverUprightRoutine(dice));
    }

    public IEnumerator RecoverUprightRoutine(Dice dice)
    {
        float duration = 0.35f;
        Rigidbody rigidbody = dice.rb;
        float t = 0f;
        Quaternion startRot = dice.transform.rotation;
        Quaternion targetRot = Quaternion.Euler(0f, dice.transform.eulerAngles.y, 0f);

        while (t < 1f)
        {
            if (dice == null)
                yield break;

            t += Time.deltaTime / duration;
            dice.transform.rotation = Quaternion.Slerp(startRot, targetRot, 1f - Mathf.Pow(1f - t, 3f));
            rigidbody.linearVelocity = Vector3.Lerp(rigidbody.linearVelocity, Vector3.zero, Time.deltaTime * 8f);
            rigidbody.angularVelocity = Vector3.Lerp(rigidbody.angularVelocity, Vector3.zero, Time.deltaTime * 10f);
            yield return null;
        }

        dice.transform.rotation = targetRot;
        rigidbody.linearVelocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
    }
}




