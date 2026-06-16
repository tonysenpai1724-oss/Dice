using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "RuneDice/Dice")]
public class DiceData : SerializedScriptableObject
{
    public string diceName;

    public int level;

    public int damage;
    [Min(1)] public int attackCount = 1;
    public ERarity rarity;

    //public Mesh mesh;

    public Material diceMaterial;

    public List<Material> decalMaterial;
    public Sprite diceSprite;
    public Color baseOutlineColor;
    public Color targetColor;
    public Color diceColor;
    public GameObject hitEffectPrefab;
    public DiceType type;
    [Header("Skill")]
    public DiceSkillData skillData;

    public bool CanUpgrade
    {
        get
        {
            return DiceManager.Instance != null &&
                   DiceManager.Instance.GetDiceDataByLevelAndType(level + 1, type) != null;
        }
    }

    public DiceData GetUpgradeData()
    {
        return DiceManager.Instance != null
            ? DiceManager.Instance.GetDiceDataByLevelAndType(level + 1, type)
            : null;
    }

    public bool TryUpgrade(out DiceData upgradedDiceData)
    {
        upgradedDiceData = GetUpgradeData();
        return upgradedDiceData != null;
    }

    public void ExecuteSkill()
    {
        DiceSkillData resolvedSkill =
            skillData != null
                ? skillData
                : DiceSkillFactory.Create(type);

        if (resolvedSkill == null)
            return;

        resolvedSkill.Execute();
    }

    void OnValidate()
    {
        if (skillData != null &&
            skillData.TargetType != type)
        {
            Debug.LogWarning(
                $"{name}: skill {skillData.name} targets {skillData.TargetType} but dice type is {type}",
                this
            );
        }
    }
}



