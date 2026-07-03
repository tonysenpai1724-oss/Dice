using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class EnemyMeleeTurnService
{
    readonly Func<List<Enemy>> getAliveMeleeEnemies;
    readonly Func<Enemy, bool> canMeleeAttack;
    readonly Func<Enemy, int, int, Vector3> getGridLocalPosition;
    readonly Func<int> getMeleeAttackColumn;
    readonly Func<float> getGridMoveStagger;
    readonly Func<float> getEnemyMoveDuration;

    public EnemyMeleeTurnService(
        Func<List<Enemy>> getAliveMeleeEnemies,
        Func<Enemy, bool> canMeleeAttack,
        Func<Enemy, int, int, Vector3> getGridLocalPosition,
        Func<int> getMeleeAttackColumn,
        Func<float> getGridMoveStagger,
        Func<float> getEnemyMoveDuration)
    {
        this.getAliveMeleeEnemies = getAliveMeleeEnemies;
        this.canMeleeAttack = canMeleeAttack;
        this.getGridLocalPosition = getGridLocalPosition;
        this.getMeleeAttackColumn = getMeleeAttackColumn;
        this.getGridMoveStagger = getGridMoveStagger;
        this.getEnemyMoveDuration = getEnemyMoveDuration;
    }

    public IEnumerator MoveMeleeEnemiesOneGridStep()
    {
        List<Enemy> meleeEnemies = getAliveMeleeEnemies?.Invoke();
        if (meleeEnemies == null || meleeEnemies.Count == 0)
            yield break;

        meleeEnemies.Sort((a, b) => a.gridColumn.CompareTo(b.gridColumn));

        Sequence sequence = DOTween.Sequence();
        int moveIndex = 0;
        HashSet<string> occupiedCells = BuildOccupiedMeleeCells(meleeEnemies);
        HashSet<string> reservedCells = new();
        int meleeAttackColumn = getMeleeAttackColumn();
        float gridMoveStagger = getGridMoveStagger();
        float enemyMoveDuration = getEnemyMoveDuration();

        for (int i = 0; i < meleeEnemies.Count; i++)
        {
            Enemy enemy = meleeEnemies[i];
            if (enemy == null || !enemy.IsAlive() || (canMeleeAttack != null && canMeleeAttack(enemy)))
                continue;

            RectTransform rectTransform = enemy.transform as RectTransform;
            if (rectTransform == null)
                continue;

            int nextColumn = Mathf.Max(meleeAttackColumn, enemy.gridColumn - 1);
            string currentCell = GetGridCellKey(enemy.gridRow, enemy.gridColumn);
            string targetCell = GetGridCellKey(enemy.gridRow, nextColumn);

            if (nextColumn == enemy.gridColumn || occupiedCells.Contains(targetCell) || reservedCells.Contains(targetCell))
                continue;

            Vector3 targetPosition = getGridLocalPosition != null
                ? getGridLocalPosition(enemy, enemy.gridRow, nextColumn)
                : rectTransform.localPosition;
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
                rectTransform.DOLocalMoveX(targetPosition.x, enemyMoveDuration)
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
}