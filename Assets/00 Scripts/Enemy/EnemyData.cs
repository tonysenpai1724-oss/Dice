using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Spine.Unity;
using Spine;
using Sirenix.OdinInspector;
[CreateAssetMenu(menuName = "RuneDice/Enemy")]
public class EnemyData : SerializedScriptableObject
{
    public int hp;
    public string enemyName;
    public int damage;
    [Header("Scale")]
    public Vector3 scale = Vector3.one;
    public EnemyType type;
    public EnemyLevel enemyLevel;
    public SkeletonDataAsset skeletonData;
    [Min(0.1f)] public float attackAnimSpeed = 1f;
    // public int startDistance = 3;
    // public int attackRange = 1;
    public int level = 1;
    public int coinReward;


}

