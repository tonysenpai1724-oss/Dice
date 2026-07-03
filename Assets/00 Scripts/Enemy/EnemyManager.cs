using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EnemyManager : Singleton<EnemyManager>
{
    EnemyWinCoordinator winCoordinator;
    EnemyProjectileAttackPresenter projectileAttackPresenter;
    EnemyTurnService turnService;
    EnemyChestService chestService;
    EnemyAttackRoutineService attackRoutineService;
    EnemyMeleeTurnService meleeTurnService;
    EnemyGridService gridService;
    EnemyWaveSpawner waveSpawner;
    EnemyQueryService queryService;

    public EffectManager effectManager;

    [Header("Refs")]
    public Enemy enemyPrefab;
    public Transform enemyRoot;
    public PlayerController player;
    public EnemyLevelPositionGenerator spawnPositionGenerator;
    public EnemySpawnArea spawnArea;
    public Transform combatSpaceRoot;
    public RectTransform attackPoint;

    [Header("Layout")]
    public float enemySpacing = 120f;
    public float meleeMoveDistance = 80f;
    public float enemyMoveDuration = 0.2f;

    [Header("Combat")]
    public int meleeStepPerTurn = 30;
    public float enemyActionDelay = 0.15f;

    [Header("Grid Combat")]
    public int gridRows = 3;
    public int gridColumns = 6;
    public int playerColumn = 0;
    public int meleeAttackColumn = 1;
    public int meleeSpawnColumn = 4;
    public int rangeSpawnColumn = 5;
    public float gridMoveStagger = 0.08f;
    public bool snapSpawnedEnemiesToGrid = true;

    public List<Enemy> enemies = new();
    public int activeProjectiles = 0;

    Level currentLevel;
    int currentWaveIndex;
    int chestTurnsRemaining;

    [Header("Projectile")]
    public RectTransform projectilePrefab;
    public Transform projectileRoot;
    public float projectileSpeed = 2400f;
    public Vector3 projectileOffset = new Vector3(0, 4f, 0);
    public float projectileRotationOffset;

    public override void Awake()
    {
        base.Awake();

        effectManager = GetComponent<EffectManager>();
        if (effectManager == null)
            effectManager = gameObject.AddComponent<EffectManager>();

        gridService = new EnemyGridService(
            () => gridRows,
            () => gridColumns,
            () => playerColumn,
            () => enemySpacing,
            () => spawnArea,
            () => spawnPositionGenerator,
            () => combatSpaceRoot,
            () => enemyRoot,
            () => transform,
            () => player,
            () => snapSpawnedEnemiesToGrid);

        queryService = new EnemyQueryService(
            () => enemies,
            CleanupEnemies,
            GetCombatSpaceX,
            () => player);

        chestService = new EnemyChestService(
            () => currentLevel,
            () => gridRows,
            () => gridColumns,
            () => playerColumn,
            () => chestTurnsRemaining,
            value => chestTurnsRemaining = value,
            GetAliveChestEnemy,
            SetEnemyGrid,
            SnapEnemyToGridPosition,
            () => GameplayManager.Instance?.EndGame(false));

        attackRoutineService = new EnemyAttackRoutineService(() => player);

        meleeTurnService = new EnemyMeleeTurnService(
            () => GetAliveEnemiesOfType(EnemyType.Melee),
            CanMeleeAttack,
            (enemy, row, column) => GetGridLocalPosition(enemy, row, column),
            () => meleeAttackColumn,
            () => gridMoveStagger,
            () => enemyMoveDuration);

        projectileAttackPresenter = new EnemyProjectileAttackPresenter(
            this,
            () => player,
            () => projectilePrefab,
            () => projectileRoot,
            () => projectileSpeed,
            () => projectileOffset,
            () => projectileRotationOffset,
            () => activeProjectiles++,
            () => activeProjectiles--);

        waveSpawner = new EnemyWaveSpawner(
            () => currentLevel,
            () => currentWaveIndex,
            () => enemyPrefab,
            () => combatSpaceRoot,
            () => enemyRoot,
            () => transform,
            () => spawnPositionGenerator,
            () => playerColumn,
            () => gridRows,
            () => gridColumns,
            () => meleeSpawnColumn,
            () => rangeSpawnColumn,
            () => enemySpacing,
            () => IsChestLevel(currentLevel),
            IsChestEnemy,
            GetChestSpawnRow,
            GetChestSpawnColumn,
            () => enemies,
            ClearEnemies,
            SnapPlayerToConfiguredGrid,
            RegisterEnemy,
            (enemy, position, useUIPosition, useWorldPosition) => SetEnemyPosition(enemy, position, useUIPosition, useWorldPosition),
            SetEnemyGridFromCurrentPosition,
            SetEnemyGrid,
            SnapEnemyToGridPosition,
            CleanupEnemies);

        turnService = new EnemyTurnService(
            CleanupEnemies,
            ApplyPoisonTicks,
            CheckWinGame,
            HasAliveEnemies,
            TryHandleChestTurn,
            () => effectManager?.GetEffect<EnemyTurnSkipEffect>(),
            MoveMeleeEnemiesOneGridStep,
            () => GetEnemyTurnAttackers(null),
            CanMeleeAttack,
            AttackPlayerRoutine,
            enemyActionDelay);

        winCoordinator = new EnemyWinCoordinator(
            this,
            CleanupEnemies,
            () => enemies.Count != 0,
            TryAdvanceToNextWave,
            () => GameplayManager.Instance?.EndGame(true));
    }

    public void StartLevel(Level level)
    {
        currentLevel = level;
        currentWaveIndex = 0;
        chestTurnsRemaining = IsChestLevel(level) ? 5 : 0;
        SpawnCurrentWave(true);
    }

    public void SpawnEnemies(Level level)
    {
        StartLevel(level);
    }

    public bool HasMoreWaves()
    {
        return currentLevel != null && currentWaveIndex + 1 < currentLevel.WaveCount;
    }

    public int GetCurrentWaveIndex()
    {
        return currentWaveIndex;
    }

    public void SpawnCurrentWave(bool clearExisting)
    {
        waveSpawner?.SpawnCurrentWave(clearExisting);
    }

    bool TryAdvanceToNextWave()
    {
        if (!HasMoreWaves())
            return false;

        currentWaveIndex++;
        SpawnCurrentWave(true);
        return HasAliveEnemies();
    }

    public Enemy AddEnemy(EnemyData data)
    {
        return waveSpawner != null ? waveSpawner.AddEnemy(data) : null;
    }

    public float GetCombatSpaceX(Transform target)
    {
        if (target == null)
            return 0f;

        if (target is RectTransform rectTransform)
            return rectTransform.localPosition.x;

        return target.localPosition.x;
    }

    public Enemy GetNearestAliveEnemy()
    {
        return queryService != null ? queryService.GetNearestAliveEnemy() : null;
    }

    public Enemy GetRightmostAliveEnemy()
    {
        return queryService != null ? queryService.GetRightmostAliveEnemy() : null;
    }

    public void PlayerAttack(DiceData diceData)
    {
        if (diceData == null)
            return;

        int finalDamage = CombatSystem.CalculateFinalPlayerAttackDamage(player, diceData.damage);
        PlayerAttack(finalDamage);
    }

    public void PlayerAttack(int damage)
    {
        Enemy target = GetNearestAliveEnemy();
        if (target == null)
        {
            CheckWinGame();
            return;
        }

        projectileAttackPresenter?.PlayPlayerAttack(target, damage);
        CheckWinGame();
    }

    public IEnumerator EnemyTurn()
    {
        if (turnService != null)
            yield return turnService.ExecuteTurn();
    }

    public void RemoveEnemy(Enemy enemy)
    {
        if (enemy == null)
            return;

        enemy.DeathCompleted -= RemoveEnemy;
        enemies.Remove(enemy);
        Destroy(enemy.gameObject);
        CheckWinGame();
    }

    void RegisterEnemy(Enemy enemy)
    {
        if (enemy == null)
            return;

        enemy.DeathCompleted -= RemoveEnemy;
        enemy.DeathCompleted += RemoveEnemy;
    }

    public void CheckWinGame()
    {
        winCoordinator?.CheckWinGame();
    }

    void RequestDeferredWinCheck()
    {
        winCoordinator?.RequestDeferredWinCheck();
    }

    bool IsChestLevel(Level level)
    {
        return chestService != null && chestService.IsChestLevel(level);
    }

    bool IsChestEnemy(EnemyData data)
    {
        return chestService != null && chestService.IsChestEnemy(data);
    }

    int GetChestSpawnRow()
    {
        return chestService != null ? chestService.GetChestSpawnRow() : 0;
    }

    int GetChestSpawnColumn()
    {
        return chestService != null ? chestService.GetChestSpawnColumn() : playerColumn + 1;
    }

    Enemy GetAliveChestEnemy()
    {
        CleanupEnemies();

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy != null && enemy.IsAlive() && enemy.type == EnemyType.Chest)
                return enemy;
        }

        return null;
    }

    bool TryHandleChestTurn()
    {
        return chestService != null && chestService.TryHandleChestTurn();
    }

    public bool HasAliveEnemies()
    {
        return queryService != null && queryService.HasAliveEnemies();
    }

    IEnumerator AttackPlayerRoutine(Enemy enemy)
    {
        if (attackRoutineService != null)
            yield return attackRoutineService.AttackPlayerRoutine(enemy);
    }

    public void SkipNextEnemyTurns(int amount = 1)
    {
        if (amount <= 0)
            return;

        EnemyTurnSkipEffect turnSkipEffect = effectManager?.AddEffect<EnemyTurnSkipEffect>();
        if (turnSkipEffect != null)
            turnSkipEffect.AddTurns(amount);
    }

    public void ReduceNextPlayerDamage(int amount)
    {
        if (amount <= 0 || player == null)
            return;

        DamageReductionEffect damageReductionEffect = player.effectManager?.AddEffect<DamageReductionEffect>();
        if (damageReductionEffect != null)
            damageReductionEffect.AddReduction(amount);
    }

    public void DamageAllEnemies(int amount)
    {
        if (amount <= 0)
            return;

        DamageAllEnemiesEffect damageAllEnemiesEffect = effectManager?.AddEffect<DamageAllEnemiesEffect>();
        if (damageAllEnemiesEffect != null)
            damageAllEnemiesEffect.Apply(amount);
    }

    public void ApplyPoison(Enemy target, int turns, int damagePerTurn)
    {
        if (target == null || !target.gameObject.activeInHierarchy || turns <= 0 || damagePerTurn <= 0)
            return;

        PoisonEffect poisonEffect = target.effectManager?.AddEffect<PoisonEffect>();
        if (poisonEffect != null)
            poisonEffect.Apply(turns, damagePerTurn);
    }

    void ApplyPoisonTicks()
    {
        CleanupEnemies();

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy != null && enemy.IsAlive())
                enemy.BeginTurn();
        }
    }

    Enemy GetFrontEnemy()
    {
        return queryService != null ? queryService.GetFrontEnemy() : null;
    }

    Enemy GetFrontEnemyOfType(EnemyType type)
    {
        return queryService != null ? queryService.GetFrontEnemyOfType(type) : null;
    }

    List<Enemy> GetEnemyTurnAttackers(Enemy frontEnemy)
    {
        List<Enemy> attackers = new();

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy == null || !enemy.IsAlive())
                continue;

            if (enemy.type == EnemyType.Range)
                AddEnemyTurnAttacker(attackers, enemy);
        }

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy == null || !enemy.IsAlive())
                continue;

            if (enemy.type == EnemyType.Melee && CanMeleeAttack(enemy))
                AddEnemyTurnAttacker(attackers, enemy);
        }

        return attackers;
    }

    void AddEnemyTurnAttacker(List<Enemy> attackers, Enemy enemy)
    {
        if (enemy == null || !enemy.IsAlive() || attackers.Contains(enemy))
            return;

        attackers.Add(enemy);
    }

    bool CanMeleeAttack(Enemy enemy)
    {
        if (enemy == null || !enemy.IsAlive() || enemy.type != EnemyType.Melee)
            return false;

        if (!enemy.CanAttack() || enemy.gridColumn > meleeAttackColumn)
            return false;

        if (enemy.transform is not RectTransform rectTransform)
            return true;

        float attackX = GetGridLocalPosition(enemy, enemy.gridRow, meleeAttackColumn).x;
        return Mathf.Abs(rectTransform.localPosition.x - attackX) <= 1f;
    }

    IEnumerator MoveMeleeEnemiesOneGridStep()
    {
        if (meleeTurnService != null)
            yield return meleeTurnService.MoveMeleeEnemiesOneGridStep();
    }

    void SetEnemyGrid(Enemy enemy, int row, int column)
    {
        gridService?.SetEnemyGrid(enemy, row, column);
    }

    void SetEnemyGridFromCurrentPosition(Enemy enemy)
    {
        gridService?.SetEnemyGridFromCurrentPosition(enemy);
    }

    void SnapEnemyToGridPosition(Enemy enemy)
    {
        gridService?.SnapEnemyToGridPosition(enemy);
    }

    void SnapPlayerToConfiguredGrid()
    {
        gridService?.SnapPlayerToConfiguredGrid();
    }

    Vector3 GetGridLocalPosition(int row, int column)
    {
        return gridService != null ? gridService.GetGridLocalPosition(row, column) : Vector3.zero;
    }

    Vector3 GetGridLocalPosition(Enemy enemy, int row, int column)
    {
        return gridService != null ? gridService.GetGridLocalPosition(enemy, row, column) : Vector3.zero;
    }

    Vector3 SpawnAreaPointToEnemyParentLocal(Vector3 spawnAreaLocalPosition, Transform targetParent = null)
    {
        return gridService != null
            ? gridService.SpawnAreaPointToEnemyParentLocal(spawnAreaLocalPosition, targetParent)
            : spawnAreaLocalPosition;
    }

    List<Enemy> GetAliveEnemiesOfType(EnemyType type)
    {
        List<Enemy> row = new();

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy == null || !enemy.IsAlive())
                continue;

            if (enemy.type != type)
                continue;

            row.Add(enemy);
        }

        row.Sort((a, b) => GetCombatSpaceX(a.transform).CompareTo(GetCombatSpaceX(b.transform)));
        return row;
    }

    void SetEnemyPosition(Enemy enemy, Vector3 position, bool useUIPosition, bool useWorldPosition = false)
    {
        RectTransform rectTransform = enemy.transform as RectTransform;
        if (useWorldPosition)
        {
            Transform parent = enemyRoot != null ? enemyRoot : transform;
            enemy.transform.SetParent(parent, true);
            enemy.transform.position = position;
            return;
        }

        if (useUIPosition && rectTransform != null)
        {
            rectTransform.SetParent(
                combatSpaceRoot != null ? combatSpaceRoot : (enemyRoot != null ? enemyRoot : transform),
                false);

            Vector3 localPosition = SpawnAreaPointToEnemyParentLocal(position, rectTransform.parent);
            rectTransform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
            return;
        }

        if (rectTransform != null)
        {
            rectTransform.SetParent(
                combatSpaceRoot != null ? combatSpaceRoot : (enemyRoot != null ? enemyRoot : transform),
                false);
            rectTransform.localPosition = position;
            return;
        }

        enemy.transform.SetParent(
            combatSpaceRoot != null ? combatSpaceRoot : (enemyRoot != null ? enemyRoot : transform),
            false);
        enemy.transform.localPosition = position;
    }

    public void ClearEnemies()
    {
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = enemies[i];
            if (enemy == null)
                continue;

            enemy.DeathCompleted -= RemoveEnemy;
            enemy.transform.DOKill();

            if (enemy.skeletonGraphic != null)
                enemy.skeletonGraphic.DOKill();

            Destroy(enemy.gameObject);
        }

        enemies.Clear();
        activeProjectiles = 0;
    }

    void CleanupEnemies()
    {
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            if (enemies[i] == null)
                enemies.RemoveAt(i);
        }
    }

    void OnDrawGizmosSelected()
    {
        DrawCombatGridGizmos();
    }

    void DrawCombatGridGizmos()
    {
        if (spawnArea == null || !spawnArea.HasValidArea)
            return;

        int rows = Mathf.Max(1, gridRows);
        int columns = Mathf.Max(2, gridColumns);
        Gizmos.color = new Color(0f, 1f, 1f, 0.65f);

        for (int row = 0; row < rows; row++)
        {
            Vector3 start = GetGridWorldPosition(row, 0);
            Vector3 end = GetGridWorldPosition(row, columns - 1);
            Gizmos.DrawLine(start, end);
        }

        for (int column = 0; column < columns; column++)
        {
            Vector3 start = GetGridWorldPosition(0, column);
            Vector3 end = GetGridWorldPosition(rows - 1, column);
            Gizmos.DrawLine(start, end);
        }

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                Vector3 point = GetGridWorldPosition(row, column);
                Gizmos.color = column == playerColumn
                    ? Color.green
                    : column == meleeAttackColumn
                        ? Color.yellow
                        : Color.cyan;
                Gizmos.DrawSphere(point, 6f);

#if UNITY_EDITOR
                Handles.Label(point + Vector3.up * 10f, $"R{row} C{column}");
#endif
            }
        }
    }

    Vector3 GetGridWorldPosition(int row, int column)
    {
        return gridService != null ? gridService.GetGridWorldPosition(row, column) : Vector3.zero;
    }
}
