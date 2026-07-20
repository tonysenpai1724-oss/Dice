using TMPro;
using TigerForge;
using UnityEngine;

public class PlayerInventoryUI : MonoBehaviour
{
    [Header("Refs")]
    public EquipmentDatabaseSO equipmentDatabase;
    public HeroDatabaseSO heroDatabase;

    [Header("Texts")]
    public TextMeshProUGUI txtHp;
    public TextMeshProUGUI txtDamage;
    public TextMeshProUGUI txtDefense;
    public TextMeshProUGUI txtCritRate;
    public TextMeshProUGUI txtCritDamage;
    public TextMeshProUGUI txtLuck;
    public TextMeshProUGUI txtName;

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
        EventManager.StartListening(Constant.ON_PLAYER_STATS_CHANGED, Refresh);
        Refresh();
    }

    void OnDisable()
    {
        EventManager.StopListening(Constant.ON_EQUIPMENT_CHANGED, Refresh);
        EventManager.StopListening(Constant.ON_EQUIPMENT_SESSION_CHANGED, Refresh);
        EventManager.StopListening(Constant.ON_PLAYER_STATS_CHANGED, Refresh);
    }

    public void Refresh()
    {
        HeroData currentHeroData = ResolveHeroData();
        HeroStatSnapshot stats = GetDisplayedStats(currentHeroData);

        SetText(txtHp, GetDisplayCurrentHp(stats.hp) + "/" + stats.hp);
        SetText(txtDamage, stats.damage.ToString());
        SetText(txtDefense, stats.defense.ToString());
        SetText(txtCritRate, $"{stats.critRate:0.##}");
        SetText(txtCritDamage, $"{stats.critDamage:0.##}");
        SetText(txtLuck, $"{stats.luck:0.##}");
        SetText(txtName, currentHeroData != null ? currentHeroData.name : string.Empty);
    }

    HeroStatSnapshot GetDisplayedStats(HeroData currentHeroData)
    {
        PlayerStats previewStats = BuildPreviewStats(currentHeroData);
        return previewStats.ToHeroStatSnapshot(currentHeroData);
    }

    PlayerStats BuildPreviewStats(HeroData currentHeroData)
    {
        PlayerStats stats = new PlayerStats();
        stats.InitStats();
        stats.ClearStats(PlayerStats.HeroBaseKey);
        stats.ClearStats(PlayerStats.EquipmentKey);
        stats.ClearTemporaryStats();
        stats.ApplyHeroBaseStats(currentHeroData);

        EquipmentSession session = EquipmentSession.GetOrCreate();
        if (session != null)
        {
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
        }

        ApplyTemporaryStats(stats);
        return stats;
    }

    HeroData ResolveHeroData()
    {
        return HeroDataResolver.Resolve(heroData, true, heroDatabase);
    }

    int GetDisplayCurrentHp(int maxHp)
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
            return Mathf.Clamp(player.currentHp, 0, maxHp);

        ChapterDiceSession chapterDiceSession = ChapterDiceSession.Instance;
        if (chapterDiceSession != null && chapterDiceSession.TryGetCurrentHp(out int savedCurrentHp))
            return Mathf.Clamp(savedCurrentHp, 0, maxHp);

        return maxHp;
    }

    void ApplyTemporaryStats(PlayerStats stats)
    {
        if (stats == null || PlayerStats.Shared == null)
            return;

        ApplyTemporaryStat(stats, HeroStatType.Hp, PlayerStats.TemporaryLevelKey);
        ApplyTemporaryStat(stats, HeroStatType.Damage, PlayerStats.TemporaryLevelKey);
        ApplyTemporaryStat(stats, HeroStatType.Defense, PlayerStats.TemporaryLevelKey);
        ApplyTemporaryStat(stats, HeroStatType.CritDamage, PlayerStats.TemporaryLevelKey);
        ApplyTemporaryStat(stats, HeroStatType.CritRate, PlayerStats.TemporaryLevelKey);
        ApplyTemporaryStat(stats, HeroStatType.Luck, PlayerStats.TemporaryLevelKey);

        ApplyTemporaryStat(stats, HeroStatType.Hp, PlayerStats.TemporaryChapterKey);
        ApplyTemporaryStat(stats, HeroStatType.Damage, PlayerStats.TemporaryChapterKey);
        ApplyTemporaryStat(stats, HeroStatType.Defense, PlayerStats.TemporaryChapterKey);
        ApplyTemporaryStat(stats, HeroStatType.CritDamage, PlayerStats.TemporaryChapterKey);
        ApplyTemporaryStat(stats, HeroStatType.CritRate, PlayerStats.TemporaryChapterKey);
        ApplyTemporaryStat(stats, HeroStatType.Luck, PlayerStats.TemporaryChapterKey);
    }

    void ApplyTemporaryStat(PlayerStats previewStats, HeroStatType statType, string keyGlobal)
    {
        CompositeStats sharedStat = null;
        if (!PlayerStats.Shared.dicStats.TryGetValue(statType, out sharedStat) || sharedStat == null)
            return;

        float currentValue = sharedStat.GetValue(keyGlobal);
        float currentPercent = sharedStat.GetPercent(keyGlobal);

        if (!Mathf.Approximately(currentValue, 0f))
            previewStats.ApplyStats(statType, currentValue, keyGlobal, statType.ToString() + "_" + keyGlobal + "_Flat", true);

        if (!Mathf.Approximately(currentPercent, 0f))
            previewStats.ApplyStats(statType, currentPercent, keyGlobal, statType.ToString() + "_" + keyGlobal + "_Percent", false);
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
                bonus.modifierType == EquipmentStatModifierType.Flat
            );
        }
    }

    void SetText(TextMeshProUGUI textComponent, string value)
    {
        if (textComponent != null)
            textComponent.text = value;
    }
}
