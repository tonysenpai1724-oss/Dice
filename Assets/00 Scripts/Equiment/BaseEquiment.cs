using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public enum EquipmentType
{
    Weapon,
    Helmet,
    Armor,
    Gloves,
    Boots,
    Ring,
    Necklace,
    Artifact
}

public enum HeroStatType
{
    Hp,
    Damage,
    Defense,
    CritDamage,
    CritRate,
    Luck
}

public enum EquipmentStatModifierType
{
    Flat,
    Percent
}

[Serializable]
public class EquipmentStatBonus
{
    public HeroStatType statType;
    public EquipmentStatModifierType modifierType;
    public float amount;

    [ShowInInspector, ReadOnly]
    public string Preview => GetPreviewText();

    public string GetPreviewText()
    {
        string suffix = modifierType == EquipmentStatModifierType.Percent ? "%" : string.Empty;
        string sign = amount >= 0f ? "+" : string.Empty;
        return $"{statType}: {sign}{amount:0.##}{suffix}";
    }
}

[Serializable]
public class HeroStatSnapshot
{
    public int hp;
    public int damage;
    public int defense;
    public float critDamage;
    public float critRate;
    public float luck;

    public HeroStatSnapshot()
    {
    }

    public HeroStatSnapshot(HeroData heroData)
    {
        if (heroData == null)
            return;

        hp = heroData.hp;
        damage = heroData.damage;
        defense = heroData.def;
        critDamage = heroData.critDmg;
        critRate = heroData.critRate;
        luck = heroData.luck;
    }
}

public interface IHeroEquipment
{
    EquipmentType EquipmentType { get; }
    IReadOnlyList<EquipmentStatBonus> StatBonuses { get; }
}

public abstract class BaseEquipmentEffect : SerializedScriptableObject
{
    public virtual void OnEquip(PlayerController player, BaseEquiment equipment)
    {
    }

    public virtual void OnUnequip(PlayerController player, BaseEquiment equipment)
    {
    }

    protected T GetOrAddRuntimeEffect<T>(PlayerController player, BaseEquiment equipment)
        where T : EquipmentRuntimeEffect
    {
        if (player == null)
            return null;

        EquipmentEffectManager manager = player.GetComponent<EquipmentEffectManager>();
        if (manager == null)
            manager = player.gameObject.AddComponent<EquipmentEffectManager>();

        return manager.AddEffect<T>(player, equipment, this);
    }
}

[CreateAssetMenu(menuName = "RuneDice/Equipment/Base Equipment")]
public class BaseEquiment : SerializedScriptableObject, IHeroEquipment
{
    public const int UpgradeRequireCount = 3;

    [Title("Info")]
    public string equipmentId;
    public string equipmentName;
    [TextArea] public string description;
    public Sprite icon;
    public EquipmentType equipmentType;
    public ERarity rarity;

    [Title("Upgrade")]
    public BaseEquiment upgradeResult;

    [Title("Stack")]
    public bool canStack = true;
    [Min(1)] public int maxStack = 99;

    [Title("Stats")]
    public List<EquipmentStatBonus> statBonuses = new();

    [Title("Effects")]
    public List<BaseEquipmentEffect> rarityEffects = new();

    [ShowInInspector, ReadOnly]
    public string StatsPreview => GetStatsPreview();

    public EquipmentType EquipmentType => equipmentType;
    public IReadOnlyList<EquipmentStatBonus> StatBonuses => statBonuses;
    public IReadOnlyList<BaseEquipmentEffect> Effects => rarityEffects;

    public virtual void ApplyEquipEffects(PlayerController player)
    {
        InvokeEffects(player, (effect, owner) => effect.OnEquip(owner, this));
    }

    public virtual void ApplyUnequipEffects(PlayerController player)
    {
        InvokeEffects(player, (effect, owner) => effect.OnUnequip(owner, this));
    }

    public virtual bool TryGetStatBonus(HeroStatType statType, out float value)
    {
        value = 0f;

        if (statBonuses == null)
            return false;

        bool found = false;

        for (int i = 0; i < statBonuses.Count; i++)
        {
            EquipmentStatBonus statBonus = statBonuses[i];
            if (statBonus == null || statBonus.statType != statType)
                continue;

            value += statBonus.amount;
            found = true;
        }

        return found;
    }

    public virtual string GetStatsPreview()
    {
        if (statBonuses == null || statBonuses.Count == 0)
            return "No stats";

        List<string> previews = new List<string>();

        for (int i = 0; i < statBonuses.Count; i++)
        {
            EquipmentStatBonus statBonus = statBonuses[i];
            if (statBonus == null)
                continue;

            previews.Add(statBonus.GetPreviewText());
        }

        return previews.Count > 0
            ? string.Join(", ", previews)
            : "No stats";
    }

    public virtual bool CanUpgradeRarity()
    {
        return upgradeResult != null && rarity != ERarity.Mythical;
    }

    public virtual bool CanMergeForUpgrade(BaseEquiment other)
    {
        if (other == null)
            return false;

        string currentId = !string.IsNullOrEmpty(equipmentId) ? equipmentId : name;
        string otherId = !string.IsNullOrEmpty(other.equipmentId) ? other.equipmentId : other.name;

        return other.equipmentType == equipmentType &&
               other.rarity == rarity &&
               otherId == currentId;
    }

    public virtual bool CanUpgradeFromMaterials(IList<BaseEquiment> materials)
    {
        if (!CanUpgradeRarity() || materials == null || materials.Count < UpgradeRequireCount)
            return false;

        int validCount = 0;

        for (int i = 0; i < materials.Count; i++)
        {
            if (CanMergeForUpgrade(materials[i]))
            {
                validCount++;
            }
        }

        return validCount >= UpgradeRequireCount;
    }

    public virtual bool TryUpgradeRarity(IList<BaseEquiment> materials, out BaseEquiment upgradedEquipment)
    {
        upgradedEquipment = null;

        if (!CanUpgradeFromMaterials(materials))
            return false;

        upgradedEquipment = upgradeResult;
        return upgradedEquipment != null;
    }

    public static ERarity GetNextRarity(ERarity rarity)
    {
        return rarity switch
        {
            ERarity.Common => ERarity.Uncommon,
            ERarity.Uncommon => ERarity.Rare,
            ERarity.Rare => ERarity.Epic,
            ERarity.Epic => ERarity.Legendary,
            ERarity.Legendary => ERarity.Mythical,
            _ => ERarity.Mythical
        };
    }

    void InvokeEffects(PlayerController player, Action<BaseEquipmentEffect, PlayerController> callback)
    {
        if (callback == null || rarityEffects == null)
            return;

        for (int i = 0; i < rarityEffects.Count; i++)
        {
            BaseEquipmentEffect effect = rarityEffects[i];
            if (effect == null)
                continue;

            callback(effect, player);
        }
    }
}
