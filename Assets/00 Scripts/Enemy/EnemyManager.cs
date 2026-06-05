using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class EnemyManager : Singleton<EnemyManager>
{
    [Header("Refs")]
    public Enemy enemyPrefab;
    public Transform enemyRoot;
    public PlayerController player;

    [Header("Layout")]
    public float enemySpacing = 120f;
    public float meleeMoveDistance = 80f;
    public float enemyMoveDuration = 0.2f;

    [Header("Combat")]
    public int meleeStepPerTurn = 1;
    public float enemyActionDelay = 0.15f;

    public List<Enemy> enemies = new();
    [Header("Projectile")]
    public RectTransform projectilePrefab;
    public Transform projectileRoot;
    public float projectileSpeed = 2400f;
    public Vector3 projectileOffset = new Vector3(0, 4f, 0);
    public float projectileRotationOffset;
    public int skipNextEnemyTurns;
    public int nextPlayerDamageReduction;
    readonly Dictionary<Enemy, PoisonStatus> poisonStatuses = new();
    readonly List<Enemy> pendingPoisonRemovals = new();

    public void SpawnEnemies(List<EnemyData> enemyDatas)
    {
        DebugCustom.LogColor("Spawn Enemies: " + enemyDatas.Count);
        ClearEnemies();
        DebugCustom.LogColor("Enemy Prefab: " + enemyPrefab);
        DebugCustom.LogColor("Enemy Root: " + enemyRoot);
        DebugCustom.LogColor("Enemy Datas: " + enemyDatas);


        if (enemyDatas == null || enemyPrefab == null)
        {
            DebugCustom.LogColor("Enemy Datas or Prefab is null");
            return;
        }

        Transform root = enemyRoot != null ? enemyRoot : transform;

        for (int i = 0; i < enemyDatas.Count; i++)
        {
            EnemyData data = enemyDatas[i];
            DebugCustom.LogColor("Enemy Data: " + data);
            if (data == null)
                continue;

            Enemy enemy = Instantiate(enemyPrefab, root);
            DebugCustom.LogColor("Spawn Enemy: " + enemy.name);
            enemy.Setup(data);
            enemies.Add(enemy);
            SetEnemyPosition(enemy, enemies.Count - 1);
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
            target.TakeDamage(damage);
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

        if (skipNextEnemyTurns > 0)
        {
            skipNextEnemyTurns--;
            yield break;
        }

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy == null || !enemy.IsAlive())
                continue;

            enemy.SetDistanceToPlayer(
                GetDistanceToPlayer(enemy)
            );

            if (enemy.CanAttack())
            {
                AttackPlayer(enemy);
            }
            else
            {
                MoveEnemyTowardPlayer(enemy);
            }

            if (enemyActionDelay > 0f)
                yield return new WaitForSeconds(enemyActionDelay);
        }
    }

    public void RemoveEnemy(Enemy enemy)
    {
        if (enemy == null)
            return;

        enemies.Remove(enemy);
        if (!pendingPoisonRemovals.Contains(enemy))
            pendingPoisonRemovals.Add(enemy);
        Destroy(enemy.gameObject);
        // RebuildLayout();
        CheckWinGame();
    }

    void CheckWinGame()
    {
        CleanupEnemies();

        if (enemies.Count == 0 && GameplayManager.Instance != null && !GameplayManager.Instance.winGame)
            GameplayManager.Instance.EndGame(true);
    }

    void AttackPlayer(Enemy enemy)
    {
        if (player == null || enemy == null)
            return;

        enemy.PlayAnimation(enemy.attackAnim, false);

        int damage =
            Mathf.Max(0, enemy.damage - nextPlayerDamageReduction);
        nextPlayerDamageReduction = 0;

        player.TakeDamage(damage);

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

        skipNextEnemyTurns += amount;
    }

    public void ReduceNextPlayerDamage(int amount)
    {
        if (amount <= 0)
            return;

        nextPlayerDamageReduction += amount;
    }

    public void DamageAllEnemies(int amount)
    {
        if (amount <= 0)
            return;

        CleanupEnemies();

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy == null || !enemy.IsAlive())
                continue;

            enemy.TakeDamage(amount);
        }
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

        if (!poisonStatuses.TryGetValue(target, out PoisonStatus status) ||
            status == null)
        {
            status = new PoisonStatus();
            poisonStatuses[target] = status;
        }

        status.turnsRemaining += turns;
        status.damagePerTurn = Mathf.Max(status.damagePerTurn, damagePerTurn);
    }

    void ApplyPoisonTicks()
    {
        CleanupPendingPoisonRemovals();

        if (poisonStatuses.Count == 0)
            return;

        List<Enemy> expired = null;

        foreach (var pair in poisonStatuses)
        {
            Enemy enemy = pair.Key;
            PoisonStatus status = pair.Value;

            if (enemy == null ||
                status == null ||
                !enemy.gameObject.activeInHierarchy)
            {
                expired ??= new List<Enemy>();
                expired.Add(enemy);
                continue;
            }

            if (status.turnsRemaining <= 0)
            {
                expired ??= new List<Enemy>();
                expired.Add(enemy);
                continue;
            }

            enemy.TakeDamage(status.damagePerTurn);
            status.turnsRemaining--;

            if (status.turnsRemaining <= 0)
            {
                expired ??= new List<Enemy>();
                expired.Add(enemy);
            }
        }

        if (expired == null)
        {
            CleanupPendingPoisonRemovals();
            return;
        }

        for (int i = 0; i < expired.Count; i++)
        {
            if (expired[i] != null)
                poisonStatuses.Remove(expired[i]);
        }

        CleanupPendingPoisonRemovals();
    }

    void OnDisable()
    {
        poisonStatuses.Clear();
        pendingPoisonRemovals.Clear();
    }

    void MoveEnemyTowardPlayer(Enemy enemy)
    {
        float distance =
            GetDistanceToPlayer(enemy);

        float moveAmount =
            Mathf.Max(
                0f,
                distance - enemy.attackRange
            );

        moveAmount =
            Mathf.Min(
                moveAmount,
                meleeMoveDistance
            );

        enemy.MoveTowardPlayer(moveAmount);

        RectTransform rectTransform = enemy.transform as RectTransform;
        if (rectTransform != null)
        {
            rectTransform.DOAnchorPosX(
                rectTransform.anchoredPosition.x - moveAmount,
                enemyMoveDuration
            );
            return;
        }

        enemy.transform.DOMoveX(
            enemy.transform.position.x - moveAmount,
            enemyMoveDuration
        );
    }

    float GetDistanceToPlayer(Enemy enemy)
    {
        if (enemy == null || player == null)
            return float.MaxValue;

        Vector3 enemyPos = GetActorPosition(enemy.transform);
        Vector3 playerPos = GetActorPosition(player.transform);

        return Vector3.Distance(enemyPos, playerPos);
    }

    Vector3 GetActorPosition(Transform actor)
    {
        if (actor == null)
            return Vector3.zero;

        RectTransform rectTransform = actor as RectTransform;
        if (rectTransform != null)
            return rectTransform.position;

        return actor.position;
    }

    void SetEnemyPosition(Enemy enemy, int index)
    {
        RectTransform rectTransform = enemy.transform as RectTransform;
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition =
                new Vector2(index * enemySpacing, rectTransform.anchoredPosition.y);
            return;
        }

        enemy.transform.localPosition =
            new Vector3(index * enemySpacing, enemy.transform.localPosition.y, enemy.transform.localPosition.z);
    }

    void RebuildLayout()
    {
        CleanupEnemies();

        for (int i = 0; i < enemies.Count; i++)
            SetEnemyPosition(enemies[i], i);
    }

    void CleanupEnemies()
    {
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            if (enemies[i] == null)
            {
                poisonStatuses.Remove(enemies[i]);
                enemies.RemoveAt(i);
            }
        }
    }

    void CleanupPendingPoisonRemovals()
    {
        if (pendingPoisonRemovals.Count == 0)
            return;

        for (int i = 0; i < pendingPoisonRemovals.Count; i++)
        {
            Enemy enemy = pendingPoisonRemovals[i];
            if (enemy != null)
                poisonStatuses.Remove(enemy);
        }

        pendingPoisonRemovals.Clear();
    }

    class PoisonStatus
    {
        public int turnsRemaining;
        public int damagePerTurn;
    }
}
