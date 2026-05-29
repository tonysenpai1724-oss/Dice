using System.Collections;
using UnityEngine;
using System.Collections.Generic;
[CreateAssetMenu(menuName = "RuneDice/Level")]
public class Level : ScriptableObject
{
    public int levelNumber;
    public List<EnemyData> enemyDatas;
    //public DiceType[] diceTypes;
}