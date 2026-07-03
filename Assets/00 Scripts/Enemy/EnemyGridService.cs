using System;
using UnityEngine;

public class EnemyGridService
{
    readonly Func<int> getGridRows;
    readonly Func<int> getGridColumns;
    readonly Func<int> getPlayerColumn;
    readonly Func<float> getEnemySpacing;
    readonly Func<EnemySpawnArea> getSpawnArea;
    readonly Func<EnemyLevelPositionGenerator> getSpawnPositionGenerator;
    readonly Func<Transform> getCombatSpaceRoot;
    readonly Func<Transform> getEnemyRoot;
    readonly Func<Transform> getFallbackRoot;
    readonly Func<PlayerController> getPlayer;
    readonly Func<bool> getSnapSpawnedEnemiesToGrid;

    public EnemyGridService(
        Func<int> getGridRows,
        Func<int> getGridColumns,
        Func<int> getPlayerColumn,
        Func<float> getEnemySpacing,
        Func<EnemySpawnArea> getSpawnArea,
        Func<EnemyLevelPositionGenerator> getSpawnPositionGenerator,
        Func<Transform> getCombatSpaceRoot,
        Func<Transform> getEnemyRoot,
        Func<Transform> getFallbackRoot,
        Func<PlayerController> getPlayer,
        Func<bool> getSnapSpawnedEnemiesToGrid)
    {
        this.getGridRows = getGridRows;
        this.getGridColumns = getGridColumns;
        this.getPlayerColumn = getPlayerColumn;
        this.getEnemySpacing = getEnemySpacing;
        this.getSpawnArea = getSpawnArea;
        this.getSpawnPositionGenerator = getSpawnPositionGenerator;
        this.getCombatSpaceRoot = getCombatSpaceRoot;
        this.getEnemyRoot = getEnemyRoot;
        this.getFallbackRoot = getFallbackRoot;
        this.getPlayer = getPlayer;
        this.getSnapSpawnedEnemiesToGrid = getSnapSpawnedEnemiesToGrid;
    }

    public void SetEnemyGrid(Enemy enemy, int row, int column)
    {
        if (enemy == null)
            return;

        int playerColumn = getPlayerColumn();
        int gridColumns = getGridColumns();
        int gridRows = getGridRows();

        if (column <= playerColumn)
        {
            column = enemy.type == EnemyType.Range
                ? gridColumns - 1
                : gridColumns - 2;
        }

        enemy.gridRow = Mathf.Clamp(row, 0, Mathf.Max(0, gridRows - 1));
        enemy.gridColumn = Mathf.Clamp(column, playerColumn + 1, Mathf.Max(playerColumn + 1, gridColumns - 1));
    }

    public void SetEnemyGridFromCurrentPosition(Enemy enemy)
    {
        if (enemy == null)
            return;

        RectTransform rectTransform = enemy.transform as RectTransform;
        if (rectTransform == null)
        {
            SetEnemyGrid(enemy, 1, enemy.type == EnemyType.Range ? getGridColumns() - 1 : getGridColumns() - 2);
            return;
        }

        int playerColumn = getPlayerColumn();
        int closestRow = 0;
        int closestColumn = playerColumn + 1;
        float bestDistance = float.PositiveInfinity;

        for (int row = 0; row < Mathf.Max(1, getGridRows()); row++)
        {
            for (int column = playerColumn + 1; column < Mathf.Max(2, getGridColumns()); column++)
            {
                Vector3 gridPosition = GetGridLocalPosition(enemy, row, column);
                float distance = Vector2.SqrMagnitude(
                    new Vector2(rectTransform.localPosition.x, rectTransform.localPosition.y) - new Vector2(gridPosition.x, gridPosition.y)
                );

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    closestRow = row;
                    closestColumn = column;
                }
            }
        }

        SetEnemyGrid(enemy, closestRow, closestColumn);
    }

    public void SnapEnemyToGridPosition(Enemy enemy)
    {
        if (!getSnapSpawnedEnemiesToGrid() || enemy == null)
            return;

        RectTransform rectTransform = enemy.transform as RectTransform;
        if (rectTransform == null)
            return;

        Vector3 gridPosition = GetGridLocalPosition(enemy, enemy.gridRow, enemy.gridColumn);
        rectTransform.localPosition = new Vector3(gridPosition.x, gridPosition.y, rectTransform.localPosition.z);
    }

    public void SnapPlayerToConfiguredGrid()
    {
        PlayerController player = getPlayer?.Invoke();
        EnemyLevelPositionGenerator spawnPositionGenerator = getSpawnPositionGenerator?.Invoke();
        if (player == null || spawnPositionGenerator == null)
            return;

        int row = spawnPositionGenerator.GetPlayerSpawnRow();
        int column = spawnPositionGenerator.GetPlayerSpawnColumn();
        Vector3 position = GetGridLocalPosition(row, column);

        RectTransform playerRect = player.GetComponent<RectTransform>();
        if (playerRect != null)
        {
            Transform targetParent = GetDefaultParent();
            if (targetParent != null && playerRect.parent != targetParent)
                playerRect.SetParent(targetParent, false);

            playerRect.localPosition = position;
            return;
        }

        player.transform.localPosition = position;
    }

    public Vector3 GetGridLocalPosition(int row, int column)
    {
        return GetGridLocalPosition(null, row, column);
    }

    public Vector3 GetGridLocalPosition(Enemy enemy, int row, int column)
    {
        EnemySpawnArea spawnArea = getSpawnArea?.Invoke();
        if (spawnArea != null && spawnArea.HasValidArea)
            return SpawnAreaPointToEnemyParentLocal(
                GetGridAreaLocalPosition(row, column),
                GetEnemyGridParent(enemy)
            );

        float enemySpacing = getEnemySpacing();
        return new Vector3(column * enemySpacing, row * -enemySpacing, 0f);
    }

    public Vector3 GetGridWorldPosition(int row, int column)
    {
        EnemySpawnArea spawnArea = getSpawnArea?.Invoke();
        Vector3 areaPoint = GetGridAreaLocalPosition(row, column);
        return spawnArea.spawnSpace == EnemySpawnSpace.World ? areaPoint : spawnArea.uiArea.TransformPoint(areaPoint);
    }

    Vector3 GetGridAreaLocalPosition(int row, int column)
    {
        EnemySpawnArea spawnArea = getSpawnArea?.Invoke();
        int rows = Mathf.Max(1, getGridRows());
        int columns = Mathf.Max(2, getGridColumns());

        float x01 = columns <= 1 ? 0.5f : (float)Mathf.Clamp(column, 0, columns - 1) / (columns - 1);
        float y01 = rows <= 1 ? 0.5f : 1f - ((float)Mathf.Clamp(row, 0, rows - 1) / (rows - 1));
        return spawnArea.GetPoint(x01, y01);
    }

    Transform GetEnemyGridParent(Enemy enemy)
    {
        if (enemy != null && enemy.transform != null)
            return enemy.transform.parent;

        return GetDefaultParent();
    }

    Transform GetDefaultParent()
    {
        return getCombatSpaceRoot?.Invoke()
            ?? getEnemyRoot?.Invoke()
            ?? getFallbackRoot?.Invoke();
    }

    public Vector3 SpawnAreaPointToEnemyParentLocal(Vector3 spawnAreaLocalPosition, Transform targetParent = null)
    {
        EnemySpawnArea spawnArea = getSpawnArea?.Invoke();
        if (targetParent == null)
            targetParent = GetDefaultParent();

        if (spawnArea == null || targetParent == null)
            return spawnAreaLocalPosition;

        if (spawnArea.spawnSpace == EnemySpawnSpace.UI)
        {
            if (spawnArea.uiArea == null || spawnArea.uiArea == targetParent)
                return spawnAreaLocalPosition;

            Vector3 uiWorldPoint = spawnArea.uiArea.TransformPoint(spawnAreaLocalPosition);
            return targetParent.InverseTransformPoint(uiWorldPoint);
        }

        Vector3 worldPoint = spawnAreaLocalPosition;
        return targetParent.InverseTransformPoint(worldPoint);
    }
}