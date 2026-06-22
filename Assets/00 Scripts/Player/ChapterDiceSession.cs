using System.Collections.Generic;
using UnityEngine;

public class ChapterDiceSession : MonoBehaviour
{
    public static ChapterDiceSession Instance;

    [SerializeField] HeroData heroData;
    [SerializeField] int heroLevel;
    [SerializeField] List<DiceData> runtimeDiceDatas = new();

    public IReadOnlyList<DiceData> RuntimeDiceDatas => runtimeDiceDatas;

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static ChapterDiceSession GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        GameObject sessionObject = new GameObject("ChapterDiceSession");
        return sessionObject.AddComponent<ChapterDiceSession>();
    }

    public void InitializeFromHero(HeroData sourceHeroData)
    {
        if (sourceHeroData == null)
            return;

        bool sameHero = heroData == sourceHeroData;
        bool sameHeroLevel = heroLevel == sourceHeroData.level;
        bool hasRuntimeDice = runtimeDiceDatas.Count > 0;

        if (sameHero && sameHeroLevel && hasRuntimeDice)
            return;

        if (hasRuntimeDice && sameHeroLevel)
        {
            heroData = sourceHeroData;
            heroLevel = sourceHeroData.level;
            return;
        }

        heroData = sourceHeroData;
        heroLevel = sourceHeroData.level;
        runtimeDiceDatas.Clear();

        if (sourceHeroData.startDiceLevelConfig == null)
            return;

        if (!sourceHeroData.startDiceLevelConfig.TryGetValue(heroLevel, out List<DiceData> startDices))
            return;

        if (startDices == null)
            return;

        for (int i = 0; i < startDices.Count; i++)
        {
            if (startDices[i] != null)
                runtimeDiceDatas.Add(startDices[i]);
        }

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

    public bool UpgradeDiceData(DiceData currentDiceData, DiceData upgradedDiceData)
    {
        if (currentDiceData == null || upgradedDiceData == null)
            return false;

        for (int i = 0; i < runtimeDiceDatas.Count; i++)
        {
            if (runtimeDiceDatas[i] != currentDiceData)
                continue;

            runtimeDiceDatas[i] = upgradedDiceData;
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
    }
}
