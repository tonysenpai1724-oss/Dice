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
    public int def;
    public float critDmg = 1;
    public float critRate;
    public float luck;
    public HeroType type;
    public ERarity rarity;
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