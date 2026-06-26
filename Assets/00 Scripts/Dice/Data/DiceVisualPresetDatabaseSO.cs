using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DiceVisualPresetEntry
{
    public DiceType diceType;
    public int level = 1;
    public Material diceMaterial;
    public List<Material> decalMaterial = new();
    public Color baseOutlineColor = Color.black;
    public Color targetColor = Color.white;
    public Color diceColor = Color.white;
}

[Serializable]
public class DiceBaseLevelMaterialEntry
{
    [Range(1, 9)] public int level = 1;
    public Material diceMaterial;
    public Color diceColor = Color.white;
}

[Serializable]
public class DiceTypeLevelDecalEntry
{
    [Range(1, 9)] public int level = 1;
    public List<Material> decalMaterial = new();
}

[Serializable]
public class DiceTypeVisualPresetEntry
{
    public DiceType diceType;
    public List<DiceTypeLevelDecalEntry> levels = new();
}

[CreateAssetMenu(menuName = "RuneDice/Dice Visual Preset Database")]
public class DiceVisualPresetDatabaseSO : ScriptableObject
{
    [Header("Base Material By Level")]
    public List<DiceBaseLevelMaterialEntry> baseLevelMaterials = new();

    [Header("Decal Materials By Dice Type")]
    public List<DiceTypeVisualPresetEntry> diceTypePresets = new();

    [Header("Default Colors")]
    public Color baseOutlineColor = Color.black;
    public Color targetColor = Color.white;

    [Header("Legacy Presets")]
    public List<DiceVisualPresetEntry> presets = new();

    void OnValidate()
    {
        if (baseLevelMaterials.Count == 0 && diceTypePresets.Count == 0 && presets.Count > 0)
            MigrateLegacyPresets();
    }

    public DiceVisualPresetEntry GetPreset(DiceType diceType, int level)
    {
        DiceBaseLevelMaterialEntry baseLevel = GetBaseLevelMaterial(level);
        DiceTypeLevelDecalEntry typeLevel = GetTypeLevelDecal(diceType, level);
        DiceVisualPresetEntry legacyPreset = GetLegacyPreset(diceType, level);

        if (baseLevel == null && typeLevel == null && legacyPreset == null)
            return null;

        return new DiceVisualPresetEntry
        {
            diceType = diceType,
            level = level,
            diceMaterial = baseLevel != null && baseLevel.diceMaterial != null
                ? baseLevel.diceMaterial
                : legacyPreset != null ? legacyPreset.diceMaterial : null,
            decalMaterial = typeLevel != null && typeLevel.decalMaterial != null
                ? new List<Material>(typeLevel.decalMaterial)
                : legacyPreset != null ? new List<Material>(legacyPreset.decalMaterial) : new List<Material>(),
            baseOutlineColor = legacyPreset != null ? legacyPreset.baseOutlineColor : baseOutlineColor,
            targetColor = legacyPreset != null ? legacyPreset.targetColor : targetColor,
            diceColor = baseLevel != null ? baseLevel.diceColor : legacyPreset != null ? legacyPreset.diceColor : Color.white
        };
    }

    public DiceBaseLevelMaterialEntry GetBaseLevelMaterial(int level)
    {
        for (int i = 0; i < baseLevelMaterials.Count; i++)
        {
            DiceBaseLevelMaterialEntry entry = baseLevelMaterials[i];
            if (entry == null)
                continue;

            if (entry.level == level)
                return entry;
        }

        return null;
    }

    public DiceTypeLevelDecalEntry GetTypeLevelDecal(DiceType diceType, int level)
    {
        for (int i = 0; i < diceTypePresets.Count; i++)
        {
            DiceTypeVisualPresetEntry typePreset = diceTypePresets[i];
            if (typePreset == null || typePreset.diceType != diceType)
                continue;

            for (int j = 0; j < typePreset.levels.Count; j++)
            {
                DiceTypeLevelDecalEntry levelPreset = typePreset.levels[j];
                if (levelPreset == null)
                    continue;

                if (levelPreset.level == level)
                    return levelPreset;
            }
        }

        return null;
    }

    DiceVisualPresetEntry GetLegacyPreset(DiceType diceType, int level)
    {
        for (int i = 0; i < presets.Count; i++)
        {
            DiceVisualPresetEntry preset = presets[i];
            if (preset == null)
                continue;

            if (preset.diceType == diceType && preset.level == level)
                return preset;
        }

        return null;
    }

    [ContextMenu("Ensure Base Levels 1-9")]
    public void EnsureBaseLevels()
    {
        for (int level = 1; level <= 9; level++)
        {
            if (GetBaseLevelMaterial(level) != null)
                continue;

            baseLevelMaterials.Add(new DiceBaseLevelMaterialEntry
            {
                level = level
            });
        }
    }

    [ContextMenu("Migrate Legacy Presets")]
    public void MigrateLegacyPresets()
    {
        EnsureBaseLevels();

        for (int i = 0; i < presets.Count; i++)
        {
            DiceVisualPresetEntry legacyPreset = presets[i];
            if (legacyPreset == null)
                continue;

            DiceBaseLevelMaterialEntry baseLevel = GetBaseLevelMaterial(legacyPreset.level);
            if (baseLevel != null && baseLevel.diceMaterial == null)
            {
                baseLevel.diceMaterial = legacyPreset.diceMaterial;
                baseLevel.diceColor = legacyPreset.diceColor;
            }

            DiceTypeVisualPresetEntry typePreset = GetOrCreateTypePreset(legacyPreset.diceType);
            DiceTypeLevelDecalEntry levelPreset = GetOrCreateLevelPreset(typePreset, legacyPreset.level);
            if (levelPreset.decalMaterial.Count == 0 && legacyPreset.decalMaterial != null)
                levelPreset.decalMaterial = new List<Material>(legacyPreset.decalMaterial);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Apply Presets To All DiceData")]
    public void ApplyPresetsToAllDiceData()
    {
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:DiceData");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            DiceData diceData = UnityEditor.AssetDatabase.LoadAssetAtPath<DiceData>(path);
            if (diceData == null || diceData.visualPresetDatabase != this)
                continue;

            diceData.ApplyVisualPreset();
            UnityEditor.EditorUtility.SetDirty(diceData);
        }

        UnityEditor.AssetDatabase.SaveAssets();
    }
#endif

    DiceTypeVisualPresetEntry GetOrCreateTypePreset(DiceType diceType)
    {
        for (int i = 0; i < diceTypePresets.Count; i++)
        {
            DiceTypeVisualPresetEntry typePreset = diceTypePresets[i];
            if (typePreset != null && typePreset.diceType == diceType)
                return typePreset;
        }

        DiceTypeVisualPresetEntry newPreset = new DiceTypeVisualPresetEntry
        {
            diceType = diceType
        };
        diceTypePresets.Add(newPreset);
        return newPreset;
    }

    DiceTypeLevelDecalEntry GetOrCreateLevelPreset(DiceTypeVisualPresetEntry typePreset, int level)
    {
        for (int i = 0; i < typePreset.levels.Count; i++)
        {
            DiceTypeLevelDecalEntry levelPreset = typePreset.levels[i];
            if (levelPreset != null && levelPreset.level == level)
                return levelPreset;
        }

        DiceTypeLevelDecalEntry newPreset = new DiceTypeLevelDecalEntry
        {
            level = level
        };
        typePreset.levels.Add(newPreset);
        return newPreset;
    }
}
