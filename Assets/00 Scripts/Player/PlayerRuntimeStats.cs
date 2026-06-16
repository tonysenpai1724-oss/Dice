using System;
using System.Collections.Generic;
using UnityEngine;

public enum HeroStatModifierMode
{
    Flat,
    PercentFromBase
}

[Serializable]
public class HeroStatModifier
{
    public string sourceId;
    public HeroStatType statType;
    public HeroStatModifierMode mode;
    public float amount;
}

public class PlayerRuntimeStats : MonoBehaviour
{
    [SerializeField] HeroStatSnapshot baseStats = new();
    [SerializeField] HeroStatSnapshot finalStats = new();
    [SerializeField] List<HeroStatModifier> modifiers = new();

    public HeroStatSnapshot BaseStats => baseStats;
    public HeroStatSnapshot FinalStats => finalStats;
    public IReadOnlyList<HeroStatModifier> Modifiers => modifiers;

    public event Action<HeroStatSnapshot> StatsChanged;

    public void SetBaseStats(HeroData heroData)
    {
        baseStats = new HeroStatSnapshot(heroData);
        Recalculate();
    }

    public void ClearModifiers()
    {
        modifiers.Clear();
        Recalculate();
    }

    public void SetModifiers(IEnumerable<HeroStatModifier> newModifiers)
    {
        modifiers.Clear();

        if (newModifiers != null)
        {
            foreach (HeroStatModifier modifier in newModifiers)
            {
                if (modifier == null)
                    continue;

                modifiers.Add(modifier);
            }
        }

        Recalculate();
    }

    public void AddModifier(HeroStatModifier modifier)
    {
        if (modifier == null)
            return;

        modifiers.Add(modifier);
        Recalculate();
    }

    public void RemoveModifiersBySource(string sourceId)
    {
        if (string.IsNullOrEmpty(sourceId))
            return;

        modifiers.RemoveAll(x => x != null && x.sourceId == sourceId);
        Recalculate();
    }

    public void Recalculate()
    {
        finalStats = Clone(baseStats);

        for (int i = 0; i < modifiers.Count; i++)
        {
            HeroStatModifier modifier = modifiers[i];
            if (modifier == null)
                continue;

            float appliedValue = modifier.mode == HeroStatModifierMode.PercentFromBase
                ? baseStats.GetValue(modifier.statType) * modifier.amount * 0.01f
                : modifier.amount;

            finalStats.Add(modifier.statType, appliedValue);
        }

        StatsChanged?.Invoke(finalStats);
    }

    HeroStatSnapshot Clone(HeroStatSnapshot source)
    {
        return new HeroStatSnapshot
        {
            hp = source != null ? source.hp : 0,
            damage = source != null ? source.damage : 0,
            defense = source != null ? source.defense : 0,
            critDamage = source != null ? source.critDamage : 0f,
            critRate = source != null ? source.critRate : 0f,
            luck = source != null ? source.luck : 0f
        };
    }
}
