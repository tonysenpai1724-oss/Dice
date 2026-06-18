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

[CreateAssetMenu(menuName = "RuneDice/Dice Visual Preset Database")]
public class DiceVisualPresetDatabaseSO : ScriptableObject
{
    public List<DiceVisualPresetEntry> presets = new();

    public DiceVisualPresetEntry GetPreset(DiceType diceType, int level)
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
}
