using System.Collections;
using System.Collections.Generic;
using System.Text;
using TigerForge;
using UnityEngine;

public class PlayerController : GameUnit
{
    public HeroData data;
    public List<DiceData> diceDatas = new();
    public EquipmentManager equipmentManager;
    public PlayerStats playerStats = PlayerStats.Shared;
    public string comboAttackAnim = "Attack1";
    public int comboAttackThreshold = 3;

    int consecutiveAttackCount;

    public int RuntimeDamage => playerStats != null
        ? Mathf.RoundToInt(playerStats.GetStatValue(HeroStatType.Damage))
        : (data != null ? data.damage : 0);

    public int RuntimeDefense => playerStats != null
        ? Mathf.RoundToInt(playerStats.GetStatValue(HeroStatType.Defense))
        : (data != null ? data.def : 0);

    public float RuntimeCritDamage => playerStats != null
        ? playerStats.GetStatValue(HeroStatType.CritDamage)
        : (data != null ? data.critDmg : 0f);

    public float RuntimeCritRate => playerStats != null
        ? playerStats.GetStatValue(HeroStatType.CritRate)
        : (data != null ? data.critRate : 0f);

    public float RuntimeLuck => playerStats != null
        ? playerStats.GetStatValue(HeroStatType.Luck)
        : (data != null ? data.luck : 0f);

    public void Setup(HeroData newData)
    {
        HeroData resolvedData = ResolveHeroData(newData);
        data = resolvedData;
        if (resolvedData == null)
            return;

        ChapterDiceSession.GetOrCreate().SetSelectedHero(resolvedData);
        EnsurePlayerStats();
        InitializeDiceDatas();
        playerStats.InitStats();
        playerStats.RebuildFromCurrentSources(data);
        RefreshStatsFromEquipment();

        if (skeletonGraphic != null)
        {
            skeletonGraphic.skeletonDataAsset = data.skeletonData;
            skeletonGraphic.Initialize(true);
            PlayAnimation(idleAnim, true);
        }
    }

    HeroData ResolveHeroData(HeroData fallbackData)
    {
        if (fallbackData != null)
            return fallbackData;

        ChapterDiceSession diceSession = ChapterDiceSession.GetOrCreate();
        HeroData sessionHero = diceSession.ResolveHeroData();
        if (sessionHero != null)
            return sessionHero;

        HeroSelectionSession heroSession = HeroSelectionSession.GetOrCreate();
        return heroSession.GetSelectedHero();
    }

    void Start()
    {
        Setup(data);
    }

    void OnEnable()
    {
        EventManager.StartListening(Constant.ON_PLAYER_EQUIPMENT_STATS_CHANGED, RefreshStatsFromEquipmentEvent);
        EventManager.StartListening(Constant.ON_PLAYER_STATS_CHANGED, RefreshStatsFromEquipmentEvent);
    }

    void OnDisable()
    {
        EventManager.StopListening(Constant.ON_PLAYER_EQUIPMENT_STATS_CHANGED, RefreshStatsFromEquipmentEvent);
        EventManager.StopListening(Constant.ON_PLAYER_STATS_CHANGED, RefreshStatsFromEquipmentEvent);
    }

    public void InitializeDiceDatas()
    {
        HeroData resolvedData = ResolveHeroData(data);
        if (resolvedData == null)
            return;

        ChapterDiceSession session = ChapterDiceSession.GetOrCreate();
        session.InitializeFromHero(resolvedData);

        List<DiceData> runtimeDiceDatas = session.GetRuntimeDiceDatasCopy();
        diceDatas.Clear();

        if (runtimeDiceDatas == null || runtimeDiceDatas.Count == 0)
        {
            DebugLogDiceDatas("InitializeDiceDatas empty-from-session");
            return;
        }

        diceDatas.AddRange(runtimeDiceDatas);
        DebugLogDiceDatas("InitializeDiceDatas after-copy");
    }

    public void AddDiceData(DiceData diceData)
    {
        if (diceData == null)
            return;

        diceDatas.Add(diceData);
        DebugLogDiceDatas($"AddDiceData before-session added={diceData.diceName}");
        ChapterDiceSession.GetOrCreate().AddDiceData(diceData);
        DebugLogDiceDatas($"AddDiceData after-session added={diceData.diceName}");
    }

    public void AddDiceDatas(List<DiceData> newDiceDatas)
    {
        if (newDiceDatas == null)
            return;

        for (int i = 0; i < newDiceDatas.Count; i++)
        {
            AddDiceData(newDiceDatas[i]);
        }
    }

    public void SetHp(int hp, int currentHp)
    {
        SetHealth(hp, currentHp);

        if (skeletonGraphic != null)
        {
            skeletonGraphic.Initialize(true);
            PlayAnimation(idleAnim, true);
        }
    }

    public void RefreshStatsFromEquipment()
    {
        if (data == null)
            return;

        EnsurePlayerStats();
        Debug.Log($"[PlayerController] RefreshStatsFromEquipment before rebuild | currentHp={currentHp} hp={hp}");
        playerStats.RebuildFromCurrentSources(data);

        HeroStatSnapshot finalStats = playerStats.ToHeroStatSnapshot(data);
        int currentHpValue = currentHp > 0 ? Mathf.Min(currentHp, finalStats.hp) : finalStats.hp;
        Debug.Log($"[PlayerController] RefreshStatsFromEquipment after rebuild | hp={finalStats.hp} dmg={finalStats.damage} def={finalStats.defense} critRate={finalStats.critRate} critDmg={finalStats.critDamage} luck={finalStats.luck} currentHpTarget={currentHpValue}");
        SetHealth(finalStats.hp, currentHpValue);
    }


    public string GetNextAttackAnimation()
    {
        consecutiveAttackCount++;

        bool useComboAttack = !string.IsNullOrEmpty(comboAttackAnim) &&
                              consecutiveAttackCount >= Mathf.Max(1, comboAttackThreshold);

        if (useComboAttack)
        {
            consecutiveAttackCount = 0;
            return comboAttackAnim;
        }

        return attackAnim;
    }

    public void ResetAttackCombo()
    {
        consecutiveAttackCount = 0;
    }

    void RefreshStatsFromEquipmentEvent()
    {
        RefreshStatsFromEquipment();
    }

    void EnsurePlayerStats()
    {
        if (playerStats == null)
            playerStats = PlayerStats.Shared;
    }

    IEnumerator PlayHurtDelayed()
    {
        yield return new WaitForSeconds(0f);
        PlayAnimation(hurtAnim, false);
        QueueIdleAnimation();
    }

    public override void OnDie()
    {
        NotifyDied();
        PlayAnimation(dieAnim, false);

        if (currentTrack == null)
        {
            EndGame();
            return;
        }

        currentTrack.Complete += _ => EndGame();
    }

    void EndGame()
    {
        Debug.Log("Player Died");
        if (GameplayManager.Instance != null)
            GameplayManager.Instance.EndGame(false);
    }

    void DebugLogDiceDatas(string context)
    {
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < diceDatas.Count; i++)
        {
            DiceData diceData = diceDatas[i];
            if (diceData == null)
                continue;

            if (builder.Length > 0)
                builder.Append(", ");

            builder.Append(diceData.diceName)
                .Append("(L")
                .Append(diceData.level)
                .Append("-")
                .Append(diceData.type)
                .Append(")");
        }

        Debug.Log($"[PlayerController] {context} count={diceDatas.Count} list=[{builder}]");
    }
}








