using UnityEngine;

public static class HeroDataResolver
{
    public static HeroData Resolve(HeroData fallbackHeroData = null, bool includeEquipmentManager = true)
    {
        if (fallbackHeroData != null)
            return fallbackHeroData;

        if (includeEquipmentManager)
        {
            HeroData equipmentHeroData = ResolveFromEquipmentManager();
            if (equipmentHeroData != null)
                return equipmentHeroData;
        }

        ChapterDiceSession chapterDiceSession = ChapterDiceSession.Instance;
        if (chapterDiceSession != null)
        {
            HeroData chapterHeroData = chapterDiceSession.ResolveHeroData();
            if (chapterHeroData != null)
                return chapterHeroData;
        }

        HeroSelectionSession heroSelectionSession = HeroSelectionSession.Instance;
        if (heroSelectionSession != null && heroSelectionSession.HasSelectedHero())
            return heroSelectionSession.GetSelectedHero();

        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        if (player != null && player.data != null)
            return player.data;

        return null;
    }

    static HeroData ResolveFromEquipmentManager()
    {
        EquipmentManager equipmentManager = EquipmentManager.Instance;
        if (equipmentManager == null)
            return null;

        if (equipmentManager.heroDataOverride != null)
            return equipmentManager.heroDataOverride;

        if (equipmentManager.player != null && equipmentManager.player.data != null)
            return equipmentManager.player.data;

        return null;
    }
}
