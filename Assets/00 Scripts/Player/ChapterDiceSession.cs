using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable]
public class ChapterDiceSessionSaveData
{
    public string heroName;
    public int heroLevel;
    public bool heroStartDiceAdded;
    public List<ChapterDiceSessionDiceSaveData> runtimeDices = new();
}

[Serializable]
public class ChapterDiceSessionDiceSaveData
{
    public string diceName;
    public int level;
    public DiceType type;
}

public class ChapterDiceSession : MonoBehaviour
{
    const string SaveKey = "chapter_dice_session";

    public static ChapterDiceSession Instance;

    [SerializeField] HeroData heroData;
    [SerializeField] int heroLevel;
    [SerializeField] bool initializedFromHero;
    [SerializeField] bool heroStartDiceAdded;
    [SerializeField] List<DiceData> runtimeDiceDatas = new();
    public DiceDatabaseSO diceDatabase;

    public IReadOnlyList<DiceData> RuntimeDiceDatas => runtimeDiceDatas;
    public HeroData CurrentHeroData => heroData;

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadSession();
    }

    public static ChapterDiceSession GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        GameObject sessionObject = new GameObject("ChapterDiceSession");
        return sessionObject.AddComponent<ChapterDiceSession>();
    }

    public void SetSelectedHero(HeroData sourceHeroData)
    {
        if (sourceHeroData == null)
            return;

        heroData = sourceHeroData;
        heroLevel = sourceHeroData.level;
        HeroSelectionSession.GetOrCreate().SetSelectedHero(sourceHeroData);
    }

    public HeroData ResolveHeroData(HeroData fallbackHeroData = null)
    {
        if (heroData != null)
            return heroData;

        HeroSelectionSession heroSession = HeroSelectionSession.GetOrCreate();
        if (heroSession != null && heroSession.HasSelectedHero())
            return heroSession.GetSelectedHero();

        return fallbackHeroData;
    }

    public void StartRunFromHero(HeroData sourceHeroData)
    {
        sourceHeroData = ResolveHeroData(sourceHeroData);
        if (sourceHeroData == null)
            return;

        SetSelectedHero(sourceHeroData);
        EnsureRuntimeDiceInitialized();
        SaveSession();
    }

    public void InitializeFromHero(HeroData sourceHeroData)
    {
        sourceHeroData = ResolveHeroData(sourceHeroData);
        if (sourceHeroData == null)
            return;

        SetSelectedHero(sourceHeroData);
        EnsureRuntimeDiceInitialized();
        SaveSession();
    }

    void EnsureRuntimeDiceInitialized()
    {
        if (TryRestoreRuntimeDiceFromSave())
            return;

        SeedHeroDiceIfNeeded(forceSeed: !heroStartDiceAdded);
    }

    bool TryRestoreRuntimeDiceFromSave()
    {
        if (runtimeDiceDatas.Count > 0)
            return true;

        string json = CPlayerPrefs.GetString(SaveKey);
        if (string.IsNullOrEmpty(json))
            return false;

        ChapterDiceSessionSaveData saveData = JsonUtility.FromJson<ChapterDiceSessionSaveData>(json);
        if (saveData == null)
            return false;

        heroLevel = saveData.heroLevel;
        heroStartDiceAdded = saveData.heroStartDiceAdded;

        bool restored = RestoreRuntimeDiceFromSaveData(saveData);
        if (restored)
        {
            DebugLogRuntimeDice("TryRestoreRuntimeDiceFromSave restored");
            return true;
        }

        Debug.Log($"[ChapterDiceSession] TryRestoreRuntimeDiceFromSave failed to rebuild runtime list. heroStartDiceAdded={heroStartDiceAdded} heroLevel={heroLevel}");
        return false;
    }

    void SeedHeroDiceIfNeeded(bool forceSeed = false)
    {
        if (heroData == null)
            return;

        if (!forceSeed && heroStartDiceAdded)
            return;

        if (runtimeDiceDatas.Count > 0)
        {
            initializedFromHero = true;
            return;
        }

        if (!forceSeed && initializedFromHero)
            return;

        if (heroData.startDiceLevelConfig == null)
            return;

        if (!heroData.startDiceLevelConfig.TryGetValue(heroLevel, out List<DiceData> startDices))
            return;

        if (startDices == null)
            return;

        for (int i = 0; i < startDices.Count; i++)
        {
            DiceData data = startDices[i];
            if (data == null)
                continue;

            runtimeDiceDatas.Add(data);
        }

        initializedFromHero = runtimeDiceDatas.Count > 0;
        heroStartDiceAdded = runtimeDiceDatas.Count > 0;
        DebugLogRuntimeDice(forceSeed ? "SeedHeroDiceIfNeeded force-seed" : "SeedHeroDiceIfNeeded");
    }

    DiceData FindDiceData(DiceDatabaseSO diceDatabase, ChapterDiceSessionDiceSaveData savedDice)
    {
        if (diceDatabase == null || savedDice == null)
            return null;

        List<DiceData> candidates = diceDatabase.GetAllByType(savedDice.type);
        for (int i = 0; i < candidates.Count; i++)
        {
            DiceData data = candidates[i];
            if (data == null)
                continue;

            if (data.level == savedDice.level && data.diceName == savedDice.diceName)
                return data;
        }

        return diceDatabase.GetDiceData(savedDice.level, savedDice.type);
    }

    void SaveSession()
    {

        ChapterDiceSessionSaveData saveData = new ChapterDiceSessionSaveData
        {
            heroName = heroData != null ? heroData.name : string.Empty,
            heroLevel = heroLevel,
            heroStartDiceAdded = heroStartDiceAdded,
            runtimeDices = new List<ChapterDiceSessionDiceSaveData>()
        };

        for (int i = 0; i < runtimeDiceDatas.Count; i++)
        {
            DiceData data = runtimeDiceDatas[i];
            if (data == null)
                continue;

            saveData.runtimeDices.Add(new ChapterDiceSessionDiceSaveData
            {
                diceName = data.diceName,
                level = data.level,
                type = data.type
            });
        }

        string json = JsonUtility.ToJson(saveData);
        CPlayerPrefs.SetString(SaveKey, json);
        DebugLogRuntimeDice("SaveSession");
        Debug.Log($"[ChapterDiceSession] Saved json count={saveData.runtimeDices.Count} json={json}");
    }

    void LoadSession()
    {
        string json = CPlayerPrefs.GetString(SaveKey);
        if (string.IsNullOrEmpty(json))
            return;

        ChapterDiceSessionSaveData saveData = JsonUtility.FromJson<ChapterDiceSessionSaveData>(json);
        if (saveData == null)
            return;

        heroLevel = saveData.heroLevel;
        heroStartDiceAdded = saveData.heroStartDiceAdded;

        bool restored = RestoreRuntimeDiceFromSaveData(saveData);
        if (!restored)
            Debug.Log($"[ChapterDiceSession] LoadSession restore returned empty. heroStartDiceAdded={heroStartDiceAdded} heroLevel={heroLevel} json={json}");

        DebugLogRuntimeDice("LoadSession");
    }

    bool RestoreRuntimeDiceFromSaveData(ChapterDiceSessionSaveData saveData)
    {
        runtimeDiceDatas.Clear();

        if (saveData == null || saveData.runtimeDices == null || saveData.runtimeDices.Count == 0)
        {
            initializedFromHero = false;
            return false;
        }
        if (diceDatabase == null)
            return false;

        for (int i = 0; i < saveData.runtimeDices.Count; i++)
        {
            ChapterDiceSessionDiceSaveData savedDice = saveData.runtimeDices[i];
            DiceData data = FindDiceData(diceDatabase, savedDice);
            if (data != null)
                runtimeDiceDatas.Add(data);
        }

        initializedFromHero = runtimeDiceDatas.Count > 0;
        heroStartDiceAdded = saveData.heroStartDiceAdded || runtimeDiceDatas.Count > 0;
        return runtimeDiceDatas.Count > 0;
    }

    public List<DiceData> GetRuntimeDiceDatasCopy()
    {
        return new List<DiceData>(runtimeDiceDatas);
    }

    public void AddDiceData(DiceData diceData)
    {
        if (diceData == null)
            return;

        runtimeDiceDatas.Add(diceData);
        initializedFromHero = true;
        DebugLogRuntimeDice($"AddDiceData before save added={diceData.diceName}");
        TigerForge.EventManager.EmitEvent(Constant.ON_EQUIMENT_DICE_CHANGED);
        SaveSession();
    }

    public void AddDiceDatas(List<DiceData> diceDatas)
    {
        if (diceDatas == null)
            return;

        for (int i = 0; i < diceDatas.Count; i++)
        {
            AddDiceData(diceDatas[i]);
        }
    }

    public bool CanCloneAnyDice()
    {
        for (int i = 0; i < runtimeDiceDatas.Count; i++)
        {
            if (runtimeDiceDatas[i] != null)
                return true;
        }

        return false;
    }

    public List<DiceData> GetCloneableDiceOptions()
    {
        List<DiceData> result = new List<DiceData>();

        for (int i = 0; i < runtimeDiceDatas.Count; i++)
        {
            DiceData current = runtimeDiceDatas[i];
            if (current == null)
                continue;

            result.Add(current);
        }

        return result;
    }

    public bool CloneDiceData(DiceData sourceDiceData)
    {
        if (sourceDiceData == null)
            return false;

        for (int i = 0; i < runtimeDiceDatas.Count; i++)
        {
            if (runtimeDiceDatas[i] != sourceDiceData)
                continue;

            runtimeDiceDatas.Insert(i + 1, sourceDiceData);
            initializedFromHero = true;
            DebugLogRuntimeDice($"CloneDiceData before save cloned={sourceDiceData.diceName}");
            SaveSession();
            return true;
        }

        return false;
    }

    public int GetTotalDiceLevelByType(DiceType type)
    {
        int totalLevel = 0;

        for (int i = 0; i < runtimeDiceDatas.Count; i++)
        {
            DiceData current = runtimeDiceDatas[i];
            if (current == null || current.type != type)
                continue;

            totalLevel += Mathf.Max(0, current.level);
        }

        return totalLevel;
    }

    public DiceData GetSummedUpgradeTarget(DiceData sourceDiceData, DiceDatabaseSO sourceDiceDatabase = null)
    {
        if (sourceDiceData == null)
            return null;

        DiceDatabaseSO database = sourceDiceDatabase != null ? sourceDiceDatabase : diceDatabase;
        if (database == null)
            return null;

        int totalLevel = GetTotalDiceLevelByType(sourceDiceData.type);
        if (totalLevel <= sourceDiceData.level)
            return null;

        return database.GetDiceData(totalLevel, sourceDiceData.type);
    }

    public bool UpgradeDiceDataByTypeSum(DiceData sourceDiceData, out DiceData upgradedDiceData, DiceDatabaseSO sourceDiceDatabase = null)
    {
        upgradedDiceData = GetSummedUpgradeTarget(sourceDiceData, sourceDiceDatabase);
        if (sourceDiceData == null || upgradedDiceData == null)
            return false;

        for (int i = 0; i < runtimeDiceDatas.Count; i++)
        {
            if (runtimeDiceDatas[i] != sourceDiceData)
                continue;

            runtimeDiceDatas[i] = upgradedDiceData;
            DebugLogRuntimeDice($"UpgradeDiceDataByTypeSum before save source={sourceDiceData.diceName} target={upgradedDiceData.diceName}");
            SaveSession();
            return true;
        }

        return false;
    }

    public bool DowngradeDiceData(DiceData currentDiceData, out DiceData downgradedDiceData, DiceDatabaseSO sourceDiceDatabase = null)
    {
        downgradedDiceData = null;

        if (currentDiceData == null)
            return false;

        int targetLevel = Mathf.Max(1, currentDiceData.level - 1);
        if (targetLevel == currentDiceData.level)
            return false;

        DiceDatabaseSO database = sourceDiceDatabase != null ? sourceDiceDatabase : diceDatabase;
        if (database == null)
            return false;

        downgradedDiceData = database.GetDiceData(targetLevel, currentDiceData.type);
        if (downgradedDiceData == null)
            return false;

        for (int i = 0; i < runtimeDiceDatas.Count; i++)
        {
            if (runtimeDiceDatas[i] != currentDiceData)
                continue;

            runtimeDiceDatas[i] = downgradedDiceData;
            DebugLogRuntimeDice($"DowngradeDiceData before save source={currentDiceData.diceName} target={downgradedDiceData.diceName}");
            SaveSession();
            return true;
        }

        return false;
    }

    public bool UpgradeDiceData(DiceData currentDiceData, DiceData upgradedDiceData)
    {
        if (currentDiceData == null || upgradedDiceData == null)
            return false;

        for (int i = 0; i < runtimeDiceDatas.Count; i++)
        {
            if (runtimeDiceDatas[i] != currentDiceData)
                continue;

            runtimeDiceDatas[i] = upgradedDiceData;
            SaveSession();
            return true;
        }

        return false;
    }

    public List<DiceData> GetUpgradeableDiceOptions(DiceDatabaseSO diceDatabase)
    {
        List<DiceData> result = new List<DiceData>();
        if (diceDatabase == null)
            return result;

        for (int i = 0; i < runtimeDiceDatas.Count; i++)
        {
            DiceData current = runtimeDiceDatas[i];
            if (current == null)
                continue;

            DiceData upgrade = diceDatabase.GetDiceData(current.level + 1, current.type);
            if (upgrade == null)
                continue;

            if (!result.Contains(current))
                result.Add(current);
        }

        return result;
    }

    public List<DiceData> GetAddableDiceOptions(DiceDatabaseSO diceDatabase)
    {
        List<DiceData> result = new List<DiceData>();
        if (diceDatabase == null)
            return result;

        List<DiceData> levelOneDice = diceDatabase.GetAllByLevel(1);
        for (int i = 0; i < levelOneDice.Count; i++)
        {
            DiceData data = levelOneDice[i];
            if (data == null)
                continue;

            result.Add(data);
        }

        return result;
    }

    public void ResetSession()
    {
        runtimeDiceDatas.Clear();
        heroData = null;
        heroLevel = 0;
        initializedFromHero = false;
        heroStartDiceAdded = false;
        CPlayerPrefs.SetString(SaveKey, string.Empty);
    }

    void DebugLogRuntimeDice(string context)
    {
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < runtimeDiceDatas.Count; i++)
        {
            DiceData data = runtimeDiceDatas[i];
            if (data == null)
                continue;

            if (builder.Length > 0)
                builder.Append(", ");

            builder.Append(data.diceName)
                .Append("(L")
                .Append(data.level)
                .Append("-")
                .Append(data.type)
                .Append(")");
        }

        Debug.Log($"[ChapterDiceSession] {context} count={runtimeDiceDatas.Count} heroStartDiceAdded={heroStartDiceAdded} list=[{builder}]");
    }
}
