using Spine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Spine.Unity;
using Sirenix.OdinInspector;


[CreateAssetMenu(menuName = "RuneDice/Hero Data")]
public class HeroData : SerializedScriptableObject
{
    public int hp;
    public int level;
    public int damage;
    public int shield;
    public float critDmg = 1;
    public float critRate;
    public HeroType type;
    public Dictionary<int, List<DiceData>> startDiceLevelConfig;
    public SkeletonDataAsset skeletonData;
}
public enum HeroType
{
    Rogue,
    Mage,
    Warrior,
    Archer,
    Druid,
    Paladin,
    Necromancer,
    Bard,

}