using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DiceMergeConfig
{
    public float bombExplosionRadius = 10f;
    public float bombExplosionForce = 20f;
    public float bombExplosionMinForce = 15f;
    public GameObject bombExplosionPrefab;
}

public class DiceMergeService
{
    readonly BoardService boardService;
    readonly DiceMergeConfig config;
    readonly DiceQueueManager diceQueue;
    readonly Func<DiceQueueUI> getDiceQueueUI;
    readonly Func<List<Dice>> getBoardDices;
    readonly Func<int, DiceType, DiceData> getDiceData;
    readonly Func<DiceData, Vector3, Dice> spawnDice;
    readonly Action<Dice> returnBoardDice;
    readonly Action<Vector3, string, Color> spawnMergeFloatingText;
    readonly Action<IEnumerator> runCoroutine;
    readonly Action<Dice> continueCombo;
    readonly Dictionary<Dice, int> comboChainMap;
    readonly HashSet<Dice> pendingMergeClaims = new HashSet<Dice>();

    public DiceMergeService(
        BoardService boardService,
        DiceMergeConfig config,
        DiceQueueManager diceQueue,
        Func<DiceQueueUI> getDiceQueueUI,
        Func<List<Dice>> getBoardDices,
        Func<int, DiceType, DiceData> getDiceData,
        Func<DiceData, Vector3, Dice> spawnDice,
        Action<Dice> returnBoardDice,
        Action<Vector3, string, Color> spawnMergeFloatingText,
        Action<IEnumerator> runCoroutine,
        Action<Dice> continueCombo,
        Dictionary<Dice, int> comboChainMap)
    {
        this.boardService = boardService;
        this.config = config;
        this.diceQueue = diceQueue;
        this.getDiceQueueUI = getDiceQueueUI;
        this.getBoardDices = getBoardDices;
        this.getDiceData = getDiceData;
        this.spawnDice = spawnDice;
        this.returnBoardDice = returnBoardDice;
        this.spawnMergeFloatingText = spawnMergeFloatingText;
        this.runCoroutine = runCoroutine;
        this.continueCombo = continueCombo;
        this.comboChainMap = comboChainMap;
    }

    public bool TryMerge(Dice a, Dice b)
    {
        if (a == null || b == null)
            return false;

        if (a == b || a.isMerging || b.isMerging)
            return false;

        if (!a.gameObject.activeInHierarchy || !b.gameObject.activeInHierarchy)
            return false;

        if (pendingMergeClaims.Contains(a) || pendingMergeClaims.Contains(b))
            return false;

        if (a.Level != b.Level)
            return false;

        if (a.state == DiceState.Merging || b.state == DiceState.Merging)
            return false;

        pendingMergeClaims.Add(a);
        pendingMergeClaims.Add(b);

        a.isMerging = true;
        b.isMerging = true;
        runCoroutine?.Invoke(MergeRoutine(a, b));
        return true;
    }

    public IEnumerator MergeRoutine(Dice a, Dice b)
    {
        try
        {
            if (a == null || b == null)
                yield break;

            a.FreezeForMerge();
            b.FreezeForMerge();

            Vector3 visualMergePos = (a.transform.position + b.transform.position) * 0.5f;
            Vector3 mergePos = visualMergePos;
            mergePos.y = boardService.GetBoardSurfaceY();

            EnqueueMergedDice(a.data, b.data, a.transform.position, b.transform.position, visualMergePos);

            int chain = comboChainMap.TryGetValue(a, out int chainValue) ? chainValue : 1;

            returnBoardDice?.Invoke(a);
            returnBoardDice?.Invoke(b);

            DiceData bombData = GetBombDiceData(a.data, b.data);
            if (bombData != null)
                ExplodeBoardDice(mergePos, bombData);

            DiceData nextData = getDiceData?.Invoke(a.Level + 1, DiceType.Normal);
            if (nextData == null)
            {
                Debug.LogError($"Khong tim thay data Level {a.Level + 1} Type {a.type}");
                yield break;
            }

            Vector3 spawnPos = boardService.FindClearPosition(mergePos);
            Dice merged = spawnDice?.Invoke(nextData, spawnPos);
            if (merged == null)
                yield break;

            spawnMergeFloatingText?.Invoke(merged.transform.position, merged.data.level.ToString(), merged.data.diceColor);

            if (merged.data != null && merged.data.hitEffectPrefab != null)
            {
                Vector3 fxPos = merged.transform.position;
                if (merged.cachedCollider != null)
                    fxPos.y = 1.5f;

                GameObject fx = UnityEngine.Object.Instantiate(merged.data.hitEffectPrefab, fxPos, Quaternion.identity);
                UnityEngine.Object.Destroy(fx, 1f);
            }

            comboChainMap[merged] = chain;
            merged.PlaceUpright(merged.transform.position);
            continueCombo?.Invoke(merged);
        }
        finally
        {
            if (a != null)
                pendingMergeClaims.Remove(a);

            if (b != null)
                pendingMergeClaims.Remove(b);
        }
    }

    DiceData GetBombDiceData(DiceData first, DiceData second)
    {
        if (first != null && first.type == DiceType.Bomb)
            return first;

        if (second != null && second.type == DiceType.Bomb)
            return second;

        return null;
    }

    void EnqueueDice(DiceData data, Vector3 mergePosition)
    {
        DiceQueueUI diceQueueUI = getDiceQueueUI?.Invoke();
        if (diceQueueUI != null)
        {
            diceQueueUI.AddDice(data, mergePosition);
            return;
        }

        diceQueue?.AddDice(data, mergePosition);
    }

    void EnqueueMergedDice(DiceData firstData, DiceData secondData, Vector3 firstPosition, Vector3 secondPosition, Vector3 fallbackPosition)
    {
        DiceQueueUI diceQueueUI = getDiceQueueUI?.Invoke();
        if (diceQueueUI != null)
        {
            Camera camera = diceQueueUI.worldCamera != null ? diceQueueUI.worldCamera : Camera.main;
            if (camera == null)
            {
                diceQueueUI.AddDice(firstData, fallbackPosition);
                diceQueueUI.AddDice(secondData, fallbackPosition);
                return;
            }

            Vector2 firstScreenPosition = RectTransformUtility.WorldToScreenPoint(camera, firstPosition);
            Vector2 secondScreenPosition = RectTransformUtility.WorldToScreenPoint(camera, secondPosition);
            Vector2 mergeScreenPosition = (firstScreenPosition + secondScreenPosition) * 0.5f;

            if (diceQueueUI.debugSpawnPosition)
            {
                Debug.Log(
                    $"[DiceMergeService Spawn Debug] first={GetDebugName(firstData)} second={GetDebugName(secondData)} " +
                    $"camera={camera.name} screenSize={Screen.width}x{Screen.height} " +
                    $"firstWorld={firstPosition} secondWorld={secondPosition} fallbackWorld={fallbackPosition} " +
                    $"firstScreen={firstScreenPosition} secondScreen={secondScreenPosition} mergeScreen={mergeScreenPosition}");
            }

            diceQueueUI.AddDiceFromScreenPosition(firstData, mergeScreenPosition);
            diceQueueUI.AddDiceFromScreenPosition(secondData, mergeScreenPosition);
            return;
        }

        diceQueue?.AddDice(firstData, fallbackPosition);
        diceQueue?.AddDice(secondData, fallbackPosition);
    }

    string GetDebugName(UnityEngine.Object target)
    {
        return target != null ? target.name : "null";
    }

    void ExplodeBoardDice(Vector3 position, DiceData sourceData)
    {
        GameObject prefab = sourceData != null && sourceData.hitEffectPrefab != null
            ? sourceData.hitEffectPrefab
            : config.bombExplosionPrefab;

        if (prefab != null)
        {
            GameObject fx = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
            UnityEngine.Object.Destroy(fx, 1f);
        }

        float radius = Mathf.Max(0.01f, config.bombExplosionRadius);
        List<Dice> boardDices = getBoardDices?.Invoke();
        if (boardDices == null)
            return;

        for (int i = boardDices.Count - 1; i >= 0; i--)
        {
            Dice dice = boardDices[i];
            if (dice == null)
                continue;

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
            float impulse = Mathf.Max(config.bombExplosionMinForce, config.bombExplosionForce * forceMultiplier);
            Vector3 force = direction.normalized * impulse;

            dice.ApplyBoardMoveConstraints();
            dice.rb.WakeUp();
            dice.rb.AddForce(force, ForceMode.VelocityChange);
        }
    }
}


