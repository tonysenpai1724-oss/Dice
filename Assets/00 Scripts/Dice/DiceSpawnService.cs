using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct DiceStartSpawnSettings
{
    public int minStartSpawnCount;
    public int maxStartSpawnCount;
    public float maxSingleDiceShare;
    public int minStartLevel;
    public int maxStartLevel;
    public float diceSpacingRadius;
}

public struct DiceHeroSpawnSettings
{
    public bool animateHeroStartDice;
    public Transform heroDiceSpawnPoint;
    public Vector3 heroDiceSpawnOffset;
    public float heroDiceSpawnStartDelay;
    public float heroDiceFlyDuration;
    public float heroDiceFlyArcHeight;
    public float heroDiceSpawnStagger;
    public Vector3 heroDiceFlySpin;
}

public class DiceSpawnService
{
    readonly BoardService boardService;
    readonly Func<DiceStartSpawnSettings> getStartSpawnSettings;
    readonly Func<DiceHeroSpawnSettings> getHeroSpawnSettings;
    readonly Func<int, DiceType, DiceData> getDiceData;
    readonly Func<PlayerController> getPlayerController;
    readonly Func<DiceData, Vector3, bool, Dice> spawnDice;
    readonly Action<Dice> registerBoardDice;
    readonly Func<IEnumerator, Coroutine> runCoroutine;
    readonly Action<bool> setHeroSpawnState;

    public DiceSpawnService(
        BoardService boardService,
        Func<DiceStartSpawnSettings> getStartSpawnSettings,
        Func<DiceHeroSpawnSettings> getHeroSpawnSettings,
        Func<int, DiceType, DiceData> getDiceData,
        Func<PlayerController> getPlayerController,
        Func<DiceData, Vector3, bool, Dice> spawnDice,
        Action<Dice> registerBoardDice,
        Func<IEnumerator, Coroutine> runCoroutine,
        Action<bool> setHeroSpawnState)
    {
        this.boardService = boardService;
        this.getStartSpawnSettings = getStartSpawnSettings;
        this.getHeroSpawnSettings = getHeroSpawnSettings;
        this.getDiceData = getDiceData;
        this.getPlayerController = getPlayerController;
        this.spawnDice = spawnDice;
        this.registerBoardDice = registerBoardDice;
        this.runCoroutine = runCoroutine;
        this.setHeroSpawnState = setHeroSpawnState;
    }

    public void SpawnStartBoard()
    {
        if (boardService == null)
        {
            Debug.LogError("BoardService is null!");
            return;
        }

        DiceStartSpawnSettings startSettings = getStartSpawnSettings != null
            ? getStartSpawnSettings()
            : default(DiceStartSpawnSettings);

        int targetSpawnCount = UnityEngine.Random.Range(
            startSettings.minStartSpawnCount,
            startSettings.maxStartSpawnCount + 1);

        List<DiceData> plannedStartDice = BuildBalancedStartDicePlan(targetSpawnCount, startSettings);
        List<Vector3> plannedPositions = boardService.BuildSpreadSpawnPositions(targetSpawnCount);

        int spawnCount = Mathf.Min(plannedStartDice.Count, plannedPositions.Count);
        for (int i = 0; i < spawnCount; i++)
        {
            DiceData data = plannedStartDice[i];
            if (data == null)
                continue;

            Vector3 position = boardService.FindClearPosition(
                plannedPositions[i],
                null,
                startSettings.diceSpacingRadius);

            if (boardService.IsOccupied(position, null, startSettings.diceSpacingRadius))
                continue;

            spawnDice?.Invoke(data, position, true);
        }

        SpawnPlayerStartDiceDatas(targetSpawnCount, startSettings);
    }

    List<DiceData> BuildBalancedStartDicePlan(int targetSpawnCount, DiceStartSpawnSettings startSettings)
    {
        List<DiceData> result = new List<DiceData>();
        List<DiceData> normalCandidates = new List<DiceData>();

        for (int level = startSettings.minStartLevel; level <= startSettings.maxStartLevel; level++)
        {
            DiceData data = getDiceData != null ? getDiceData(level, DiceType.Normal) : null;
            if (data != null)
                normalCandidates.Add(data);
        }

        if (normalCandidates.Count == 0)
            return result;

        Dictionary<DiceData, int> counts = new Dictionary<DiceData, int>();
        int maxPerDice = Mathf.Max(1, Mathf.FloorToInt(targetSpawnCount * startSettings.maxSingleDiceShare));

        List<DiceData> shuffled = new List<DiceData>(normalCandidates);
        for (int i = 0; i < shuffled.Count; i++)
        {
            int swapIndex = UnityEngine.Random.Range(i, shuffled.Count);
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
            result.Add(shuffled[UnityEngine.Random.Range(0, shuffled.Count)]);

        return result;
    }

    void SpawnPlayerStartDiceDatas(int targetSpawnCount, DiceStartSpawnSettings startSettings)
    {
        PlayerController player = getPlayerController != null ? getPlayerController() : null;
        if (player == null)
            return;

        if (player.diceDatas == null || player.diceDatas.Count == 0)
            player.InitializeDiceDatas();

        if (player.diceDatas == null || boardService == null)
            return;

        DiceHeroSpawnSettings heroSettings = getHeroSpawnSettings != null
            ? getHeroSpawnSettings()
            : default(DiceHeroSpawnSettings);

        if (heroSettings.animateHeroStartDice && runCoroutine != null)
        {
            setHeroSpawnState?.Invoke(true);
            runCoroutine(SpawnPlayerStartDiceDatasRoutine(player, targetSpawnCount, startSettings, heroSettings));
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
                Vector3 clearPos = boardService.FindClearPosition(position, null, startSettings.diceSpacingRadius);

                if (!boardService.IsOccupied(clearPos, null, startSettings.diceSpacingRadius))
                {
                    spawnDice?.Invoke(data, clearPos, true);
                    break;
                }
            }
        }
    }

    IEnumerator SpawnPlayerStartDiceDatasRoutine(
        PlayerController player,
        int targetSpawnCount,
        DiceStartSpawnSettings startSettings,
        DiceHeroSpawnSettings heroSettings)
    {
        try
        {
            if (heroSettings.heroDiceSpawnStartDelay > 0f)
                yield return new WaitForSeconds(heroSettings.heroDiceSpawnStartDelay);

            List<Coroutine> flyRoutines = new List<Coroutine>();
            List<Vector3> reservedPositions = new List<Vector3>();

            for (int i = 0; i < player.diceDatas.Count; i++)
            {
                DiceData data = player.diceDatas[i];
                if (data == null)
                    continue;

                if (!TryGetHeroDiceBoardPosition(targetSpawnCount, reservedPositions, startSettings, out Vector3 targetPosition))
                    continue;

                reservedPositions.Add(targetPosition);

                Dice dice = spawnDice != null ? spawnDice(data, targetPosition, false) : null;
                if (dice == null)
                    continue;

                Vector3 startPosition = GetHeroDiceSpawnPosition(player, targetPosition, heroSettings);
                Coroutine flyRoutine = runCoroutine != null
                    ? runCoroutine(FlyAndRegisterHeroDice(dice, startPosition, targetPosition, heroSettings))
                    : null;

                if (flyRoutine != null)
                    flyRoutines.Add(flyRoutine);
            }

            for (int i = 0; i < flyRoutines.Count; i++)
            {
                if (flyRoutines[i] != null)
                    yield return flyRoutines[i];
            }
        }
        finally
        {
            setHeroSpawnState?.Invoke(false);
        }
    }

    IEnumerator FlyAndRegisterHeroDice(
        Dice dice,
        Vector3 startPosition,
        Vector3 targetPosition,
        DiceHeroSpawnSettings heroSettings)
    {
        yield return FlyHeroDiceToBoard(dice, startPosition, targetPosition, heroSettings);
        registerBoardDice?.Invoke(dice);
    }

    bool TryGetHeroDiceBoardPosition(
        int targetSpawnCount,
        List<Vector3> reservedPositions,
        DiceStartSpawnSettings startSettings,
        out Vector3 clearPosition)
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
            clearPosition = boardService.FindClearPosition(position, null, startSettings.diceSpacingRadius);

            if (!boardService.IsOccupied(clearPosition, null, startSettings.diceSpacingRadius) &&
                !IsReservedHeroDicePosition(clearPosition, reservedPositions, startSettings.diceSpacingRadius))
            {
                return true;
            }
        }

        return false;
    }

    static bool IsReservedHeroDicePosition(Vector3 position, List<Vector3> reservedPositions, float diceSpacingRadius)
    {
        if (reservedPositions == null)
            return false;

        float minDistance = diceSpacingRadius * 2f;
        float minDistanceSqr = minDistance * minDistance;

        for (int i = 0; i < reservedPositions.Count; i++)
        {
            Vector3 offset = position - reservedPositions[i];
            offset.y = 0f;

            if (offset.sqrMagnitude < minDistanceSqr)
                return true;
        }

        return false;
    }

    static Vector3 GetHeroDiceSpawnPosition(
        PlayerController player,
        Vector3 targetPosition,
        DiceHeroSpawnSettings heroSettings)
    {
        if (heroSettings.heroDiceSpawnPoint != null)
            return heroSettings.heroDiceSpawnPoint.position;

        if (player != null)
            return player.transform.position + heroSettings.heroDiceSpawnOffset;

        return targetPosition + Vector3.up * heroSettings.heroDiceFlyArcHeight;
    }

    static IEnumerator FlyHeroDiceToBoard(
        Dice dice,
        Vector3 startPosition,
        Vector3 targetPosition,
        DiceHeroSpawnSettings heroSettings)
    {
        if (dice == null)
            yield break;

        dice.state = DiceState.FlyingCombo;
        dice.canMerge = false;
        dice.SetCollisionEnabled(false);
        dice.transform.position = startPosition;
        dice.transform.rotation = UnityEngine.Random.rotation;

        if (dice.rb != null)
        {
            dice.rb.linearVelocity = Vector3.zero;
            dice.rb.angularVelocity = Vector3.zero;
            dice.rb.isKinematic = true;
            dice.rb.position = startPosition;
            dice.rb.rotation = dice.transform.rotation;
        }

        float duration = Mathf.Max(0.01f, heroSettings.heroDiceFlyDuration);
        Quaternion startRotation = dice.transform.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            Vector3 position = Vector3.Lerp(startPosition, targetPosition, easedT);
            position.y += Mathf.Sin(t * Mathf.PI) * heroSettings.heroDiceFlyArcHeight;

            dice.transform.position = position;
            dice.transform.rotation = startRotation * Quaternion.Euler(heroSettings.heroDiceFlySpin * t);

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
}
