using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class RelicManager : MonoBehaviour
{
    public static RelicManager Instance { get; private set; }

    [SerializeField] private List<RelicData> activeRelics = new();
    [SerializeField] private List<RelicData> startingRelics = new();
    [SerializeField] private RelicDatabaseSO relicDatabase;
    [SerializeField] private List<RelicData> allRelics = new();

    readonly Dictionary<RelicData, int> activeRelicLevels = new();
    int currentChapterId = -1;

    public IReadOnlyList<RelicData> ActiveRelics => activeRelics;

    public static RelicManager GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        RelicManager manager = FindAnyObjectByType<RelicManager>();
        if (manager != null)
            return manager;

        GameObject managerObject = new GameObject("RelicManager");
        return managerObject.AddComponent<RelicManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        NormalizeActiveRelics();
        SyncChapterScope();
    }

    public bool TryAddRelic(RelicData relic)
    {
        if (relic == null)
            return false;

        SyncChapterScope();
        int relicLevel = AddRelicLevel(relic);
        Debug.Log($"[RelicManager] Add relic {relic.name} ({relic.TargetType}) level {relicLevel} value {relic.GetValueApply(relicLevel)}");

        ExecuteLevelStartRelic(relic);
        return true;
    }

    public void ClearChapterRelics()
    {
        activeRelics.Clear();
        activeRelicLevels.Clear();
        AddStartingRelics();
    }

    public void OnLevelStarted(Level level)
    {
        SyncChapterScope();
        ExecuteLevelStartRelics();
    }

    public DiceData ResolveDiceDataBeforeSkill(DiceData diceData)
    {
        if (diceData == null)
            return null;

        SyncChapterScope();
        DiceData resolvedDiceData = diceData;
        for (int i = 0; i < activeRelics.Count; i++)
        {
            RelicData relic = activeRelics[i];
            if (relic == null)
                continue;

            resolvedDiceData = relic.ResolveDiceDataBeforeSkill(resolvedDiceData, GetRelicLevel(relic));
        }

        return resolvedDiceData;
    }

    public void ApplyBeforeDiceSkill(DiceData diceData, GameplayManager gameplay)
    {
        if (diceData == null || gameplay == null)
            return;

        SyncChapterScope();
        for (int i = 0; i < activeRelics.Count; i++)
        {
            RelicData relic = activeRelics[i];
            if (relic != null)
                relic.ApplyBeforeDiceSkill(diceData, gameplay, GetRelicLevel(relic));
        }
    }

    public void ModifyPlayerAttackDamage(PlayerAttackDamageContext context)
    {
        if (context == null || context.Damage <= 0)
            return;

        SyncChapterScope();
        for (int i = 0; i < activeRelics.Count; i++)
        {
            RelicData relic = activeRelics[i];
            if (relic != null)
                relic.ModifyPlayerAttackDamage(context, GetRelicLevel(relic));
        }
    }

    public void NotifyPlayerDealtDamage(PlayerController player, int damage)
    {
        if (player == null || damage <= 0)
            return;

        SyncChapterScope();
        for (int i = 0; i < activeRelics.Count; i++)
        {
            RelicData relic = activeRelics[i];
            if (relic != null)
                relic.NotifyPlayerDealtDamage(player, damage, GetRelicLevel(relic));
        }
    }

    public void ExecuteLevelStartRelics()
    {
        for (int i = 0; i < activeRelics.Count; i++)
        {
            RelicData relic = activeRelics[i];
            relic?.Execute(GetRelicLevel(relic));
        }
    }

    public void OnPlayerTurnStarted()
    {
        SyncChapterScope();

        for (int i = 0; i < activeRelics.Count; i++)
        {
            RelicData relic = activeRelics[i];
            if (relic != null && relic.TargetType == RelicType.RelicArmorTurn)
                relic.Execute(GetRelicLevel(relic));
        }
    }

    public void ExecuteLevelStartRelic(RelicData relic)
    {
        relic?.Execute(GetRelicLevel(relic));
    }

    public bool ShouldCloneMergedDice()
    {
        SyncChapterScope();

        for (int i = 0; i < activeRelics.Count; i++)
        {
            RelicData relic = activeRelics[i];
            if (relic != null && relic.ShouldCloneMergedDice(GetRelicLevel(relic)))
                return true;
        }

        return false;
    }

    public int GetRelicLevel(RelicData relic)
    {
        if (relic == null)
            return 0;

        if (activeRelicLevels.TryGetValue(relic, out int level))
            return Mathf.Max(1, level);

        return activeRelics.Contains(relic) ? 1 : 0;
    }

    void SyncChapterScope()
    {
        int chapterId = ChapterManager.Instance != null ? ChapterManager.Instance.CurrentChapterId : 0;
        if (currentChapterId < 0)
        {
            currentChapterId = chapterId;
            AddStartingRelics();
            return;
        }

        if (currentChapterId == chapterId)
            return;

        currentChapterId = chapterId;
        ClearChapterRelics();
    }

    void AddStartingRelics()
    {
        if (startingRelics == null)
            return;

        for (int i = 0; i < startingRelics.Count; i++)
        {
            RelicData relic = startingRelics[i];
            if (relic != null)
                AddRelicLevel(relic);
        }
    }

    int AddRelicLevel(RelicData relic)
    {
        if (relic == null)
            return 0;

        int nextLevel = GetRelicLevel(relic) + 1;
        if (!activeRelics.Contains(relic))
            activeRelics.Add(relic);

        activeRelicLevels[relic] = nextLevel;
        return nextLevel;
    }

    void NormalizeActiveRelics()
    {
        activeRelicLevels.Clear();
        if (activeRelics == null)
        {
            activeRelics = new List<RelicData>();
            return;
        }

        List<RelicData> uniqueRelics = new List<RelicData>();
        for (int i = 0; i < activeRelics.Count; i++)
        {
            RelicData relic = activeRelics[i];
            if (relic == null)
                continue;

            if (!uniqueRelics.Contains(relic))
                uniqueRelics.Add(relic);

            activeRelicLevels.TryGetValue(relic, out int currentLevel);
            activeRelicLevels[relic] = currentLevel + 1;
        }

        activeRelics.Clear();
        activeRelics.AddRange(uniqueRelics);
    }

    [Button]
    public void AddAllRelic()
    {
        SyncChapterScope();

        List<RelicData> relics = relicDatabase != null ? relicDatabase.relicDatas : allRelics;
        if (relics == null)
            return;

        for (int i = 0; i < relics.Count; i++)
        {
            RelicData relic = relics[i];
            if (relic == null)
                continue;

            int relicLevel = AddRelicLevel(relic);
            Debug.Log($"[RelicManager] Add all relic {relic.name} ({relic.TargetType}) level {relicLevel} value {relic.GetValueApply(relicLevel)}");
            ExecuteLevelStartRelic(relic);
        }
    }
}
