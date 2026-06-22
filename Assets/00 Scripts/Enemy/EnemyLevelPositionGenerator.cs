using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class EnemySpawnPlacement
{
    public EnemyData data;
    public Vector3 position;
    public bool useUIPosition;
    public bool isBackRow;
    public int gridRow;
    public int gridColumn;
}

public class EnemyLevelPositionGenerator : MonoBehaviour
{
    [Header("Refs")]
    public EnemySpawnArea spawnArea;
    public Level previewLevel;
    public List<Level> levelsToGenerate = new();

    [Header("Preview")]
    [ReadOnly]
    public List<EnemySpawnPlacement> previewPlacements =
        new();

    [Header("Fallback")]
    public int gridRows = 3;
    public int gridColumns = 6;
    public int playerColumn = 0;
    public int meleeStartColumn = 4;
    public int rangeStartColumn = 5;
    public float fallbackSpacing = 120f;
    public float minSpacing = 80f;
    public int maxRandomAttempts = 24;
    [Range(0f, 1f)]
    public float bottomRowPercent = 0.08f;

    [Button("Generate UI Level")]
    public void GenerateToLevelUI()
    {
        GenerateLevel(previewLevel);
    }

    [Button("Generate Bottom Row Level")]
    public void GenerateBottomRowLevel()
    {
        GenerateBottomRow(previewLevel);
    }

    [Button("Generate UI All Levels")]
    public void GenerateToAllLevelsUI()
    {
        if (levelsToGenerate == null || levelsToGenerate.Count == 0)
        {
            GenerateToLevelUI();
            return;
        }

        for (int i = 0; i < levelsToGenerate.Count; i++)
        {
            GenerateLevel(levelsToGenerate[i]);
        }

#if UNITY_EDITOR
        AssetDatabase.SaveAssets();
#endif
    }

    void GenerateLevel(Level level)
    {
        if (level == null)
        {
            previewPlacements.Clear();
            return;
        }

        if (level.WaveCount <= 1)
        {
            level.ClearEnemySpawnPlacements();
            previewPlacements = BuildPlacements(level.GetWaveEnemyDatas(0));
            level.enemySpawnPlacements = new List<EnemySpawnPlacement>(previewPlacements);
#if UNITY_EDITOR
            EditorUtility.SetDirty(level);
#endif
            return;
        }

        previewPlacements.Clear();

        for (int waveIndex = 0; waveIndex < level.WaveCount; waveIndex++)
        {
            LevelWaveData wave = level.waves[waveIndex];
            if (wave == null)
                continue;

            List<EnemySpawnPlacement> generatedPlacements = BuildPlacements(level.GetWaveEnemyDatas(waveIndex));
            wave.enemySpawnPlacements = new List<EnemySpawnPlacement>(generatedPlacements);

            if (waveIndex == 0)
                previewPlacements = new List<EnemySpawnPlacement>(generatedPlacements);
        }

#if UNITY_EDITOR
        EditorUtility.SetDirty(level);
#endif
    }

    void GenerateBottomRow(Level level)
    {
        if (level == null)
        {
            previewPlacements.Clear();
            return;
        }

        if (level.WaveCount <= 1)
        {
            level.ClearEnemySpawnPlacements();
            previewPlacements = BuildBottomRowPlacements(level.GetWaveEnemyDatas(0));
            level.enemySpawnPlacements = new List<EnemySpawnPlacement>(previewPlacements);
#if UNITY_EDITOR
            EditorUtility.SetDirty(level);
#endif
            return;
        }

        previewPlacements.Clear();

        for (int waveIndex = 0; waveIndex < level.WaveCount; waveIndex++)
        {
            LevelWaveData wave = level.waves[waveIndex];
            if (wave == null)
                continue;

            List<EnemySpawnPlacement> generatedPlacements = BuildBottomRowPlacements(level.GetWaveEnemyDatas(waveIndex));
            wave.enemySpawnPlacements = new List<EnemySpawnPlacement>(generatedPlacements);

            if (waveIndex == 0)
                previewPlacements = new List<EnemySpawnPlacement>(generatedPlacements);
        }

#if UNITY_EDITOR
        EditorUtility.SetDirty(level);
#endif
    }

    public List<EnemySpawnPlacement> BuildPlacements(
        List<EnemyData> enemyDatas
    )
    {
        List<EnemySpawnPlacement> placements =
            new();

        if (enemyDatas == null)
            return placements;

        if (spawnArea == null || !spawnArea.HasValidArea)
            return BuildFallbackPlacements(enemyDatas);

        List<EnemyData> meleeEnemies = new();
        List<EnemyData> rangeEnemies = new();

        for (int i = 0; i < enemyDatas.Count; i++)
        {
            EnemyData data = enemyDatas[i];
            if (data == null)
                continue;

            if (data.type == EnemyType.Range)
                rangeEnemies.Add(data);
            else
                meleeEnemies.Add(data);
        }

        AddGridPlacements(
            placements,
            meleeEnemies,
            Mathf.Clamp(meleeStartColumn, playerColumn + 1, gridColumns - 1),
            false
        );

        AddGridPlacements(
            placements,
            rangeEnemies,
            Mathf.Clamp(rangeStartColumn, playerColumn + 1, gridColumns - 1),
            true
        );

        if (placements.Count == 0)
            return BuildFallbackPlacements(enemyDatas);

        return placements;
    }

    void AddGridPlacements(
        List<EnemySpawnPlacement> placements,
        List<EnemyData> enemiesToPlace,
        int startColumn,
        bool isBackRow
    )
    {
        if (enemiesToPlace == null || enemiesToPlace.Count == 0)
            return;

        int rows = Mathf.Max(1, gridRows);
        int columns = Mathf.Max(2, gridColumns);
        int minEnemyColumn = Mathf.Clamp(playerColumn + 1, 1, columns - 1);
        int middleRow = rows / 2;
        int centerColumn = Mathf.Clamp(startColumn, minEnemyColumn, columns - 1);
        HashSet<string> occupiedCells = new HashSet<string>();
        List<EnemyData> remainingEnemies = new List<EnemyData>();

        for (int i = 0; i < placements.Count; i++)
        {
            EnemySpawnPlacement existing = placements[i];
            if (existing == null)
                continue;

            occupiedCells.Add($"{existing.gridRow}:{existing.gridColumn}");
        }

        for (int i = 0; i < enemiesToPlace.Count; i++)
        {
            EnemyData data = enemiesToPlace[i];
            if (data != null && data.enemyLevel == EnemyLevel.Boss)
            {
                FindAvailableGridCell(occupiedCells, middleRow, centerColumn, centerColumn, columns - 1, rows, out int bossRow, out int bossColumn);
                occupiedCells.Add($"{bossRow}:{bossColumn}");
                placements.Add(
                    new EnemySpawnPlacement
                    {
                        data = data,
                        position = GetGridAreaLocalPosition(bossRow, bossColumn),
                        useUIPosition = true,
                        isBackRow = isBackRow,
                        gridRow = bossRow,
                        gridColumn = bossColumn
                    }
                );
                continue;
            }

            remainingEnemies.Add(data);
        }

        for (int i = 0; i < remainingEnemies.Count; i++)
        {
            EnemyData data = remainingEnemies[i];
            int preferredRow = i % rows;
            int columnOffset = i / rows;
            int preferredColumn = Mathf.Max(minEnemyColumn, startColumn - columnOffset);
            FindAvailableGridCell(occupiedCells, preferredRow, preferredColumn, minEnemyColumn, columns - 1, rows, out int row, out int column);
            occupiedCells.Add($"{row}:{column}");

            placements.Add(
                new EnemySpawnPlacement
                {
                    data = data,
                    position = GetGridAreaLocalPosition(row, column),
                    useUIPosition = true,
                    isBackRow = isBackRow,
                    gridRow = row,
                    gridColumn = column
                }
            );
        }
    }

    void FindAvailableGridCell(
        HashSet<string> occupiedCells,
        int preferredRow,
        int preferredColumn,
        int minColumn,
        int maxColumn,
        int rows,
        out int row,
        out int column
    )
    {
        row = Mathf.Clamp(preferredRow, 0, rows - 1);
        column = Mathf.Clamp(preferredColumn, minColumn, maxColumn);

        if (!occupiedCells.Contains($"{row}:{column}"))
            return;

        for (int columnOffset = 0; columnOffset <= maxColumn - minColumn; columnOffset++)
        {
            int candidateColumn = Mathf.Clamp(preferredColumn - columnOffset, minColumn, maxColumn);

            for (int rowOffset = 0; rowOffset < rows; rowOffset++)
            {
                int candidateRow = (preferredRow + rowOffset) % rows;
                string key = $"{candidateRow}:{candidateColumn}";
                if (occupiedCells.Contains(key))
                    continue;

                row = candidateRow;
                column = candidateColumn;
                return;
            }
        }

        for (int candidateColumn = maxColumn; candidateColumn >= minColumn; candidateColumn--)
        {
            for (int candidateRow = 0; candidateRow < rows; candidateRow++)
            {
                string key = $"{candidateRow}:{candidateColumn}";
                if (occupiedCells.Contains(key))
                    continue;

                row = candidateRow;
                column = candidateColumn;
                return;
            }
        }
    }

    public List<EnemySpawnPlacement> BuildBottomRowPlacements(
        List<EnemyData> enemyDatas
    )
    {
        List<EnemySpawnPlacement> placements =
            new();

        if (enemyDatas == null)
            return placements;

        if (spawnArea == null || !spawnArea.HasValidArea)
            return BuildBottomRowFallbackPlacements(enemyDatas);

        List<EnemyData> validDatas = new();
        for (int i = 0; i < enemyDatas.Count; i++)
        {
            if (enemyDatas[i] != null)
                validDatas.Add(enemyDatas[i]);
        }

        if (validDatas.Count == 0)
            return placements;

        for (int i = 0; i < validDatas.Count; i++)
        {
            EnemyData data = validDatas[i];
            bool isMelee = data != null && data.type != EnemyType.Range;
            Vector3 position = GetBottomRowPosition(i, validDatas.Count, isMelee);

            placements.Add(
                new EnemySpawnPlacement
                {
                    data = data,
                    position = position,
                    useUIPosition = true,
                    isBackRow = false
                }
            );
        }

        return placements;
    }

    Vector3 GetGridAreaLocalPosition(int row, int column)
    {
        if (spawnArea == null || !spawnArea.HasValidArea)
            return Vector3.zero;

        int rows = Mathf.Max(1, gridRows);
        int columns = Mathf.Max(2, gridColumns);

        float x01 = columns <= 1 ? 1f : (float)Mathf.Clamp(column, 0, columns - 1) / (columns - 1);
        float y01 = rows <= 1 ? 0.5f : 1f - ((float)Mathf.Clamp(row, 0, rows - 1) / (rows - 1));
        return spawnArea.GetPoint(x01, y01);
    }

    Vector3 GetBottomRowPosition(int index, int count, bool isMelee)
    {
        for (int attempt = 0; attempt < Mathf.Max(1, maxRandomAttempts); attempt++)
        {
            Vector3 candidate = GetRandomBottomRowBand(isMelee);
            if (IsFarEnough(candidate, previewPlacements))
                return candidate;
        }

        return GetBottomRowFallback(index, count, isMelee);
    }

    Vector3 GetRandomBottomRowBand(bool isMelee)
    {
        float xMin = isMelee ? 0.60f : 0.05f;
        float xMax = isMelee ? 0.98f : 0.55f;

        return spawnArea.GetPoint(
            Random.Range(xMin, xMax),
            Mathf.Clamp01(bottomRowPercent)
        );
    }

    Vector3 GetBottomRowFallback(
        int index,
        int count,
        bool isMelee
    )
    {
        float x01 =
            count <= 1
                ? 0.5f
                : (float)index / (count - 1);

        x01 = isMelee
            ? Mathf.Lerp(0.60f, 0.98f, x01)
            : Mathf.Lerp(0.05f, 0.55f, x01);

        return spawnArea.GetPoint(x01, Mathf.Clamp01(bottomRowPercent));
    }

    Vector3 GetRandomPosition(
        List<EnemySpawnPlacement> currentPlacements,
        bool isBackRow,
        bool isMelee
    )
    {
        for (int attempt = 0; attempt < Mathf.Max(1, maxRandomAttempts); attempt++)
        {
            Vector3 candidate =
                isBackRow
                    ? GetRandomBackRowBand(isMelee)
                    : GetRandomFrontRowBand(isMelee);

            if (IsFarEnough(candidate, currentPlacements))
                return candidate;
        }

        return isBackRow
            ? GetBackRowFallback(currentPlacements.Count, currentPlacements.Count + 1, isMelee)
            : GetFrontRowFallback(currentPlacements.Count, currentPlacements.Count + 1, isMelee);
    }

    Vector3 GetRandomFrontRowBand(bool isMelee)
    {
        float xMin = isMelee ? 0.60f : 0.05f;
        float xMax = isMelee ? 0.98f : 0.55f;

        return spawnArea.GetPoint(
            Random.Range(xMin, xMax),
            Random.Range(
                Mathf.Max(0f, spawnArea.frontRowPercent - 0.12f),
                Mathf.Min(1f, spawnArea.frontRowPercent + 0.12f)
            )
        );
    }

    Vector3 GetRandomBackRowBand(bool isMelee)
    {
        float xMin = isMelee ? 0.60f : 0.05f;
        float xMax = isMelee ? 0.98f : 0.55f;

        return spawnArea.GetPoint(
            Random.Range(xMin, xMax),
            Random.Range(
                Mathf.Max(0f, spawnArea.backRowPercent - 0.12f),
                Mathf.Min(1f, spawnArea.backRowPercent + 0.12f)
            )
        );
    }

    Vector3 GetFrontRowFallback(
        int index,
        int count,
        bool isMelee
    )
    {
        float x01 =
            count <= 1
                ? 0.5f
                : (float)index / (count - 1);

        x01 = isMelee
            ? Mathf.Lerp(0.60f, 0.98f, x01)
            : Mathf.Lerp(0.05f, 0.55f, x01);

        return spawnArea.GetPoint(x01, spawnArea.frontRowPercent);
    }

    Vector3 GetBackRowFallback(
        int index,
        int count,
        bool isMelee
    )
    {
        float x01 =
            count <= 1
                ? 0.5f
                : (float)index / (count - 1);

        x01 = isMelee
            ? Mathf.Lerp(0.60f, 0.98f, x01)
            : Mathf.Lerp(0.05f, 0.55f, x01);

        return spawnArea.GetPoint(x01, spawnArea.backRowPercent);
    }

    bool IsFarEnough(
        Vector3 candidate,
        List<EnemySpawnPlacement> currentPlacements
    )
    {
        if (currentPlacements == null || currentPlacements.Count == 0)
            return true;

        float minSpacingSqr = minSpacing * minSpacing;

        for (int i = 0; i < currentPlacements.Count; i++)
        {
            EnemySpawnPlacement placement = currentPlacements[i];
            if (placement == null)
                continue;

            if ((placement.position - candidate).sqrMagnitude < minSpacingSqr)
                return false;
        }

        return true;
    }

    List<EnemySpawnPlacement> BuildFallbackPlacements(
        List<EnemyData> enemyDatas
    )
    {
        List<EnemySpawnPlacement> placements =
            new();

        for (int i = 0; i < enemyDatas.Count; i++)
        {
            EnemyData data = enemyDatas[i];
            if (data == null)
                continue;

            placements.Add(
                new EnemySpawnPlacement
                {
                    data = data,
                    position = new Vector3(
                        Random.Range(-fallbackSpacing, fallbackSpacing),
                        0f,
                        0f
                    ),
                    useUIPosition = true,
                    isBackRow = false
                }
            );
        }

        return placements;
    }

    List<EnemySpawnPlacement> BuildBottomRowFallbackPlacements(
        List<EnemyData> enemyDatas
    )
    {
        List<EnemySpawnPlacement> placements =
            new();

        if (enemyDatas == null)
            return placements;

        int validCount = 0;
        for (int i = 0; i < enemyDatas.Count; i++)
        {
            if (enemyDatas[i] != null)
                validCount++;
        }

        if (validCount == 0)
            return placements;

        int index = 0;
        for (int i = 0; i < enemyDatas.Count; i++)
        {
            EnemyData data = enemyDatas[i];
            if (data == null)
                continue;

            float x01 =
                validCount <= 1
                    ? 0.5f
                    : (float)index / (validCount - 1);
            index++;

            placements.Add(
                new EnemySpawnPlacement
                {
                    data = data,
                    position = new Vector3(
                        Mathf.Lerp(-fallbackSpacing, fallbackSpacing, x01),
                        0f,
                        0f
                    ),
                    useUIPosition = true,
                    isBackRow = false
                }
            );
        }

        return placements;
    }
}


