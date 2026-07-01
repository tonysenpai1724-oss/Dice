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
            List<EnemySpawnPlacement> generatedPlacements = BuildPlacements(level.GetWaveEnemyDatas(waveIndex));
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
            List<EnemySpawnPlacement> generatedPlacements = BuildBottomRowPlacements(level.GetWaveEnemyDatas(waveIndex));
            wave.enemySpawnPlacements = new List<EnemySpawnPlacement>(generatedPlacements);

            if (previewPlacements.Count == 0)
                previewPlacements = new List<EnemySpawnPlacement>(generatedPlacements);
        }

#if UNITY_EDITOR
        EditorUtility.SetDirty(level);
#endif
    }

    public List<EnemySpawnPlacement> BuildPlacements(List<EnemyData> enemyDatas)
    {
        List<EnemySpawnPlacement> placements = new();
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

        int rows = Mathf.Max(1, gridRows);
        int columns = Mathf.Max(2, gridColumns);
        int minEnemyColumn = Mathf.Clamp(playerColumn + 1, 1, columns - 1);
        int rangedStartColumn = Mathf.Clamp(5, minEnemyColumn, columns - 1);
        int nextMeleeColumn = AddGridPlacements(placements, rangeEnemies, rangedStartColumn, minEnemyColumn, true, rows, -1);

        if (meleeEnemies.Count > 0)
        {
            int meleeStartColumn = Mathf.Clamp(nextMeleeColumn, minEnemyColumn, columns - 1);
            AddGridPlacements(placements, meleeEnemies, meleeStartColumn, minEnemyColumn, false, rows, -1);
        }

        return placements.Count > 0 ? placements : BuildFallbackPlacements(enemyDatas);
    }

    int AddGridPlacements(List<EnemySpawnPlacement> placements, List<EnemyData> enemiesToPlace, int startColumn, int minColumn, bool isBackRow, int rows, int reservedFirstColumnRow)
    {
        if (enemiesToPlace == null || enemiesToPlace.Count == 0)
            return startColumn - 1;

        int column = startColumn;
        int slotIndex = 0;
        bool bossPlaced = false;
        int bossRow = GetBossSpawnRow(rows);

        for (int i = 0; i < enemiesToPlace.Count; i++)
        {
            EnemyData data = enemiesToPlace[i];

            if (column < minColumn)
                break;

            if (IsBossEnemy(data) && !bossPlaced)
            {
                placements.Add(CreatePlacement(data, GetGridAreaLocalPosition(bossRow, column), isBackRow, bossRow, column));
                bossPlaced = true;

                if (rows == 1)
                {
                    column--;
                    slotIndex = 0;
                }

                continue;
            }

            int reservedRow = reservedFirstColumnRow;
            if (bossPlaced && column == startColumn)
                reservedRow = bossRow;

            int row = GetNextAvailableRow(rows, reservedRow, slotIndex);

            if (row < 0)
            {
                column--;
                slotIndex = 0;

                if (column < minColumn)
                    break;

                reservedRow = -1;
                row = GetNextAvailableRow(rows, reservedRow, slotIndex);
            }

            placements.Add(CreatePlacement(data, GetGridAreaLocalPosition(row, column), isBackRow, row, column));
            slotIndex++;

            int capacity = GetColumnCapacity(rows, reservedRow);
            if (slotIndex >= capacity)
            {
                column--;
                slotIndex = 0;
            }
        }

        return column;
    }

    int GetNextAvailableRow(int rows, int reservedRow, int slotIndex)
    {
        for (int row = 0; row < rows; row++)
        {
            if (row == reservedRow)
                continue;

            if (slotIndex == 0)
                return row;

            slotIndex--;
        }

        return -1;
    }

    int GetColumnCapacity(int rows, int reservedRow)
    {
        int capacity = rows;
        if (reservedRow >= 0 && reservedRow < rows)
            capacity--;

        return Mathf.Max(0, capacity);
    }

    public List<EnemySpawnPlacement> BuildBottomRowPlacements(List<EnemyData> enemyDatas)
    {
        List<EnemySpawnPlacement> placements = new();
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

        for (int i = 0; i < validDatas.Count; i++)
        {
            EnemyData data = validDatas[i];
            if (IsBossEnemy(data))
            {
                int rows = Mathf.Max(1, gridRows);
                int columns = Mathf.Max(2, gridColumns);
                int minEnemyColumn = Mathf.Clamp(playerColumn + 1, 1, columns - 1);
                int row = GetBossSpawnRow(rows);
                int column = GetBossSpawnColumn(columns, minEnemyColumn);
                placements.Add(CreatePlacement(data, GetGridAreaLocalPosition(row, column), false, row, column));
                continue;
            }

            bool isMelee = data != null && data.type != EnemyType.Range;
            placements.Add(CreatePlacement(data, GetBottomRowFallback(i, validDatas.Count, isMelee), false));
        }

        return placements;
    }

    EnemySpawnPlacement CreatePlacement(EnemyData data, Vector3 position, bool isBackRow, int gridRow = 0, int gridColumn = 0)
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
            gridColumn = gridColumn
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

    int GetBossSpawnColumn(int columns, int minEnemyColumn)
    {
        return Mathf.Clamp(columns / 2, minEnemyColumn, Mathf.Max(minEnemyColumn, columns - 1));
    }

    List<EnemySpawnPlacement> BuildFallbackPlacements(List<EnemyData> enemyDatas)
    {
        List<EnemySpawnPlacement> placements = new();
        for (int i = 0; i < enemyDatas.Count; i++)
        {
            EnemyData data = enemyDatas[i];
            if (data == null)
                continue;

            Vector3 position = IsBossEnemy(data)
                ? Vector3.zero
                : new Vector3(Random.Range(-fallbackSpacing, fallbackSpacing), 0f, 0f);

            placements.Add(CreatePlacement(data, position, false));
        }

        return placements;
    }

    List<EnemySpawnPlacement> BuildBottomRowFallbackPlacements(List<EnemyData> enemyDatas)
    {
        List<EnemySpawnPlacement> placements = new();
        if (enemyDatas == null)
            return placements;

        int validCount = 0;
        for (int i = 0; i < enemyDatas.Count; i++)
        {
            if (enemyDatas[i] != null)
                validCount++;
        }

        int index = 0;
        for (int i = 0; i < enemyDatas.Count; i++)
        {
            EnemyData data = enemyDatas[i];
            if (data == null)
                continue;

            if (IsBossEnemy(data))
            {
                placements.Add(CreatePlacement(data, Vector3.zero, false));
                continue;
            }

            float x01 = validCount <= 1 ? 0.5f : (float)index / (validCount - 1);
            index++;
            placements.Add(CreatePlacement(data, new Vector3(Mathf.Lerp(-fallbackSpacing, fallbackSpacing, x01), 0f, 0f), false));
        }

        return placements;
    }
}



