using Sirenix.OdinInspector;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;


[CreateAssetMenu(menuName = "RuneDice/Rune/Rune UI")]
public class RuneUIData : SerializedScriptableObject
{
    public Dictionary<RuneType, Sprite> dicRuneUIData = new();
}
