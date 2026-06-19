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
        PlayerStats previewStats = BuildPreviewStats();
        HeroStatSnapshot stats = previewStats.ToHeroStatSnapshot(heroData);

        SetText(txtHp, "HP:" + stats.hp);
        SetText(txtDamage, "Dmg:" + stats.damage);
        SetText(txtDefense, "Defense:" + stats.defense);
        SetText(txtCritRate, "CritRate:" + $"{stats.critRate:0.##}");
        SetText(txtCritDamage, "CritDmg:" + $"{stats.critDamage:0.##}");
        SetText(txtLuck, "Luck:" + $"{stats.luck:0.##}");
    }

    PlayerStats BuildPreviewStats()
    {
        PlayerStats stats = new PlayerStats();
        stats.InitStats();
        stats.ClearStats(PlayerStats.HeroBaseKey);
        stats.ClearStats(PlayerStats.EquipmentKey);
        stats.ApplyHeroBaseStats(heroData);

        EquipmentSession session = EquipmentSession.GetOrCreate();
        if (session == null)
            return stats;

        if (equipmentDatabase != null)
            session.SetDatabase(equipmentDatabase);

        ApplyEquipment(stats, session.GetEquipped(EquipmentType.Weapon));
        ApplyEquipment(stats, session.GetEquipped(EquipmentType.Helmet));
        ApplyEquipment(stats, session.GetEquipped(EquipmentType.Armor));
        ApplyEquipment(stats, session.GetEquipped(EquipmentType.Gloves));
        ApplyEquipment(stats, session.GetEquipped(EquipmentType.Boots));
        ApplyEquipment(stats, session.GetEquipped(EquipmentType.Ring));
        ApplyEquipment(stats, session.GetEquipped(EquipmentType.Necklace));
        ApplyEquipment(stats, session.GetEquipped(EquipmentType.Artifact));

        return stats;
    }

    void ApplyEquipment(PlayerStats stats, BaseEquiment equipment)
    {
        if (stats == null || equipment == null || equipment.statBonuses == null)
            return;

        string keyLocal = !string.IsNullOrEmpty(equipment.equipmentId)
            ? equipment.equipmentId
            : equipment.name;

        for (int i = 0; i < equipment.statBonuses.Count; i++)
        {
            EquipmentStatBonus bonus = equipment.statBonuses[i];
            if (bonus == null)
                continue;

            stats.ApplyStats(
                bonus.statType,
                bonus.amount,
                PlayerStats.EquipmentKey,
                keyLocal,
                bonus.modifierType == EquipmentStatModifierType.Flat);
        }
    }

    void SetText(TextMeshProUGUI label, string value)
    {
        if (label != null)
            label.text = value;
    }
}
