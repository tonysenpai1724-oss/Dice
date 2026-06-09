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

        level.ClearEnemySpawnPlacements();
        previewPlacements =
            BuildPlacements(level.enemyDatas);

        level.enemySpawnPlacements =
            new List<EnemySpawnPlacement>(previewPlacements);

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

        level.ClearEnemySpawnPlacements();
        previewPlacements =
            BuildBottomRowPlacements(level.enemyDatas);

        level.enemySpawnPlacements =
            new List<EnemySpawnPlacement>(previewPlacements);

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

        List<EnemyData> meleeRow = new();
        List<EnemyData> rangeRow = new();

        for (int i = 0; i < enemyDatas.Count; i++)
        {
            EnemyData data = enemyDatas[i];
            if (data == null)
                continue;

            if (data.type == EnemyType.Range)
                rangeRow.Add(data);
            else
                meleeRow.Add(data);
        }

        AddRowPlacements(
            placements,
            meleeRow,
            false
        );

        AddRowPlacements(
            placements,
            rangeRow,
            true
        );

        if (placements.Count == 0)
            return BuildFallbackPlacements(enemyDatas);

        return placements;
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
            float x01 =
                validDatas.Count <= 1
                    ? 0.5f
                    : (float)i / (validDatas.Count - 1);

            Vector3 position =
                spawnArea.GetPoint(
                    x01,
                    bottomRowPercent
                );

            placements.Add(
                new EnemySpawnPlacement
                {
                    data = validDatas[i],
                    position = position,
                    useUIPosition = true,
                    isBackRow = false
                }
            );
        }

        return placements;
    }

    void AddRowPlacements(
        List<EnemySpawnPlacement> placements,
        List<EnemyData> row,
        bool isBackRow
    )
    {
        if (row == null || row.Count == 0)
            return;

        for (int i = 0; i < row.Count; i++)
        {
            EnemyData data = row[i];
            Vector3 position =
                GetRandomPlacementPosition(
                    placements,
                    isBackRow,
                    data != null && data.type != EnemyType.Range
                );

            placements.Add(
                new EnemySpawnPlacement
                {
                    data = data,
                    position = position,
                    useUIPosition = true,
                    isBackRow = isBackRow
                }
            );
        }
    }

    Vector3 GetRandomPlacementPosition(
        List<EnemySpawnPlacement> currentPlacements,
        bool isBackRow,
        bool isMelee
    )
    {
        if (spawnArea == null)
            return Vector3.zero;

        for (int attempt = 0; attempt < maxRandomAttempts; attempt++)
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
