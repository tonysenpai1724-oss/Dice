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
        SyncChapterScope();
    }

    public bool TryAddRelic(RelicData relic)
    {
        if (relic == null)
            return false;

        SyncChapterScope();
        if (!activeRelics.Contains(relic))
            activeRelics.Add(relic);

        ExecuteLevelStartRelic(relic);
        return true;
    }

    public void ClearChapterRelics()
    {
        activeRelics.Clear();
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

            resolvedDiceData = relic.ResolveDiceDataBeforeSkill(resolvedDiceData);
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
                relic.ApplyBeforeDiceSkill(diceData, gameplay);
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
                relic.ModifyPlayerAttackDamage(context);
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
                relic.NotifyPlayerDealtDamage(player, damage);
        }
    }

    public void ExecuteLevelStartRelics()
    {
        for (int i = 0; i < activeRelics.Count; i++)
        {
            activeRelics[i]?.Execute();
        }
    }

    public void ExecuteLevelStartRelic(RelicData relic)
    {
        relic?.Execute();
    }

    void SyncChapterScope()
    {
        int chapterId = ChapterManager.Instance != null ? ChapterManager.Instance.CurrentChapterId : currentChapterId;
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
            if (relic != null && !activeRelics.Contains(relic))
                activeRelics.Add(relic);
        }
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
            if (relic == null || activeRelics.Contains(relic))
                continue;

            activeRelics.Add(relic);
            ExecuteLevelStartRelic(relic);
        }
    }
}
