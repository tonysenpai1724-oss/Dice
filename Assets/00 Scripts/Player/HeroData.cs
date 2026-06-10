using Spine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Spine.Unity;

[CreateAssetMenu(fileName = "New Hero Data", menuName = "Hero Data")]
public class HeroData : ScriptableObject
{
    public int hp;
    public int damage;
    public int shield;
    public HeroType type;

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