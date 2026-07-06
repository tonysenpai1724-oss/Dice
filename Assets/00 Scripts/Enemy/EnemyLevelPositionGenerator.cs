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
    public bool useWorldPosition;
    public bool isBackRow;
    public int gridRow;
    public int gridColumn;
    public int entryIndex = -1;
}

public class EnemyLevelPositionGenerator : MonoBehaviour
{
    [Header("Refs")]
    public EnemySpawnArea spawnArea;
    public Level previewLevel;
    public List<Level> levelsToGenerate = new();

    [Header("Preview")]
    [ReadOnly]
    public List<EnemySpawnPlacement> previewPlacements = new();

    [Header("Fallback")]
    public int gridRows = 3;
    public int gridColumns = 6;
    public int playerColumn = 0;
    public int playerRow = 1;
    public int meleeStartColumn = 4;
    public int rangeStartColumn = 5;
    public float fallbackSpacing = 120f;
    public float minSpacing = 80f;
    public int maxRandomAttempts = 24;
    [Range(0f, 1f)]
    public float bottomRowPercent = 0.08f;

    [Button("Generate Level")]
    public void GenerateToLevelUI()
    {
        GenerateLevel(previewLevel);
    }

    [Button("Generate Bottom Row Level")]
    public void GenerateBottomRowLevel()
    {
        GenerateBottomRow(previewLevel);
    }

    [Button("Generate All Levels")]
    public void GenerateToAllLevelsUI()
    {
        if (levelsToGenerate == null || levelsToGenerate.Count == 0)
        {
            GenerateToLevelUI();
            return;
        }

        for (int i = 0; i < levelsToGenerate.Count; i++)
            GenerateLevel(levelsToGenerate[i]);

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

        previewPlacements.Clear();

        for (int waveIndex = 0; waveIndex < level.WaveCount; waveIndex++)
        {
            LevelWaveData wave = level.waves[waveIndex];
            if (wave == null)
                continue;

            wave.enemySpawnPlacements.Clear();
            List<EnemySpawnPlacement> generatedPlacements = BuildPlacements(level.GetWaveEnemyEntries(waveIndex));
            wave.enemySpawnPlacements = new List<EnemySpawnPlacement>(generatedPlacements);

            if (previewPlacements.Count == 0)
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

        previewPlacements.Clear();

        for (int waveIndex = 0; waveIndex < level.WaveCount; waveIndex++)
        {
            LevelWaveData wave = level.waves[waveIndex];
            if (wave == null)
                continue;

            wave.enemySpawnPlacements.Clear();
            List<EnemySpawnPlacement> generatedPlacements = BuildBottomRowPlacements(level.GetWaveEnemyEntries(waveIndex));
            wave.enemySpawnPlacements = new List<EnemySpawnPlacement>(generatedPlacements);

            if (previewPlacements.Count == 0)
                previewPlacements = new List<EnemySpawnPlacement>(generatedPlacements);
        }

#if UNITY_EDITOR
        EditorUtility.SetDirty(level);
#endif
    }

    public List<EnemySpawnPlacement> BuildPlacements(List<EnemyEntryConfig> enemyEntries)
    {
        List<EnemySpawnPlacement> placements = new();
        if (enemyEntries == null)
            return placements;

        if (spawnArea == null || !spawnArea.HasValidArea)
            return BuildFallbackPlacements(enemyEntries);

        List<EnemyEntryConfig> meleeEntries = new();
        List<EnemyEntryConfig> rangeEntries = new();

        for (int i = 0; i < enemyEntries.Count; i++)
        {
            EnemyEntryConfig entry = enemyEntries[i];
            EnemyData data = entry != null ? entry.Data : null;
            if (data == null)
                continue;

            if (data.type == EnemyType.Range)
                rangeEntries.Add(entry);
            else
                meleeEntries.Add(entry);
        }

        int rows = Mathf.Max(1, gridRows);
        int columns = Mathf.Max(2, gridColumns);
        int minEnemyColumn = Mathf.Clamp(playerColumn + 1, 1, columns - 1);
        int frontColumn = Mathf.Clamp(meleeStartColumn, minEnemyColumn, columns - 1);
        int backColumn = Mathf.Clamp(rangeStartColumn, frontColumn, columns - 1);

        AddOrderedPlacements(placements, meleeEntries, frontColumn, false, enemyEntries);
        AddOrderedPlacements(placements, rangeEntries, backColumn, true, enemyEntries);

        return placements.Count > 0 ? placements : BuildFallbackPlacements(enemyEntries);
    }

    void AddOrderedPlacements(List<EnemySpawnPlacement> placements, List<EnemyEntryConfig> entries, int column, bool isBackRow, List<EnemyEntryConfig> sourceEntries)
    {
        if (entries == null)
            return;

        int rows = Mathf.Max(1, gridRows);
        for (int i = 0; i < entries.Count; i++)
        {
            EnemyEntryConfig entry = entries[i];
            EnemyData data = entry != null ? entry.Data : null;
            if (data == null)
                continue;

            int row = rows <= 1 ? 0 : Mathf.Clamp(i, 0, rows - 1);
            int entryIndex = sourceEntries != null ? sourceEntries.IndexOf(entry) : -1;
            placements.Add(CreatePlacement(data, GetGridAreaLocalPosition(row, column), isBackRow, row, column, entryIndex));
        }
    }

    public List<EnemySpawnPlacement> BuildBottomRowPlacements(List<EnemyEntryConfig> enemyEntries)
    {
        List<EnemySpawnPlacement> placements = new();
        if (enemyEntries == null)
            return placements;

        if (spawnArea == null || !spawnArea.HasValidArea)
            return BuildBottomRowFallbackPlacements(enemyEntries);

        List<EnemyEntryConfig> validEntries = new();
        for (int i = 0; i < enemyEntries.Count; i++)
        {
            if (enemyEntries[i] != null && enemyEntries[i].Data != null)
                validEntries.Add(enemyEntries[i]);
        }

        for (int i = 0; i < validEntries.Count; i++)
        {
            EnemyEntryConfig entry = validEntries[i];
            EnemyData data = entry.Data;
            int entryIndex = enemyEntries.IndexOf(entry);
            if (IsBossEnemy(data))
            {
                int rows = Mathf.Max(1, gridRows);
                int columns = Mathf.Max(2, gridColumns);
                int minEnemyColumn = Mathf.Clamp(playerColumn + 1, 1, columns - 1);
                int row = GetBossSpawnRow(rows);
                int column = GetBossSpawnColumn(columns, minEnemyColumn);
                placements.Add(CreatePlacement(data, GetGridAreaLocalPosition(row, column), false, row, column, entryIndex));
                continue;
            }

            bool isMelee = data.type != EnemyType.Range;
            placements.Add(CreatePlacement(data, GetBottomRowFallback(i, validEntries.Count, isMelee), false, 0, 0, entryIndex));
        }

        return placements;
    }

    EnemySpawnPlacement CreatePlacement(EnemyData data, Vector3 position, bool isBackRow, int gridRow = 0, int gridColumn = 0, int entryIndex = -1)
    {
        bool useWorldPosition = spawnArea != null && spawnArea.spawnSpace == EnemySpawnSpace.World;
        return new EnemySpawnPlacement
        {
            data = data,
            position = position,
            useUIPosition = !useWorldPosition,
            useWorldPosition = useWorldPosition,
            isBackRow = isBackRow,
            gridRow = gridRow,
            gridColumn = gridColumn,
            entryIndex = entryIndex,
        };
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

    Vector3 GetBottomRowFallback(int index, int count, bool isMelee)
    {
        float x01 = count <= 1 ? 0.5f : (float)index / (count - 1);
        x01 = isMelee ? Mathf.Lerp(0.60f, 0.98f, x01) : Mathf.Lerp(0.05f, 0.55f, x01);
        return spawnArea.GetPoint(x01, Mathf.Clamp01(bottomRowPercent));
    }

    bool IsBossEnemy(EnemyData data)
    {
        return data != null && data.enemyLevel == EnemyLevel.Boss;
    }

    int GetBossSpawnRow(int rows)
    {
        return Mathf.Clamp(rows / 2, 0, Mathf.Max(0, rows - 1));
    }

    public int GetPlayerSpawnRow()
    {
        return Mathf.Clamp(playerRow, 0, Mathf.Max(0, gridRows - 1));
    }

    public int GetPlayerSpawnColumn()
    {
        return Mathf.Clamp(playerColumn, 0, Mathf.Max(0, gridColumns - 1));
    }

    int GetBossSpawnColumn(int columns, int minEnemyColumn)
    {
        return Mathf.Clamp(columns / 2, minEnemyColumn, Mathf.Max(minEnemyColumn, columns - 1));
    }

    List<EnemySpawnPlacement> BuildFallbackPlacements(List<EnemyEntryConfig> enemyEntries)
    {
        List<EnemySpawnPlacement> placements = new();
        for (int i = 0; i < enemyEntries.Count; i++)
        {
            EnemyEntryConfig entry = enemyEntries[i];
            EnemyData data = entry != null ? entry.Data : null;
            if (data == null)
                continue;

            Vector3 position = IsBossEnemy(data)
                ? Vector3.zero
                : new Vector3(Random.Range(-fallbackSpacing, fallbackSpacing), 0f, 0f);

            placements.Add(CreatePlacement(data, position, false, 0, 0, i));
        }

        return placements;
    }

    List<EnemySpawnPlacement> BuildBottomRowFallbackPlacements(List<EnemyEntryConfig> enemyEntries)
    {
        List<EnemySpawnPlacement> placements = new();
        if (enemyEntries == null)
            return placements;

        int validCount = 0;
        for (int i = 0; i < enemyEntries.Count; i++)
        {
            if (enemyEntries[i] != null && enemyEntries[i].Data != null)
                validCount++;
        }

        int index = 0;
        for (int i = 0; i < enemyEntries.Count; i++)
        {
            EnemyEntryConfig entry = enemyEntries[i];
            EnemyData data = entry != null ? entry.Data : null;
            if (data == null)
                continue;

            if (IsBossEnemy(data))
            {
                placements.Add(CreatePlacement(data, Vector3.zero, false, 0, 0, i));
                continue;
            }

            float x01 = validCount <= 1 ? 0.5f : (float)index / (validCount - 1);
            index++;
            placements.Add(CreatePlacement(data, new Vector3(Mathf.Lerp(-fallbackSpacing, fallbackSpacing, x01), 0f, 0f), false, 0, 0, i));
        }

        return placements;
    }
}
