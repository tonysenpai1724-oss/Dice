using Sirenix.OdinInspector;
using UnityEngine;
using Spine.Unity;

public enum EPetRarity
{
    Common,
    Great,
    Rare,
    Epic,
    // Epic2 = 4,
    // Epic3 = 5,
    Legendary,
    // Legendary2 = 7,
    // Legendary3 = 8,
    // Legendary4 = 9,
    Mythic,
    // Mythic2 = 11,
    // Mythic3 = 12,
    // Mythic4 = 13,

}
public class PetData : SerializedMonoBehaviour
{
    public string id;
    public string petName;
    public string decs;
    public SkeletonDataAsset skeletonDataAsset;
    public EPetRarity rarity;
    public int level;


}
