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

    [Header("Visual Config")]
    public bool useVisualPreset = true;
    public DiceVisualPresetDatabaseSO visualPresetDatabase;
    public bool allowManualOverride = true;
    public string description;
    public string diceStatsDes;

    //public Mesh mesh;

    public Material diceMaterial;

    public List<Material> decalMaterial = new();
    // public Sprite diceSprite;
    public Color baseOutlineColor;
    public Color targetColor;
    public Color diceColor;
    public GameObject hitEffectPrefab;
    public DiceType type;
    public DiceEvoType evol;
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

        resolvedSkill.Execute(this);
    }

    [Button]
    public void ApplyVisualPreset()
    {
        if (visualPresetDatabase == null)
            return;

        DiceVisualPresetEntry preset = visualPresetDatabase.GetPreset(type, level);
        if (preset == null)
            return;

        diceMaterial = preset.diceMaterial;
        decalMaterial = new List<Material>(preset.decalMaterial);
        baseOutlineColor = preset.baseOutlineColor;
        targetColor = preset.targetColor;
        diceColor = preset.diceColor;
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

        if (useVisualPreset && visualPresetDatabase != null && !allowManualOverride)
            ApplyVisualPreset();
    }
}


