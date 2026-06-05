using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Spine.Unity;
using Spine;
[CreateAssetMenu(menuName = "RuneDice/Enemy")]
public class EnemyData : ScriptableObject
{
    public int hp;
    public string enemyName;
    public int damage;
    [Header("Scale")]
    public Vector3 scale = Vector3.one;
    public EnemyType type;
    public SkeletonDataAsset skeletonData;
    public int startDistance = 3;
    public int attackRange = 1;
    public int level = 1;


}
