using System.Collections.Generic;
using TMPro;
using TigerForge;
using UnityEngine;

public class PlayerInventoryUI : MonoBehaviour
{
    [Header("Refs")]
    public EquipmentDatabaseSO equipmentDatabase;

    [Header("Texts")]
    public TextMeshProUGUI txtHp;
    public TextMeshProUGUI txtDamage;
    public TextMeshProUGUI txtDefense;
    public TextMeshProUGUI txtCritRate;
    public TextMeshProUGUI txtCritDamage;
    public TextMeshProUGUI txtLuck;

    [Header("Base Hero Stats")]
    public HeroData heroData;

    void Start()
    {
        Refresh();
    }

    void OnEnable()
    {
        EventManager.StartListening(Constant.ON_EQUIPMENT_CHANGED, Refresh);
        EventManager.StartListening(Constant.ON_EQUIPMENT_SESSION_CHANGED, Refresh);
        Refresh();
    }

    void OnDisable()
    {
        EventManager.StopListening(Constant.ON_EQUIPMENT_CHANGED, Refresh);
        EventManager.StopListening(Constant.ON_EQUIPMENT_SESSION_CHANGED, Refresh);
    }

    public void Refresh()
    {
        HeroStatSnapshot stats = BuildStats();

        SetText(txtHp, "HP:" + stats.hp);
        SetText(txtDamage, "Dmg:" + stats.damage);
        SetText(txtDefense, "Defense:" + stats.defense);
        SetText(txtCritRate, "CritRate:" + $"{stats.critRate:0.##}");
        SetText(txtCritDamage, "CritDmg:" + $"{stats.critDamage:0.##}");
        SetText(txtLuck, "Luck:" + $"{stats.luck:0.##}");
    }

    HeroStatSnapshot BuildStats()
    {
        HeroStatSnapshot baseStats = new HeroStatSnapshot(heroData);
        HeroStatSnapshot finalStats = Clone(baseStats);
        List<HeroStatModifier> modifiers = BuildModifiers();

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

        return finalStats;
    }

    List<HeroStatModifier> BuildModifiers()
    {
        List<HeroStatModifier> modifiers = new List<HeroStatModifier>();
        EquipmentSession session = EquipmentSession.GetOrCreate();

        if (session == null)
            return modifiers;

        if (equipmentDatabase != null)
            session.SetDatabase(equipmentDatabase);

        AddEquipmentModifiers(modifiers, session.GetEquipped(EquipmentType.Weapon));
        AddEquipmentModifiers(modifiers, session.GetEquipped(EquipmentType.Helmet));
        AddEquipmentModifiers(modifiers, session.GetEquipped(EquipmentType.Armor));
        AddEquipmentModifiers(modifiers, session.GetEquipped(EquipmentType.Gloves));
        AddEquipmentModifiers(modifiers, session.GetEquipped(EquipmentType.Boots));
        AddEquipmentModifiers(modifiers, session.GetEquipped(EquipmentType.Ring));
        AddEquipmentModifiers(modifiers, session.GetEquipped(EquipmentType.Necklace));
        AddEquipmentModifiers(modifiers, session.GetEquipped(EquipmentType.Artifact));

        return modifiers;
    }

    void AddEquipmentModifiers(List<HeroStatModifier> modifiers, BaseEquiment equipment)
    {
        if (modifiers == null || equipment == null)
            return;

        equipment.CollectModifiers(null, modifiers);
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

    void SetText(TextMeshProUGUI label, string value)
    {
        if (label != null)
            label.text = value;
    }
}