using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using System.Collections.Generic;
[CreateAssetMenu(fileName = "Data Roll UI", menuName = "RuneDice/Data Roll UI")]
public class DataRollUI : SerializedScriptableObject
{
    public Dictionary<RollGuessType, RollItemData> rollItemDatas;
    public Dictionary<RollDiceType, RollItemBuffData> rollItemBuffDatas;

}
[System.Serializable]
public class RollItemData
{
    public Sprite bgSprite;
    public Sprite headerSprite;
    public Sprite iconDie1Sprite;
    public Sprite iconDie2Sprite;
    public Sprite typeIconSprite;
}

public class RollItemBuffData
{
    public Sprite bgSprite;
    public Sprite headerSprite;
    public Sprite bgIcon;

}
