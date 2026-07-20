using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "RuneDice/Hero Visual")]
public class HeroVisualData : SerializedScriptableObject
{
    public Dictionary<HeroType, HeroVisualConfig> heroVisual;
}
public class HeroVisualConfig
{
    public Sprite bg;
    public Sprite icon;
    public Sprite dieIcon;


}