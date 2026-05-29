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

    public void SpawnEnemies(List<EnemyData> enemyDatas)
    {
        ClearEnemies();

        if (enemyDatas == null || enemyPrefab == null)
            return;

        Transform root = enemyRoot != null ? enemyRoot : transform;

        for (int i = 0; i < enemyDatas.Count; i++)
        {
            EnemyData data = enemyDatas[i];
            if (data == null)
                continue;

            Enemy enemy = Instantiate(enemyPrefab, root);
            enemy.Setup(data);
            enemies.Add(enemy);
            SetEnemyPosition(enemy, enemies.Count - 1);
        }
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

        target.TakeDamage(damage);
        CheckWinGame();
    }

    public IEnumerator EnemyTurn()
    {
        CleanupEnemies();

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy == null || !enemy.IsAlive())
                continue;

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

        player.TakeDamage(enemy.damage);
    }

    void MoveEnemyTowardPlayer(Enemy enemy)
    {
        enemy.MoveTowardPlayer(meleeStepPerTurn);

        RectTransform rectTransform = enemy.transform as RectTransform;
        if (rectTransform != null)
        {
            rectTransform.DOAnchorPosX(
                rectTransform.anchoredPosition.x - meleeMoveDistance,
                enemyMoveDuration
            );
            return;
        }

        enemy.transform.DOMoveX(
            enemy.transform.position.x - meleeMoveDistance,
            enemyMoveDuration
        );
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
                enemies.RemoveAt(i);
        }
    }
}
