using System;
using System.Collections.Generic;

public class EnemyQueryService
{
    readonly Func<List<Enemy>> getEnemies;
    readonly Action cleanupEnemies;
    readonly Func<UnityEngine.Transform, float> getCombatSpaceX;
    readonly Func<PlayerController> getPlayer;

    public EnemyQueryService(
        Func<List<Enemy>> getEnemies,
        Action cleanupEnemies,
        Func<UnityEngine.Transform, float> getCombatSpaceX,
        Func<PlayerController> getPlayer)
    {
        this.getEnemies = getEnemies;
        this.cleanupEnemies = cleanupEnemies;
        this.getCombatSpaceX = getCombatSpaceX;
        this.getPlayer = getPlayer;
    }

    public Enemy GetNearestAliveEnemy()
    {
        cleanupEnemies?.Invoke();
        List<Enemy> enemies = getEnemies?.Invoke();
        if (enemies == null)
            return null;

        Enemy nearestEnemy = null;
        float bestX = float.PositiveInfinity;

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy == null || !enemy.IsAlive())
                continue;

            float enemyX = getCombatSpaceX(enemy.transform);
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
        cleanupEnemies?.Invoke();
        List<Enemy> enemies = getEnemies?.Invoke();
        if (enemies == null)
            return null;

        Enemy rightmostEnemy = null;
        float bestX = float.NegativeInfinity;

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy == null || !enemy.IsAlive())
                continue;

            float enemyX = getCombatSpaceX(enemy.transform);
            if (enemyX > bestX)
            {
                bestX = enemyX;
                rightmostEnemy = enemy;
            }
        }

        return rightmostEnemy;
    }

    public Enemy GetRandomAliveEnemy()
    {
        cleanupEnemies?.Invoke();
        List<Enemy> enemies = getEnemies?.Invoke();
        if (enemies == null)
            return null;

        List<Enemy> aliveEnemies = new();
        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy != null && enemy.IsAlive())
                aliveEnemies.Add(enemy);
        }

        if (aliveEnemies.Count <= 0)
            return null;

        return aliveEnemies[UnityEngine.Random.Range(0, aliveEnemies.Count)];
    }

    public bool HasAliveEnemies()
    {
        List<Enemy> enemies = getEnemies?.Invoke();
        if (enemies == null)
            return false;

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy != null && enemy.IsAlive())
                return true;
        }

        return false;
    }

    public Enemy GetFrontEnemy()
    {
        cleanupEnemies?.Invoke();
        List<Enemy> enemies = getEnemies?.Invoke();
        PlayerController player = getPlayer?.Invoke();
        if (enemies == null)
            return null;

        Enemy frontEnemy = null;
        float bestX = float.PositiveInfinity;
        float playerX = player != null ? getCombatSpaceX(player.transform) : float.NegativeInfinity;

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy == null || !enemy.IsAlive())
                continue;

            float enemyX = getCombatSpaceX(enemy.transform);
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

    public Enemy GetFrontEnemyOfType(EnemyType type)
    {
        cleanupEnemies?.Invoke();
        List<Enemy> enemies = getEnemies?.Invoke();
        PlayerController player = getPlayer?.Invoke();
        if (enemies == null)
            return null;

        Enemy frontEnemy = null;
        float bestX = float.PositiveInfinity;
        float playerX = player != null ? getCombatSpaceX(player.transform) : float.NegativeInfinity;

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy == null || !enemy.IsAlive())
                continue;

            if (enemy.type != type)
                continue;

            float enemyX = getCombatSpaceX(enemy.transform);
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
}
