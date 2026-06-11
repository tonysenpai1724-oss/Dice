using System.Collections.Generic;
using Unity;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

public class DiceEffectIconView : SerializedMonoBehaviour
{
    public RuneEffectIconView icons;
    public Sprite GetSprite(DiceType type)
    {

        return icons.icons[type];
    }
}
[CreateAssetMenu(menuName = "RuneDice/RuneEffectIconView")]
public class RuneEffectIconView : SerializedScriptableObject
{
    public Dictionary<DiceType, Sprite> icons = new();
}
