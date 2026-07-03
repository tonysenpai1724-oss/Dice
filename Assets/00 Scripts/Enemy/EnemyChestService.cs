using System;

public class EnemyChestService
{
    readonly Func<Level> getCurrentLevel;
    readonly Func<int> getGridRows;
    readonly Func<int> getGridColumns;
    readonly Func<int> getPlayerColumn;
    readonly Func<int> getChestTurnsRemaining;
    readonly Action<int> setChestTurnsRemaining;
    readonly Func<Enemy> getAliveChestEnemy;
    readonly Action<Enemy, int, int> setEnemyGrid;
    readonly Action<Enemy> snapEnemyToGridPosition;
    readonly Action endGameAsLose;

    public EnemyChestService(
        Func<Level> getCurrentLevel,
        Func<int> getGridRows,
        Func<int> getGridColumns,
        Func<int> getPlayerColumn,
        Func<int> getChestTurnsRemaining,
        Action<int> setChestTurnsRemaining,
        Func<Enemy> getAliveChestEnemy,
        Action<Enemy, int, int> setEnemyGrid,
        Action<Enemy> snapEnemyToGridPosition,
        Action endGameAsLose)
    {
        this.getCurrentLevel = getCurrentLevel;
        this.getGridRows = getGridRows;
        this.getGridColumns = getGridColumns;
        this.getPlayerColumn = getPlayerColumn;
        this.getChestTurnsRemaining = getChestTurnsRemaining;
        this.setChestTurnsRemaining = setChestTurnsRemaining;
        this.getAliveChestEnemy = getAliveChestEnemy;
        this.setEnemyGrid = setEnemyGrid;
        this.snapEnemyToGridPosition = snapEnemyToGridPosition;
        this.endGameAsLose = endGameAsLose;
    }

    public bool IsChestLevel(Level level)
    {
        return level != null && level.leveltype == LevelType.Chest;
    }

    public bool IsChestEnemy(EnemyData data)
    {
        return data != null && data.type == EnemyType.Chest;
    }

    public int GetChestSpawnRow()
    {
        return UnityEngine.Mathf.Clamp(1, 0, UnityEngine.Mathf.Max(0, getGridRows() - 1));
    }

    public int GetChestSpawnColumn()
    {
        int playerColumn = getPlayerColumn();
        return UnityEngine.Mathf.Clamp(2, playerColumn + 1, UnityEngine.Mathf.Max(playerColumn + 1, getGridColumns() - 1));
    }

    public bool TryHandleChestTurn()
    {
        if (!IsChestLevel(getCurrentLevel?.Invoke()))
            return false;

        Enemy chestEnemy = getAliveChestEnemy?.Invoke();
        if (chestEnemy == null)
            return false;

        int turnsRemaining = UnityEngine.Mathf.Max(0, getChestTurnsRemaining() - 1);
        setChestTurnsRemaining?.Invoke(turnsRemaining);

        int playerColumn = getPlayerColumn();
        int gridColumns = getGridColumns();
        int nextColumn = UnityEngine.Mathf.Min(UnityEngine.Mathf.Max(playerColumn + 1, gridColumns - 1), chestEnemy.gridColumn + 1);
        setEnemyGrid?.Invoke(chestEnemy, chestEnemy.gridRow, nextColumn);
        snapEnemyToGridPosition?.Invoke(chestEnemy);

        if (turnsRemaining <= 0)
        {
            endGameAsLose?.Invoke();
            return true;
        }

        return true;
    }
}