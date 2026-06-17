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

        SetText(txtHp, "HP:" + stats.hp.ToString());
        SetText(txtDamage, "Dmg:" + stats.damage.ToString());
        SetText(txtDefense, "Defense:" + stats.defense.ToString());
        SetText(txtCritRate, "CritRate:" + $"{stats.critRate:0.##}");
        SetText(txtCritDamage, "CritDmg:" + $"{stats.critDamage:0.##}");
        SetText(txtLuck, "Luck:" + $"{stats.luck:0.##}");
    }

    HeroStatSnapshot BuildStats()
    {
        HeroStatSnapshot stats = new HeroStatSnapshot(heroData);
        EquipmentSession session = EquipmentSession.GetOrCreate();

        if (session == null)
            return stats;

        if (equipmentDatabase != null)
            session.SetDatabase(equipmentDatabase);

        AddEquipmentStats(stats, session.GetEquipped(EquipmentType.Weapon));
        AddEquipmentStats(stats, session.GetEquipped(EquipmentType.Helmet));
        AddEquipmentStats(stats, session.GetEquipped(EquipmentType.Armor));
        AddEquipmentStats(stats, session.GetEquipped(EquipmentType.Gloves));
        AddEquipmentStats(stats, session.GetEquipped(EquipmentType.Boots));
        AddEquipmentStats(stats, session.GetEquipped(EquipmentType.Ring));
        AddEquipmentStats(stats, session.GetEquipped(EquipmentType.Necklace));
        AddEquipmentStats(stats, session.GetEquipped(EquipmentType.Artifact));

        return stats;
    }

    void AddEquipmentStats(HeroStatSnapshot targetStats, BaseEquiment equipment)
    {
        if (targetStats == null || equipment == null || equipment.statBonuses == null)
            return;

        for (int i = 0; i < equipment.statBonuses.Count; i++)
        {
            EquipmentStatBonus bonus = equipment.statBonuses[i];
            if (bonus == null)
                continue;

            float value = bonus.modifierType == EquipmentStatModifierType.Percent
                ? targetStats.GetValue(bonus.statType) * bonus.amount * 0.01f
                : bonus.amount;

            targetStats.Add(bonus.statType, value);
        }
    }

    void SetText(TextMeshProUGUI label, string value)
    {
        if (label != null)
            label.text = value;
    }
}