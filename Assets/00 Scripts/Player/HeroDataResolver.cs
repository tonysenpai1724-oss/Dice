using UnityEngine;

public static class HeroDataResolver
{
    static HeroDatabaseSO heroDatabase;

    public static void SetDatabase(HeroDatabaseSO database)
    {
        heroDatabase = database;
    }

    public static HeroData Resolve(HeroData fallbackHeroData = null, bool includeEquipmentManager = true, HeroDatabaseSO sourceDatabase = null)
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

            HeroData databaseHeroData = ResolveFromDatabase(chapterDiceSession.CurrentHeroName, sourceDatabase != null ? sourceDatabase : chapterDiceSession.heroDatabase);
            if (databaseHeroData != null)
                return databaseHeroData;
        }

        HeroSelectionSession heroSelectionSession = HeroSelectionSession.Instance;
        if (heroSelectionSession != null && heroSelectionSession.HasSelectedHero())
            return heroSelectionSession.GetSelectedHero();

        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        if (player != null && player.data != null)
            return player.data;

        return ResolveFromDatabase(null, sourceDatabase);
    }

    public static HeroData ResolveByType(HeroType type, HeroDatabaseSO sourceDatabase = null)
    {
        HeroDatabaseSO database = GetDatabase(sourceDatabase);
        return database != null ? database.GetHero(type) : null;
    }

    public static HeroData ResolveByName(string heroName, HeroDatabaseSO sourceDatabase = null)
    {
        return ResolveFromDatabase(heroName, sourceDatabase);
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

    static HeroData ResolveFromDatabase(string heroName = null, HeroDatabaseSO sourceDatabase = null)
    {
        HeroDatabaseSO database = GetDatabase(sourceDatabase);
        if (database == null)
            return null;

        HeroData heroData = database.GetHeroByName(heroName);
        return heroData != null ? heroData : database.GetDefaultHero();
    }

    static HeroDatabaseSO GetDatabase(HeroDatabaseSO sourceDatabase = null)
    {
        if (sourceDatabase != null)
            return sourceDatabase;

        if (heroDatabase != null)
            return heroDatabase;

        EquipmentManager equipmentManager = EquipmentManager.Instance;
        if (equipmentManager != null && equipmentManager.heroDatabase != null)
            return equipmentManager.heroDatabase;

        return heroDatabase;
    }
}
