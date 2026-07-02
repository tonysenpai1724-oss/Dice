using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EnemyManager : Singleton<EnemyManager>
{
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
        if (clearExisting)
            ClearEnemies();

        if (currentLevel == null || enemyPrefab == null)
            return;

        SnapPlayerToConfiguredGrid();


        List<EnemyData> waveEnemyDatas = currentLevel.GetWaveEnemyDatas(currentWaveIndex);
        List<EnemySpawnPlacement> wavePlacements = currentLevel.GetWaveSpawnPlacements(currentWaveIndex);

        Transform root =
            combatSpaceRoot != null
                ? combatSpaceRoot
                : (enemyRoot != null ? enemyRoot : transform);
        List<EnemySpawnPlacement> placements =
            wavePlacements != null &&
            wavePlacements.Count > 0
                ? wavePlacements
                : spawnPositionGenerator != null
                    ? spawnPositionGenerator.BuildPlacements(waveEnemyDatas)
                    : null;

        if (placements != null && placements.Count > 0)
        {
            for (int i = 0; i < placements.Count; i++)
            {
                EnemySpawnPlacement placement = placements[i];
                if (placement == null || placement.data == null)
                    continue;

                Enemy enemy = Instantiate(enemyPrefab, root, false);
                RegisterEnemy(enemy);
                enemy.Setup(placement.data);
                enemies.Add(enemy);
                SetEnemyPosition(
                    enemy,
                    placement.position,
                    placement.useUIPosition,
                    placement.useWorldPosition
                );

                if (placement.gridColumn <= playerColumn)
                    SetEnemyGridFromCurrentPosition(enemy);
                else
                    SetEnemyGrid(enemy, placement.gridRow, placement.gridColumn);
            }

            return;
        }

        if (waveEnemyDatas == null)
            return;

        for (int i = 0; i < waveEnemyDatas.Count; i++)
        {
            EnemyData data = waveEnemyDatas[i];
            if (data == null)
                continue;

            Enemy enemy = Instantiate(enemyPrefab, root, false);
            RegisterEnemy(enemy);
            enemy.Setup(data);
            enemies.Add(enemy);
            SetEnemyPosition(
                enemy,
                new Vector3(
                    (enemies.Count - 1) * enemySpacing,
                    0f,
                    0f
                ),
                false
            );
            if (IsChestEnemy(data) || IsChestLevel(currentLevel))
                SetEnemyGrid(enemy, GetChestSpawnRow(), GetChestSpawnColumn());
            else
                SetEnemyGrid(enemy, 1, Mathf.Max(meleeAttackColumn + 1, gridColumns - 2));
            SnapEnemyToGridPosition(enemy);
        }
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
        if (data == null || enemyPrefab == null)
            return null;

        if (!TryGetEmptySpawnCell(data, out int row, out int column))
            return null;

        Transform root =
            combatSpaceRoot != null
                ? combatSpaceRoot
                : (enemyRoot != null ? enemyRoot : transform);

        Enemy enemy = Instantiate(enemyPrefab, root, false);
        RegisterEnemy(enemy);
        enemy.Setup(data);
        enemies.Add(enemy);

        SetEnemyGrid(enemy, row, column);
        SnapEnemyToGridPosition(enemy);

        return enemy;
    }

    bool TryGetEmptySpawnCell(EnemyData data, out int row, out int column)
    {
        row = 0;
        column = 0;

        int rows = Mathf.Max(1, gridRows);
        int columns = Mathf.Max(2, gridColumns);
        int minEnemyColumn = Mathf.Clamp(playerColumn + 1, 1, columns - 1);
        int startColumn = data.type == EnemyType.Range
            ? Mathf.Clamp(rangeSpawnColumn, minEnemyColumn, columns - 1)
            : Mathf.Clamp(meleeSpawnColumn, minEnemyColumn, columns - 1);

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
        CleanupEnemies();

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

    void SpawnProjectile(Enemy target, int damage)
    {
        if (projectilePrefab == null || target == null)
        {
            activeProjectiles--;
            return;
        }

        RectTransform projectile =
            Instantiate(projectilePrefab, projectileRoot);

        RectTransform playerRect =
            player.GetComponent<RectTransform>();

        RectTransform targetRect =
            target.GetComponent<RectTransform>();

        // Spawn tại vị trí player
        projectile.position = playerRect.position + projectileOffset;

        // Tính hướng bay
        Vector3 targetPos = targetRect.position;
        targetPos.y += projectileRotationOffset; // Điều chỉnh độ cao nếu cần
        Vector3 dir = targetPos - projectile.position;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // Nếu sprite gốc hướng lên
        projectile.rotation = Quaternion.Euler(0, 0, angle - 90f);

        projectile.DOMove(
            targetPos,
            Mathf.Max(1f, projectileSpeed)
        )
        .SetSpeedBased(true)
        .SetEase(Ease.Linear)
        .OnComplete(() =>
        {
            if (target != null)
                target.OnTakeDamage(damage);
            Destroy(projectile.gameObject);
            activeProjectiles--;
        });
    }
    IEnumerator SpawnProjectileDelayed(Enemy target, int damage)
    {
        yield return new WaitForSeconds(0.35f);

        SpawnProjectile(target, damage);
    }
    public void ClearEnemies()
    {
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            if (enemies[i] != null)
                Destroy(enemies[i].gameObject);
        }

        enemies.Clear();
    }

    public float GetCombatSpaceX(Transform target)
    {
        if (target == null)
            return 0f;

        RectTransform rectTransform =
            target as RectTransform;

        if (rectTransform != null)
            return rectTransform.localPosition.x;

        return target.localPosition.x;
    }

    public Enemy GetNearestAliveEnemy()
    {
        CleanupEnemies();

        Enemy nearestEnemy = null;
        float bestX = float.PositiveInfinity;

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy == null || !enemy.IsAlive())
                continue;

            float enemyX = GetCombatSpaceX(enemy.transform);
            if (enemyX < bestX)
            {
                bestX = enemyX;
                nearestEnemy = enemy;
            }
        }

        return nearestEnemy;
    }

    public Enemy GetRightmostAliveEnemy()
    {
        CleanupEnemies();

        Enemy rightmostEnemy = null;
        float bestX = float.NegativeInfinity;

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy == null || !enemy.IsAlive())
                continue;

            float enemyX = GetCombatSpaceX(enemy.transform);
            if (enemyX > bestX)
            {
                bestX = enemyX;
                rightmostEnemy = enemy;
            }
        }

        return rightmostEnemy;
    }

    public void PlayerAttack(DiceData diceData)
    {
        if (diceData == null)
            return;

        int baseDamage = diceData.damage + (player != null ? player.RuntimeDamage : 0);
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

        activeProjectiles++;
        player.PlayAnimation(player.attackAnim, false);
        StartCoroutine(SpawnProjectileDelayed(target, damage));

        if (player.skeletonGraphic != null)
        {
            player.skeletonGraphic.AnimationState.AddAnimation(
                0,
                AnimationNameUtility.ResolveAnimationName(
                    player.skeletonGraphic.Skeleton?.Data?.Animations,
                    player.idleAnim
                ),
                true,
                0
            );
        }


        CheckWinGame();
    }

    public IEnumerator EnemyTurn()
    {
        CleanupEnemies();
        ApplyPoisonTicks();

        CheckWinGame();
        if (!HasAliveEnemies())
            yield break;

        if (TryHandleChestTurn())
            yield break;

        EnemyTurnSkipEffect turnSkipEffect = effectManager?.GetEffect<EnemyTurnSkipEffect>();
        if (turnSkipEffect != null && turnSkipEffect.ConsumeTurnSkip())
        {
            yield break;
        }

        yield return MoveMeleeEnemiesOneGridStep();

        List<Enemy> attackers = GetEnemyTurnAttackers(null);

        for (int i = 0; i < attackers.Count; i++)
        {
            Enemy attacker = attackers[i];
            if (attacker == null || !attacker.IsAlive())
                continue;

            if (attacker.type == EnemyType.Melee && !CanMeleeAttack(attacker))
                continue;

            yield return AttackPlayerRoutine(attacker);

            if (enemyActionDelay > 0f)
                yield return new WaitForSeconds(enemyActionDelay);
        }

    }

    public void RemoveEnemy(Enemy enemy)
    {
        if (enemy == null)
            return;

        enemy.DeathCompleted -= RemoveEnemy;
        enemies.Remove(enemy);
        Destroy(enemy.gameObject);
        // RebuildLayout();
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
        CleanupEnemies();

        if (enemies.Count != 0 || GameplayManager.Instance == null || GameplayManager.Instance.IsGameEnded)
            return;

        DiceQueueUI queueUI = DiceManager.Instance != null && DiceManager.Instance.diceQueueUI != null
            ? DiceManager.Instance.diceQueueUI
            : DiceQueueUI.Instance;
        if (queueUI != null && queueUI.IsBusy)
        {
            queueUI.RequestFastFlush();
            return;
        }

        DiceQueue queue = DiceManager.Instance != null ? DiceManager.Instance.diceQueue : null;
        if (queue != null && queue.IsBusy)
        {
            queue.RequestFastFlush();
            return;
        }

        if (TryAdvanceToNextWave())
            return;

        GameplayManager.Instance.EndGame(true);
    }

    bool IsChestLevel(Level level)
    {
        return level != null && level.leveltype == LevelType.Chest;
    }

    bool IsChestEnemy(EnemyData data)
    {
        return data != null && data.type == EnemyType.Chest;
    }

    int GetChestSpawnRow()
    {
        return Mathf.Clamp(1, 0, Mathf.Max(0, gridRows - 1));
    }

    int GetChestSpawnColumn()
    {
        return Mathf.Clamp(2, playerColumn + 1, Mathf.Max(playerColumn + 1, gridColumns - 1));
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
        if (!IsChestLevel(currentLevel))
            return false;

        Enemy chestEnemy = GetAliveChestEnemy();
        if (chestEnemy == null)
            return false;

        chestTurnsRemaining = Mathf.Max(0, chestTurnsRemaining - 1);

        int nextColumn = Mathf.Min(Mathf.Max(playerColumn + 1, gridColumns - 1), chestEnemy.gridColumn + 1);
        SetEnemyGrid(chestEnemy, chestEnemy.gridRow, nextColumn);
        SnapEnemyToGridPosition(chestEnemy);

        if (chestTurnsRemaining <= 0)
        {
            GameplayManager.Instance?.EndGame(false);
            return true;
        }

        return true;
    }

    public bool HasAliveEnemies()
    {
        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy != null && enemy.IsAlive())
                return true;
        }

        return false;
    }

    IEnumerator AttackPlayerRoutine(Enemy enemy)
    {
        if (player == null || enemy == null)
            yield break;

        bool attackCompleted = false;
        Spine.TrackEntry attackTrack = enemy.PlayAnimation(enemy.attackAnim, false);
        
        if (attackTrack == null)
            yield break;

        attackTrack.Complete += _ => attackCompleted = true;

        float duration = attackTrack.Animation.Duration;
        float halfTime = duration * 0.5f;
        float timer = 0f;

        while (enemy != null && enemy.IsAlive())
        {
            timer += Time.deltaTime;

            if (!attackCompleted && (attackTrack.TrackTime >= halfTime || timer >= halfTime))
            {
                attackCompleted = true;
                int finalDamage = CombatSystem.ApplyDefenseToPlayer(player, enemy.damage);
                player.OnTakeDamage(finalDamage);
            }

            if (attackTrack.IsComplete || timer >= duration)
                break;

            yield return null;
        }

        if (enemy != null && enemy.IsAlive() && enemy.skeletonGraphic != null)
        {
            enemy.skeletonGraphic.AnimationState.AddAnimation(
                0,
                AnimationNameUtility.ResolveAnimationName(
                    enemy.skeletonGraphic.Skeleton?.Data?.Animations,
                    enemy.idleAnim
                ),
                true,
                0
            );
        }
        // player.OnTakeDamage(enemy.damage);
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
        if (amount <= 0)
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

    public void ApplyPoison(
        Enemy target,
        int turns,
        int damagePerTurn
    )
    {
        if (target == null ||
            !target.gameObject.activeInHierarchy ||
            turns <= 0 ||
            damagePerTurn <= 0)
        {
            return;
        }

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
        CleanupEnemies();

        Enemy frontEnemy = null;
        float bestX = float.PositiveInfinity;
        float playerX =
            player != null
                ? GetCombatSpaceX(player.transform)
                : float.NegativeInfinity;

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy == null || !enemy.IsAlive())
                continue;

            float enemyX = GetCombatSpaceX(enemy.transform);
            if (enemyX < playerX)
                continue;

            if (enemyX < bestX)
            {
                bestX = enemyX;
                frontEnemy = enemy;
            }
        }

        if (frontEnemy != null)
            return frontEnemy;

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy != null && enemy.IsAlive())
                return enemy;
        }

        return null;
    }

    Enemy GetFrontEnemyOfType(EnemyType type)
    {
        CleanupEnemies();

        Enemy frontEnemy = null;
        float bestX = float.PositiveInfinity;
        float playerX =
            player != null
                ? GetCombatSpaceX(player.transform)
                : float.NegativeInfinity;

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy == null || !enemy.IsAlive())
                continue;

            if (enemy.type != type)
                continue;

            float enemyX = GetCombatSpaceX(enemy.transform);
            if (enemyX < playerX)
                continue;

            if (enemyX < bestX)
            {
                bestX = enemyX;
                frontEnemy = enemy;
            }
        }

        return frontEnemy;
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

        RectTransform rectTransform = enemy.transform as RectTransform;
        if (rectTransform == null)
            return true;

        float attackX = GetGridLocalPosition(enemy, enemy.gridRow, meleeAttackColumn).x;
        return Mathf.Abs(rectTransform.localPosition.x - attackX) <= 1f;
    }

    IEnumerator MoveMeleeEnemiesOneGridStep()
    {
        List<Enemy> meleeEnemies = GetAliveEnemiesOfType(EnemyType.Melee);
        if (meleeEnemies.Count == 0)
            yield break;

        meleeEnemies.Sort((a, b) => a.gridColumn.CompareTo(b.gridColumn));

        Sequence sequence = DOTween.Sequence();
        int moveIndex = 0;
        HashSet<string> occupiedCells = BuildOccupiedMeleeCells(meleeEnemies);
        HashSet<string> reservedCells = new();

        for (int i = 0; i < meleeEnemies.Count; i++)
        {
            Enemy enemy = meleeEnemies[i];
            if (enemy == null || !enemy.IsAlive() || CanMeleeAttack(enemy))
                continue;

            RectTransform rectTransform = enemy.transform as RectTransform;
            if (rectTransform == null)
                continue;

            int nextColumn = Mathf.Max(meleeAttackColumn, enemy.gridColumn - 1);
            string currentCell = GetGridCellKey(enemy.gridRow, enemy.gridColumn);
            string targetCell = GetGridCellKey(enemy.gridRow, nextColumn);

            if (nextColumn == enemy.gridColumn ||
                occupiedCells.Contains(targetCell) ||
                reservedCells.Contains(targetCell))
            {
                continue;
            }

            Vector3 targetPosition = GetGridLocalPosition(enemy, enemy.gridRow, nextColumn);
            float fixedY = rectTransform.localPosition.y;

            if (Mathf.Abs(targetPosition.x - rectTransform.localPosition.x) <= 0.1f)
            {
                occupiedCells.Remove(currentCell);
                occupiedCells.Add(targetCell);
                enemy.gridColumn = nextColumn;
                continue;
            }

            occupiedCells.Remove(currentCell);
            reservedCells.Add(targetCell);

            enemy.PlayAnimation(enemy.moveAnim, true);
            rectTransform.DOKill();

            sequence.Insert(
                moveIndex * gridMoveStagger,
                rectTransform.DOLocalMoveX(
                    targetPosition.x,
                    enemyMoveDuration
                )
                .OnUpdate(() =>
                {
                    if (rectTransform != null)
                    {
                        Vector3 localPosition = rectTransform.localPosition;
                        localPosition.y = fixedY;
                        rectTransform.localPosition = localPosition;
                    }
                })
                .OnComplete(() =>
                {
                    if (enemy != null)
                        enemy.gridColumn = nextColumn;

                    reservedCells.Remove(targetCell);
                    occupiedCells.Add(targetCell);
                })
            );

            moveIndex++;
        }

        if (moveIndex == 0)
            yield break;

        yield return sequence.WaitForCompletion();

        for (int i = 0; i < meleeEnemies.Count; i++)
        {
            Enemy enemy = meleeEnemies[i];
            if (enemy != null && enemy.IsAlive())
                enemy.PlayAnimation(enemy.idleAnim, true);
        }
    }

    HashSet<string> BuildOccupiedMeleeCells(List<Enemy> meleeEnemies)
    {
        HashSet<string> cells = new();

        if (meleeEnemies == null)
            return cells;

        for (int i = 0; i < meleeEnemies.Count; i++)
        {
            Enemy enemy = meleeEnemies[i];
            if (enemy == null || !enemy.IsAlive())
                continue;

            cells.Add(GetGridCellKey(enemy.gridRow, enemy.gridColumn));
        }

        return cells;
    }

    string GetGridCellKey(int row, int column)
    {
        return row + ":" + column;
    }

    void SetEnemyGrid(Enemy enemy, int row, int column)
    {
        if (enemy == null)
            return;

        if (column <= playerColumn)
        {
            column = enemy.type == EnemyType.Range
                ? gridColumns - 1
                : gridColumns - 2;
        }

        enemy.gridRow = Mathf.Clamp(row, 0, Mathf.Max(0, gridRows - 1));
        enemy.gridColumn = Mathf.Clamp(column, playerColumn + 1, Mathf.Max(playerColumn + 1, gridColumns - 1));
    }

    void SetEnemyGridFromCurrentPosition(Enemy enemy)
    {
        if (enemy == null)
            return;

        RectTransform rectTransform = enemy.transform as RectTransform;
        if (rectTransform == null)
        {
            SetEnemyGrid(enemy, 1, enemy.type == EnemyType.Range ? gridColumns - 1 : gridColumns - 2);
            return;
        }

        int closestRow = 0;
        int closestColumn = playerColumn + 1;
        float bestDistance = float.PositiveInfinity;

        for (int row = 0; row < Mathf.Max(1, gridRows); row++)
        {
            for (int column = playerColumn + 1; column < Mathf.Max(2, gridColumns); column++)
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

    void SnapEnemyToGridPosition(Enemy enemy)
    {
        if (!snapSpawnedEnemiesToGrid || enemy == null)
            return;

        RectTransform rectTransform = enemy.transform as RectTransform;
        if (rectTransform == null)
            return;

        Vector3 gridPosition = GetGridLocalPosition(enemy, enemy.gridRow, enemy.gridColumn);
        rectTransform.localPosition = new Vector3(gridPosition.x, gridPosition.y, rectTransform.localPosition.z);
    }

    void SnapPlayerToConfiguredGrid()
    {
        if (player == null || spawnPositionGenerator == null)
            return;

        int row = spawnPositionGenerator.GetPlayerSpawnRow();
        int column = spawnPositionGenerator.GetPlayerSpawnColumn();
        Vector3 position = GetGridLocalPosition(row, column);

        RectTransform playerRect = player.GetComponent<RectTransform>();
        if (playerRect != null)
        {
            Transform targetParent = combatSpaceRoot != null
                ? combatSpaceRoot
                : (enemyRoot != null ? enemyRoot : transform);

            if (targetParent != null && playerRect.parent != targetParent)
                playerRect.SetParent(targetParent, false);

            playerRect.localPosition = position;
            return;
        }

        player.transform.localPosition = position;
    }

    Vector3 GetGridLocalPosition(int row, int column)
    {
        return GetGridLocalPosition(null, row, column);
    }

    Vector3 GetGridLocalPosition(Enemy enemy, int row, int column)
    {
        if (spawnArea != null && spawnArea.HasValidArea)
            return SpawnAreaPointToEnemyParentLocal(
                GetGridAreaLocalPosition(row, column),
                GetEnemyGridParent(enemy)
            );

        return new Vector3(column * enemySpacing, row * -enemySpacing, 0f);
    }

    Vector3 GetGridAreaLocalPosition(int row, int column)
    {
        int rows = Mathf.Max(1, gridRows);
        int columns = Mathf.Max(2, gridColumns);

        float x01 = columns <= 1 ? 0.5f : (float)Mathf.Clamp(column, 0, columns - 1) / (columns - 1);
        float y01 = rows <= 1 ? 0.5f : 1f - ((float)Mathf.Clamp(row, 0, rows - 1) / (rows - 1));
        return spawnArea.GetPoint(x01, y01);
    }

    Transform GetEnemyGridParent(Enemy enemy)
    {
        if (enemy != null && enemy.transform != null)
            return enemy.transform.parent;

        return combatSpaceRoot != null
            ? combatSpaceRoot
            : (enemyRoot != null ? enemyRoot : transform);
    }

    Vector3 SpawnAreaPointToEnemyParentLocal(Vector3 spawnAreaLocalPosition, Transform targetParent = null)
    {
        if (targetParent == null)
        {
            targetParent = combatSpaceRoot != null
                ? combatSpaceRoot
                : (enemyRoot != null ? enemyRoot : transform);
        }

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

        row.Sort(
            (a, b) =>
                GetCombatSpaceX(a.transform).CompareTo(
                    GetCombatSpaceX(b.transform)
                )
        );

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
                combatSpaceRoot != null
                    ? combatSpaceRoot
                    : (enemyRoot != null ? enemyRoot : transform),
                false
            );

            Vector3 localPosition = SpawnAreaPointToEnemyParentLocal(position, rectTransform.parent);

            rectTransform.localPosition =
                new Vector3(localPosition.x, localPosition.y, 0f);

            return;
        }

        if (rectTransform != null)
        {
            rectTransform.SetParent(
                combatSpaceRoot != null
                    ? combatSpaceRoot
                    : (enemyRoot != null ? enemyRoot : transform),
                false
            );
            rectTransform.localPosition = position;
            return;
        }

        enemy.transform.SetParent(
            combatSpaceRoot != null
                ? combatSpaceRoot
                : (enemyRoot != null ? enemyRoot : transform),
            false
        );
        enemy.transform.localPosition = position;
    }

    void RebuildLayout()
    {
        CleanupEnemies();

        for (int i = 0; i < enemies.Count; i++)
        {
            SetEnemyPosition(
                enemies[i],
                new Vector3(
                    i * enemySpacing,
                    0f,
                    0f
                ),
                false
            );
        }
    }

    void CleanupEnemies()
    {
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            if (enemies[i] == null)
            {
                enemies.RemoveAt(i);
            }
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
        Vector3 areaPoint = GetGridAreaLocalPosition(row, column);
        return spawnArea.spawnSpace == EnemySpawnSpace.World ? areaPoint : spawnArea.uiArea.TransformPoint(areaPoint);
    }
}


