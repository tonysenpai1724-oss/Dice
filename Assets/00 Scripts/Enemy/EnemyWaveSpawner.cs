using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyWaveSpawner
{
    readonly Func<Level> getCurrentLevel;
    readonly Func<int> getCurrentWaveIndex;
    readonly Func<Enemy> getEnemyPrefab;
    readonly Func<Transform> getCombatSpaceRoot;
    readonly Func<Transform> getEnemyRoot;
    readonly Func<Transform> getFallbackRoot;
    readonly Func<EnemyLevelPositionGenerator> getSpawnPositionGenerator;
    readonly Func<int> getPlayerColumn;
    readonly Func<int> getGridRows;
    readonly Func<int> getGridColumns;
    readonly Func<int> getMeleeSpawnColumn;
    readonly Func<int> getRangeSpawnColumn;
    readonly Func<float> getEnemySpacing;
    readonly Func<bool> isChestLevel;
    readonly Func<EnemyData, bool> isChestEnemy;
    readonly Func<int> getChestSpawnRow;
    readonly Func<int> getChestSpawnColumn;
    readonly Func<List<Enemy>> getEnemies;
    readonly Action clearEnemies;
    readonly Action snapPlayerToConfiguredGrid;
    readonly Action<Enemy> registerEnemy;
    readonly Action<Enemy, Vector3, bool, bool> setEnemyPosition;
    readonly Action<Enemy> setEnemyGridFromCurrentPosition;
    readonly Action<Enemy, int, int> setEnemyGrid;
    readonly Action<Enemy> snapEnemyToGridPosition;
    readonly Action cleanupEnemies;

    public EnemyWaveSpawner(
        Func<Level> getCurrentLevel,
        Func<int> getCurrentWaveIndex,
        Func<Enemy> getEnemyPrefab,
        Func<Transform> getCombatSpaceRoot,
        Func<Transform> getEnemyRoot,
        Func<Transform> getFallbackRoot,
        Func<EnemyLevelPositionGenerator> getSpawnPositionGenerator,
        Func<int> getPlayerColumn,
        Func<int> getGridRows,
        Func<int> getGridColumns,
        Func<int> getMeleeSpawnColumn,
        Func<int> getRangeSpawnColumn,
        Func<float> getEnemySpacing,
        Func<bool> isChestLevel,
        Func<EnemyData, bool> isChestEnemy,
        Func<int> getChestSpawnRow,
        Func<int> getChestSpawnColumn,
        Func<List<Enemy>> getEnemies,
        Action clearEnemies,
        Action snapPlayerToConfiguredGrid,
        Action<Enemy> registerEnemy,
        Action<Enemy, Vector3, bool, bool> setEnemyPosition,
        Action<Enemy> setEnemyGridFromCurrentPosition,
        Action<Enemy, int, int> setEnemyGrid,
        Action<Enemy> snapEnemyToGridPosition,
        Action cleanupEnemies)
    {
        this.getCurrentLevel = getCurrentLevel;
        this.getCurrentWaveIndex = getCurrentWaveIndex;
        this.getEnemyPrefab = getEnemyPrefab;
        this.getCombatSpaceRoot = getCombatSpaceRoot;
        this.getEnemyRoot = getEnemyRoot;
        this.getFallbackRoot = getFallbackRoot;
        this.getSpawnPositionGenerator = getSpawnPositionGenerator;
        this.getPlayerColumn = getPlayerColumn;
        this.getGridRows = getGridRows;
        this.getGridColumns = getGridColumns;
        this.getMeleeSpawnColumn = getMeleeSpawnColumn;
        this.getRangeSpawnColumn = getRangeSpawnColumn;
        this.getEnemySpacing = getEnemySpacing;
        this.isChestLevel = isChestLevel;
        this.isChestEnemy = isChestEnemy;
        this.getChestSpawnRow = getChestSpawnRow;
        this.getChestSpawnColumn = getChestSpawnColumn;
        this.getEnemies = getEnemies;
        this.clearEnemies = clearEnemies;
        this.snapPlayerToConfiguredGrid = snapPlayerToConfiguredGrid;
        this.registerEnemy = registerEnemy;
        this.setEnemyPosition = setEnemyPosition;
        this.setEnemyGridFromCurrentPosition = setEnemyGridFromCurrentPosition;
        this.setEnemyGrid = setEnemyGrid;
        this.snapEnemyToGridPosition = snapEnemyToGridPosition;
        this.cleanupEnemies = cleanupEnemies;
    }

    public void SpawnCurrentWave(bool clearExisting)
    {
        if (clearExisting)
            clearEnemies?.Invoke();

        Level currentLevel = getCurrentLevel?.Invoke();
        Enemy enemyPrefab = getEnemyPrefab?.Invoke();
        if (currentLevel == null || enemyPrefab == null)
            return;

        snapPlayerToConfiguredGrid?.Invoke();

        int currentWaveIndex = getCurrentWaveIndex();
        List<EnemyEntryConfig> waveEntries = currentLevel.GetWaveEnemyEntries(currentWaveIndex);
        List<EnemyData> waveEnemyDatas = currentLevel.GetWaveEnemyDatas(currentWaveIndex);
        List<EnemySpawnPlacement> wavePlacements = currentLevel.GetWaveSpawnPlacements(currentWaveIndex);

        Transform root = getCombatSpaceRoot?.Invoke()
            ?? getEnemyRoot?.Invoke()
            ?? getFallbackRoot?.Invoke();
        List<EnemySpawnPlacement> placements = wavePlacements != null && wavePlacements.Count > 0
            ? wavePlacements
            : getSpawnPositionGenerator?.Invoke() != null
                ? getSpawnPositionGenerator().BuildPlacements(waveEntries)
                : null;

        List<Enemy> enemies = getEnemies?.Invoke();
        int playerColumn = getPlayerColumn();
        int gridColumns = getGridColumns();
        float enemySpacing = getEnemySpacing();

        if (placements != null && placements.Count > 0)
        {
            List<IndexedEnemySpawnPlacement> spawnOrderedPlacements = BuildTopToBottomSpawnOrder(placements);

            for (int i = 0; i < spawnOrderedPlacements.Count; i++)
            {
                EnemySpawnPlacement placement = spawnOrderedPlacements[i].placement;
                if (placement == null || placement.data == null)
                    continue;

                Enemy enemy = UnityEngine.Object.Instantiate(enemyPrefab, root, false);
                registerEnemy?.Invoke(enemy);

                EnemyEntryConfig entry = null;
                if (waveEntries != null)
                {
                    if (placement.entryIndex >= 0 && placement.entryIndex < waveEntries.Count)
                        entry = waveEntries[placement.entryIndex];
                    else if (spawnOrderedPlacements[i].sourceIndex < waveEntries.Count)
                        entry = waveEntries[spawnOrderedPlacements[i].sourceIndex];
                }

                if (entry != null && entry.Data == placement.data)
                    enemy.Setup(entry);
                else
                    enemy.Setup(placement.data);

                enemies?.Add(enemy);
                setEnemyPosition?.Invoke(
                    enemy,
                    placement.position,
                    placement.useUIPosition,
                    placement.useWorldPosition
                );

                if (placement.gridColumn <= playerColumn)
                    setEnemyGridFromCurrentPosition?.Invoke(enemy);
                else
                    setEnemyGrid?.Invoke(enemy, placement.gridRow, placement.gridColumn);
            }

            return;
        }

        if (waveEnemyDatas == null)
            return;

        int spawnCount = waveEntries != null && waveEntries.Count > 0 ? waveEntries.Count : waveEnemyDatas.Count;
        for (int i = 0; i < spawnCount; i++)
        {
            EnemyEntryConfig entry = waveEntries != null && i < waveEntries.Count ? waveEntries[i] : null;
            EnemyData data = entry != null ? entry.Data : waveEnemyDatas[i];
            if (data == null)
                continue;

            Enemy enemy = UnityEngine.Object.Instantiate(enemyPrefab, root, false);
            registerEnemy?.Invoke(enemy);

            if (entry != null)
                enemy.Setup(entry);
            else
                enemy.Setup(data);

            enemies?.Add(enemy);
            setEnemyPosition?.Invoke(
                enemy,
                new Vector3((enemies.Count - 1) * enemySpacing, 0f, 0f),
                false,
                false
            );

            if ((isChestEnemy != null && isChestEnemy(data)) || (isChestLevel != null && isChestLevel()))
                setEnemyGrid?.Invoke(enemy, getChestSpawnRow(), getChestSpawnColumn());
            else
                setEnemyGrid?.Invoke(enemy, 1, Mathf.Max(playerColumn + 1, gridColumns - 2));

            snapEnemyToGridPosition?.Invoke(enemy);
        }
    }

    List<IndexedEnemySpawnPlacement> BuildTopToBottomSpawnOrder(List<EnemySpawnPlacement> placements)
    {
        List<IndexedEnemySpawnPlacement> orderedPlacements = new List<IndexedEnemySpawnPlacement>();
        for (int i = 0; i < placements.Count; i++)
        {
            orderedPlacements.Add(new IndexedEnemySpawnPlacement
            {
                placement = placements[i],
                sourceIndex = i
            });
        }

        orderedPlacements.Sort(CompareSpawnPlacementTopToBottom);
        return orderedPlacements;
    }

    int CompareSpawnPlacementTopToBottom(IndexedEnemySpawnPlacement a, IndexedEnemySpawnPlacement b)
    {
        EnemySpawnPlacement placementA = a != null ? a.placement : null;
        EnemySpawnPlacement placementB = b != null ? b.placement : null;

        if (placementA == null && placementB == null)
            return 0;

        if (placementA == null)
            return 1;

        if (placementB == null)
            return -1;

        int yCompare = placementB.position.y.CompareTo(placementA.position.y);
        if (yCompare != 0)
            return yCompare;

        int rowCompare = placementA.gridRow.CompareTo(placementB.gridRow);
        if (rowCompare != 0)
            return rowCompare;

        int columnCompare = placementA.gridColumn.CompareTo(placementB.gridColumn);
        if (columnCompare != 0)
            return columnCompare;

        return a.sourceIndex.CompareTo(b.sourceIndex);
    }

    class IndexedEnemySpawnPlacement
    {
        public EnemySpawnPlacement placement;
        public int sourceIndex;
    }

    public Enemy AddEnemy(EnemyData data)
    {
        Enemy enemyPrefab = getEnemyPrefab?.Invoke();
        if (data == null || enemyPrefab == null)
            return null;

        if (!TryGetEmptySpawnCell(data, out int row, out int column))
            return null;

        Transform root = getCombatSpaceRoot?.Invoke()
            ?? getEnemyRoot?.Invoke()
            ?? getFallbackRoot?.Invoke();

        Enemy enemy = UnityEngine.Object.Instantiate(enemyPrefab, root, false);
        registerEnemy?.Invoke(enemy);
        enemy.Setup(data);
        getEnemies?.Invoke()?.Add(enemy);

        setEnemyGrid?.Invoke(enemy, row, column);
        snapEnemyToGridPosition?.Invoke(enemy);

        return enemy;
    }

    bool TryGetEmptySpawnCell(EnemyData data, out int row, out int column)
    {
        row = 0;
        column = 0;

        int rows = Mathf.Max(1, getGridRows());
        int columns = Mathf.Max(2, getGridColumns());
        int playerColumn = getPlayerColumn();
        int minEnemyColumn = Mathf.Clamp(playerColumn + 1, 1, columns - 1);
        int startColumn = data.type == EnemyType.Range
            ? Mathf.Clamp(getRangeSpawnColumn(), minEnemyColumn, columns - 1)
            : Mathf.Clamp(getMeleeSpawnColumn(), minEnemyColumn, columns - 1);

        List<int> preferredRows = GetPreferredSpawnRows(data, rows);

        for (int currentColumn = startColumn; currentColumn >= minEnemyColumn; currentColumn--)
        {
            for (int i = 0; i < preferredRows.Count; i++)
            {
                int currentRow = preferredRows[i];
                if (IsGridCellOccupied(currentRow, currentColumn))
                    continue;

                row = currentRow;
                column = currentColumn;
                return true;
            }
        }

        return false;
    }

    List<int> GetPreferredSpawnRows(EnemyData data, int rows)
    {
        List<int> preferredRows = new();
        int middleRow = rows / 2;

        if (data != null && data.enemyLevel == EnemyLevel.Boss)
        {
            preferredRows.Add(middleRow);
            return preferredRows;
        }

        for (int i = 0; i < rows; i++)
        {
            preferredRows.Add(i);
        }

        return preferredRows;
    }

    bool IsGridCellOccupied(int row, int column)
    {
        cleanupEnemies?.Invoke();
        List<Enemy> enemies = getEnemies?.Invoke();
        if (enemies == null)
            return false;

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy == null || !enemy.IsAlive())
                continue;

            if (enemy.gridRow == row && enemy.gridColumn == column)
                return true;
        }

        return false;
    }
}
