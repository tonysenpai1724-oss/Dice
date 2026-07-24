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
    public int level = 1;
    public int coinReward;

    [Header("HP Bar")]
    public Vector2 hpBarScreenOffsetOverride = Vector2.zero;

    public int GetResolvedHp(int hpOverride, float hpMultiplier = 1f)
    {
        int value = hpOverride > 0 ? hpOverride : hp;
        return Mathf.Max(1, Mathf.RoundToInt(value * Mathf.Max(0f, hpMultiplier)));
    }

    public int GetResolvedDamage(int damageOverride, float damageMultiplier = 1f)
    {
        int value = damageOverride > 0 ? damageOverride : damage;
        return Mathf.Max(0, Mathf.RoundToInt(value * Mathf.Max(0f, damageMultiplier)));
    }
}

[System.Serializable]
public class EnemyConfig
{
    [Min(0)] public int hpOverride;
    [Min(0)] public int damageOverride;
    [Min(0f)] public float hpMultiplier = 1f;
    [Min(0f)] public float damageMultiplier = 1f;

    public int GetHp(EnemyData enemyData)
    {
        return enemyData != null ? enemyData.GetResolvedHp(hpOverride, hpMultiplier) : 0;
    }

    public int GetDamage(EnemyData enemyData)
    {
        return enemyData != null ? enemyData.GetResolvedDamage(damageOverride, damageMultiplier) : 0;
    }
}

[System.Serializable]
public class EnemyEntryConfig
{
    public EnemyData enemyData;
    public EnemyConfig config = new();

    public EnemyData Data => enemyData;

    public int GetHp()
    {
        return config != null ? config.GetHp(enemyData) : (enemyData != null ? enemyData.hp : 0);
    }

    public int GetDamage()
    {
        return config != null ? config.GetDamage(enemyData) : (enemyData != null ? enemyData.damage : 0);
    }
}
