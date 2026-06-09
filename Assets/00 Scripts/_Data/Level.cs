using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
[CreateAssetMenu(menuName = "RuneDice/Level")]
public class Level : SerializedScriptableObject
{
    public int levelNumber;
    public List<EnemyData> enemyDatas;
    [Header("Enemy Spawn")]
    public List<EnemySpawnPlacement> enemySpawnPlacements = new();
    //public DiceType[] diceTypes;

    [Button]
    public void ClearEnemySpawnPlacements()
    {
        enemySpawnPlacements.Clear();
    }
}
