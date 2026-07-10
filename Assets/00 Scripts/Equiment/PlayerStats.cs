using System.Collections.Generic;
using TigerForge;
using UnityEngine;

public class PlayerStats
{
    public const string HeroBaseKey = "HeroBase";
    public const string EquipmentKey = "Equipment";
    public const string TemporaryLevelKey = "TemporaryLevel";
    public const string TemporaryChapterKey = "TemporaryChapter";

    static PlayerStats sharedInstance;

    public static PlayerStats Shared
    {
        get
        {
            if (sharedInstance == null)
                sharedInstance = new PlayerStats();

            return sharedInstance;
        }
    }

    public Dictionary<HeroStatType, CompositeStats> dicStats =
        new Dictionary<HeroStatType, CompositeStats>();

    bool isInitialized;
    int temporaryLevelStatApplyIndex;
    int temporaryChapterStatApplyIndex;
    int cachedChapterId = -1;
    int cachedLevelIndex = -1;

    public void InitStats()
    {
        if (dicStats == null)
            dicStats = new Dictionary<HeroStatType, CompositeStats>();

        foreach (HeroStatType statType in System.Enum.GetValues(typeof(HeroStatType)))
        {
            if (!dicStats.ContainsKey(statType) || dicStats[statType] == null)
                dicStats[statType] = new CompositeStats();
        }

        if (!isInitialized)
        {
            EventManager.StartListening(Constant.ON_EQUIPMENT_CHANGED, OnEquipmentChange);
            isInitialized = true;
        }

        RefreshTemporaryScopes();
        RebuildFromCurrentSources(ResolveCurrentHeroData());
    }

    public void Dispose()
    {
        if (!isInitialized)
            return;

        EventManager.StopListening(Constant.ON_EQUIPMENT_CHANGED, OnEquipmentChange);
        isInitialized = false;
    }

    void OnEquipmentChange()
    {
        RebuildFromCurrentSources(ResolveCurrentHeroData());
    }

    HeroData ResolveCurrentHeroData()
    {
        if (ChapterDiceSession.Instance != null)
        {
            HeroData chapterHero = ChapterDiceSession.Instance.ResolveHeroData();
            if (chapterHero != null)
                return chapterHero;
        }

        if (HeroSelectionSession.Instance != null)
            return HeroSelectionSession.Instance.GetSelectedHero();

        if (EquipmentManager.Instance != null)
            return EquipmentManager.Instance.GetHeroData();

        return null;
    }

    public void RebuildFromCurrentSources(HeroData heroData = null)
    {
        RefreshTemporaryScopes();

        ClearStats(HeroBaseKey);
        ClearStats(EquipmentKey);

        HeroData resolvedHeroData = heroData != null ? heroData : ResolveCurrentHeroData();
        Debug.Log($"[PlayerStats] RebuildFromCurrentSources hero={(resolvedHeroData != null ? resolvedHeroData.name : "null")} equipmentManager={(EquipmentManager.Instance != null)}");

        if (resolvedHeroData != null)
            ApplyHeroBaseStats(resolvedHeroData);

        if (EquipmentManager.Instance != null)
            ApplyEquipmentStats(EquipmentManager.Instance.GetAllEquippedItems());
        else
            ApplyEquipmentStats(EquipmentSession.GetOrCreate().GetAllEquipped());
    }

    void RefreshTemporaryScopes()
    {
        if (ChapterManager.Instance == null)
        {
            Debug.Log("[PlayerStats] RefreshTemporaryScopes skipped: ChapterManager.Instance is null");
            return;
        }

        int currentChapterId = ChapterManager.Instance.CurrentChapterId;
        int currentLevelIndex = ChapterManager.Instance.CurrentLevelIndex;

        Debug.Log($"[PlayerStats] RefreshTemporaryScopes currentChapter={currentChapterId} currentLevel={currentLevelIndex} cachedChapter={cachedChapterId} cachedLevel={cachedLevelIndex}");

        if (cachedChapterId != currentChapterId)
        {
            Debug.Log($"[PlayerStats] Chapter changed => clear temp chapter + temp level | from chapter {cachedChapterId} to {currentChapterId}");
            ClearTemporaryChapterStats();
            ClearTemporaryLevelStats();
            cachedChapterId = currentChapterId;
            cachedLevelIndex = currentLevelIndex;
            return;
        }

        if (cachedLevelIndex != currentLevelIndex)
        {
            Debug.Log($"[PlayerStats] Level changed => clear temp level | from level {cachedLevelIndex} to {currentLevelIndex}");
            ClearTemporaryLevelStats();
            cachedLevelIndex = currentLevelIndex;
        }
    }

    public void ApplyEquipmentStats(List<BaseEquiment> equippedItems)
    {
        if (equippedItems == null)
            return;

        for (int i = 0; i < equippedItems.Count; i++)
        {
            BaseEquiment equipment = equippedItems[i];
            if (equipment == null || equipment.statBonuses == null)
                continue;

            string keyLocal = !string.IsNullOrEmpty(equipment.equipmentId)
                ? equipment.equipmentId
                : equipment.name;

            for (int j = 0; j < equipment.statBonuses.Count; j++)
            {
                EquipmentStatBonus bonus = equipment.statBonuses[j];
                if (bonus == null)
                    continue;

                bool isFlatValue = bonus.modifierType == EquipmentStatModifierType.Flat;

                if (!dicStats.ContainsKey(bonus.statType))
                    continue;

                dicStats[bonus.statType].ApplyStats(
                    bonus.amount,
                    EquipmentKey,
                    keyLocal,
                    isFlatValue
                );
            }
        }
    }

    public void ApplyHeroBaseStats(HeroData heroData)
    {
        if (heroData == null)
            return;

        ApplyStats(HeroStatType.Hp, heroData.hp, HeroBaseKey, "Hp", true);
        ApplyStats(HeroStatType.Damage, heroData.damage, HeroBaseKey, "Damage", true);
        ApplyStats(HeroStatType.Defense, heroData.def, HeroBaseKey, "Defense", true);
        ApplyStats(HeroStatType.CritDamage, heroData.critDmg, HeroBaseKey, "CritDamage", true);
        ApplyStats(HeroStatType.CritRate, heroData.critRate, HeroBaseKey, "CritRate", true);
        ApplyStats(HeroStatType.Luck, heroData.luck, HeroBaseKey, "Luck", true);
    }

    public float GetStatValue(HeroStatType statType)
    {
        if (!dicStats.ContainsKey(statType))
            return 0f;

        return dicStats[statType].Value;
    }

    public void ApplyStats(HeroStatType statType, float value, string keyGlobal, string keyLocal, bool isFlatValue)
    {
        if (!dicStats.ContainsKey(statType) || dicStats[statType] == null)
            dicStats[statType] = new CompositeStats();

        dicStats[statType].ApplyStats(value, keyGlobal, keyLocal, isFlatValue);
    }

    public void ClearStats(string keyGlobal)
    {
        foreach (var item in dicStats)
        {
            item.Value.ClearStats(keyGlobal);
        }
    }

    public void ClearStats(string keyGlobal, string keyLocal)
    {
        foreach (var item in dicStats)
        {
            item.Value.ClearStats(keyGlobal, keyLocal);
        }
    }

    public void ClearTemporaryLevelStats()
    {
        Debug.Log("[PlayerStats] ClearTemporaryLevelStats");
        ClearStats(TemporaryLevelKey);
        temporaryLevelStatApplyIndex = 0;
    }

    public void ClearTemporaryChapterStats()
    {
        Debug.Log("[PlayerStats] ClearTemporaryChapterStats");
        ClearStats(TemporaryChapterKey);
        temporaryChapterStatApplyIndex = 0;
    }

    public void ClearTemporaryStats()
    {
        ClearTemporaryLevelStats();
        ClearTemporaryChapterStats();
    }

    public void ApplyTemporaryLevelStat(HeroStatType statType, float value, string keyLocal, bool isFlatValue)
    {
        RefreshTemporaryScopes();
        Debug.Log($"[PlayerStats] ApplyTemporaryLevelStat stat={statType} value={value} key={keyLocal} isFlat={isFlatValue}");
        ApplyStats(statType, value, TemporaryLevelKey, keyLocal, isFlatValue);
        RebuildFromCurrentSources(ResolveCurrentHeroData());
        Debug.Log($"[PlayerStats] After ApplyTemporaryLevelStat => {statType}={GetStatValue(statType)}");
        EventManager.EmitEvent(Constant.ON_PLAYER_STATS_CHANGED);
    }

    public void ApplyTemporaryChapterStat(HeroStatType statType, float value, string keyLocal, bool isFlatValue)
    {
        RefreshTemporaryScopes();
        Debug.Log($"[PlayerStats] ApplyTemporaryChapterStat stat={statType} value={value} key={keyLocal} isFlat={isFlatValue}");
        ApplyStats(statType, value, TemporaryChapterKey, keyLocal, isFlatValue);
        RebuildFromCurrentSources(ResolveCurrentHeroData());
        Debug.Log($"[PlayerStats] After ApplyTemporaryChapterStat => {statType}={GetStatValue(statType)}");
        EventManager.EmitEvent(Constant.ON_PLAYER_STATS_CHANGED);
    }

    public void AddTemporaryLevelStat(HeroStatType statType, float value, string keyLocal, bool isFlatValue)
    {
        temporaryLevelStatApplyIndex++;
        ApplyTemporaryLevelStat(
            statType,
            value,
            keyLocal + "_" + temporaryLevelStatApplyIndex,
            isFlatValue
        );
    }

    public void AddTemporaryChapterStat(HeroStatType statType, float value, string keyLocal, bool isFlatValue)
    {
        temporaryChapterStatApplyIndex++;
        ApplyTemporaryChapterStat(
            statType,
            value,
            keyLocal + "_" + temporaryChapterStatApplyIndex,
            isFlatValue
        );
    }

    public HeroStatSnapshot ToHeroStatSnapshot(HeroData heroData = null)
    {
        RefreshTemporaryScopes();

        HeroStatSnapshot snapshot = new HeroStatSnapshot(heroData);

        snapshot.hp = Mathf.RoundToInt(GetStatValue(HeroStatType.Hp));
        snapshot.damage = Mathf.RoundToInt(GetStatValue(HeroStatType.Damage));
        snapshot.defense = Mathf.RoundToInt(GetStatValue(HeroStatType.Defense));
        snapshot.critDamage = GetStatValue(HeroStatType.CritDamage);
        snapshot.critRate = GetStatValue(HeroStatType.CritRate);
        snapshot.luck = GetStatValue(HeroStatType.Luck);

        return snapshot;
    }
}
