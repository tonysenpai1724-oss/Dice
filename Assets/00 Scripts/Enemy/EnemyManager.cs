using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class EnemyManager : Singleton<EnemyManager>
{
    public EffectManager effectManager;

    [Header("Refs")]
    public Enemy enemyPrefab;
    public Transform enemyRoot;
    public PlayerController player;
    public EnemyLevelPositionGenerator spawnPositionGenerator;
    public EnemySpawnArea spawnArea;
    public RectTransform combatSpaceRoot;
    public RectTransform attackPoint;

    [Header("Layout")]
    public float enemySpacing = 120f;
    public float meleeMoveDistance = 80f;
    public float enemyMoveDuration = 0.2f;

    [Header("Combat")]
    public int meleeStepPerTurn = 30;
    public float enemyActionDelay = 0.15f;

    public List<Enemy> enemies = new();
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

    public void SpawnEnemies(Level level)
    {
        // DebugCustom.LogColor(
        //     "Spawn Enemies: " +
        //     (level != null && level.enemyDatas != null ? level.enemyDatas.Count : 0)
        // );
        ClearEnemies();
        // DebugCustom.LogColor("Enemy Prefab: " + enemyPrefab);
        // DebugCustom.LogColor("Enemy Root: " + enemyRoot);
        // DebugCustom.LogColor("Enemy Datas: " + level);

        if (level == null || enemyPrefab == null)
        {
            // DebugCustom.LogColor("Enemy Datas or Prefab is null");
            return;
        }

        Transform root =
            combatSpaceRoot != null
                ? combatSpaceRoot
                : (enemyRoot != null ? enemyRoot : transform);
        List<EnemySpawnPlacement> placements =
            level.enemySpawnPlacements != null &&
            level.enemySpawnPlacements.Count > 0
                ? level.enemySpawnPlacements
                : spawnPositionGenerator != null
                    ? spawnPositionGenerator.BuildPlacements(level.enemyDatas)
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
                //                DebugCustom.LogColor("Spawn Enemy: " + enemy.name);
                enemy.Setup(placement.data);
                enemies.Add(enemy);
                SetEnemyPosition(
                    enemy,
                    placement.position,
                    placement.useUIPosition
                );
            }

            return;
        }

        List<EnemyData> enemyDatas = level.enemyDatas;

        for (int i = 0; i < enemyDatas.Count; i++)
        {
            EnemyData data = enemyDatas[i];
            //    DebugCustom.LogColor("Enemy Data: " + data);
            if (data == null)
                continue;

            Enemy enemy = Instantiate(enemyPrefab, root, false);
            RegisterEnemy(enemy);
            //  DebugCustom.LogColor("Spawn Enemy: " + enemy.name);
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
        }
    }
    void SpawnProjectile(Enemy target, int damage)
    {
        if (projectilePrefab == null || target == null)
            return;

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
            target.OnTakeDamage(damage);
            Destroy(projectile.gameObject);
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
            return rectTransform.anchoredPosition.x;

        return target.localPosition.x;
    }

    public Enemy GetNearestAliveEnemy()
    {
        CleanupEnemies();

        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != null && enemies[i].IsAlive())
                return enemies[i];
        }

        return null;
    }

    public void PlayerAttack(DiceData diceData)
    {
        if (diceData == null)
            return;

        PlayerAttack(diceData.damage);
    }

    public void PlayerAttack(int damage)
    {
        Enemy target = GetNearestAliveEnemy();
        if (target == null)
        {
            CheckWinGame();
            return;
        }
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

        EnemyTurnSkipEffect turnSkipEffect = effectManager?.GetEffect<EnemyTurnSkipEffect>();
        if (turnSkipEffect != null && turnSkipEffect.ConsumeTurnSkip())
        {
            yield break;
        }

        Enemy frontEnemy = GetFrontEnemy();
        if (frontEnemy == null)
        {
            CheckWinGame();
            yield break;
        }

        if (frontEnemy.type == EnemyType.Range && !IsAtAttackPoint(frontEnemy))
        {
            yield return MoveEnemyRowToAttackPoint(EnemyType.Range);
            frontEnemy = GetFrontEnemy();
        }

        List<Enemy> attackers = GetEnemyTurnAttackers(frontEnemy);

        for (int i = 0; i < attackers.Count; i++)
        {
            Enemy attacker = attackers[i];
            if (attacker == null || !attacker.IsAlive())
                continue;

            if (attacker == frontEnemy && attacker.type == EnemyType.Range && !IsAtAttackPoint(frontEnemy))
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

        DiceQueue queue = DiceManager.Instance != null ? DiceManager.Instance.diceQueue : null;
        if (queue != null && queue.IsBusy)
        {
            queue.RequestFastFlush();
            return;
        }

        GameplayManager.Instance.EndGame(true);
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
        if (attackTrack != null)
            attackTrack.Complete += _ => attackCompleted = true;

        player.OnTakeDamage(enemy.damage);

        if (attackTrack != null)
        {
            while (!attackCompleted && enemy != null && enemy.IsAlive())
                yield return null;
        }

        if (enemy.skeletonGraphic != null)
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

        AddEnemyTurnAttacker(attackers, frontEnemy);

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy == null || !enemy.IsAlive())
                continue;

            if (enemy.type == EnemyType.Melee)
                AddEnemyTurnAttacker(attackers, enemy);
        }

        AddEnemyTurnAttacker(attackers, GetFrontEnemyOfType(EnemyType.Range));

        return attackers;
    }

    void AddEnemyTurnAttacker(List<Enemy> attackers, Enemy enemy)
    {
        if (enemy == null || !enemy.IsAlive() || attackers.Contains(enemy))
            return;

        attackers.Add(enemy);
    }

    bool IsAtAttackPoint(Enemy enemy)
    {
        if (enemy == null || attackPoint == null)
            return false;

        float enemyX = GetCombatSpaceX(enemy.transform);
        float attackX = GetCombatSpaceX(attackPoint);

        return Mathf.Abs(enemyX - attackX) <= 1f;
    }

    IEnumerator MoveFrontEnemyToAttackPoint(Enemy enemy)
    {
        if (enemy == null || attackPoint == null)
            yield break;

        enemy.PlayAnimation(enemy.moveAnim, true);

        RectTransform enemyRect =
            enemy.transform as RectTransform;
        RectTransform attackRect =
            attackPoint as RectTransform;

        if (enemyRect == null || attackRect == null)
        {
            if (enemy != null && enemy.IsAlive())
                enemy.PlayAnimation(enemy.idleAnim, true);

            yield break;
        }

        float enemyX = enemyRect.anchoredPosition.x;
        float targetX = attackRect.anchoredPosition.x;
        float dx = targetX - enemyX;

        if (Mathf.Abs(dx) <= 0.1f)
        {
            enemy.PlayAnimation(enemy.idleAnim, true);
            yield break;
        }

        float moveDistance =
            Mathf.Min(
                meleeMoveDistance,
                Mathf.Max(0f, Mathf.Abs(dx) - 0.1f)
            );

        if (moveDistance <= 0.01f)
        {
            if (enemy != null && enemy.IsAlive())
                enemy.PlayAnimation(enemy.idleAnim, true);
            yield break;
        }

        float direction = Mathf.Sign(dx);

        Tween tween =
            enemyRect.DOAnchorPosX(
                enemyRect.anchoredPosition.x + (direction * moveDistance),
                enemyMoveDuration
            );

        yield return tween.WaitForCompletion();

        if (enemyRect != null)
        {
            enemyRect.anchoredPosition =
                new Vector2(
                    attackRect.anchoredPosition.x,
                    enemyRect.anchoredPosition.y
                );
        }

        if (enemy != null && enemy.IsAlive())
            enemy.PlayAnimation(enemy.idleAnim, true);
    }

    IEnumerator MoveEnemyRowToAttackPoint(EnemyType type)
    {
        if (attackPoint == null)
            yield break;

        List<Enemy> row =
            GetAliveEnemiesOfType(type);

        if (row.Count == 0)
            yield break;

        Enemy frontEnemy = row[0];
        float frontX = GetCombatSpaceX(frontEnemy.transform);
        float targetX = GetCombatSpaceX(attackPoint);
        float dx = targetX - frontX;

        if (Mathf.Abs(dx) <= 1f)
            yield break;

        float moveDistance =
            Mathf.Min(
                meleeMoveDistance,
                Mathf.Max(0f, Mathf.Abs(dx) - 1f)
            );

        if (moveDistance <= 0.01f)
            yield break;

        float direction = Mathf.Sign(dx);

        Sequence seq = DOTween.Sequence();
        for (int i = 0; i < row.Count; i++)
        {
            Enemy enemy = row[i];
            if (enemy == null || !enemy.IsAlive())
                continue;

            RectTransform rectTransform = enemy.transform as RectTransform;
            if (rectTransform == null)
                continue;

            enemy.PlayAnimation(enemy.moveAnim, true);
            seq.Join(
                rectTransform.DOAnchorPosX(
                    rectTransform.anchoredPosition.x + (direction * moveDistance),
                    enemyMoveDuration
                )
            );
        }

        yield return seq.WaitForCompletion();

        for (int i = 0; i < row.Count; i++)
        {
            Enemy enemy = row[i];
            if (enemy != null && enemy.IsAlive())
                enemy.PlayAnimation(enemy.idleAnim, true);
        }
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

    void SetEnemyPosition(
        Enemy enemy,
        Vector3 position,
        bool useUIPosition
    )
    {
        RectTransform rectTransform = enemy.transform as RectTransform;
        if (useUIPosition && rectTransform != null)
        {
            rectTransform.SetParent(
                combatSpaceRoot != null
                    ? combatSpaceRoot
                    : (enemyRoot != null ? enemyRoot : transform),
                false
            );

            Vector3 localPosition = position;
            if (spawnArea != null &&
                spawnArea.uiArea != null &&
                combatSpaceRoot != null)
            {
                Vector3 worldPoint =
                    spawnArea.uiArea.TransformPoint(
                        new Vector3(position.x, position.y, 0f)
                    );

                localPosition =
                    combatSpaceRoot.InverseTransformPoint(worldPoint);
            }

            rectTransform.anchoredPosition3D =
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

}
