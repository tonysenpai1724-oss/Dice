using System.Collections;
using System.Collections.Generic;
using TigerForge;
using UnityEngine;

public class PlayerController : GameUnit
{
    public HeroData data;
    public List<DiceData> diceDatas = new();
    public EquipmentManager equipmentManager;
    public PlayerStats playerStats = PlayerStats.Shared;

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
        data = newData;
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

    void Start()
    {
        Setup(data);
    }

    void OnEnable()
    {
        EventManager.StartListening(Constant.ON_PLAYER_EQUIPMENT_STATS_CHANGED, RefreshStatsFromEquipmentEvent);
    }

    void OnDisable()
    {
        EventManager.StopListening(Constant.ON_PLAYER_EQUIPMENT_STATS_CHANGED, RefreshStatsFromEquipmentEvent);
    }

    public void InitializeDiceDatas()
    {
        diceDatas.Clear();

        if (data == null)
            return;

        ChapterDiceSession session = ChapterDiceSession.GetOrCreate();
        session.InitializeFromHero(data);
        diceDatas = session.GetRuntimeDiceDatasCopy();
    }

    public void AddDiceData(DiceData diceData)
    {
        if (diceData == null)
            return;

        diceDatas.Add(diceData);
        ChapterDiceSession.GetOrCreate().AddDiceData(diceData);
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
        playerStats.RebuildFromCurrentSources(data);

        HeroStatSnapshot finalStats = playerStats.ToHeroStatSnapshot(data);
        int currentHpValue = currentHp > 0 ? currentHp : finalStats.hp;
        SetHealth(finalStats.hp, finalStats.hp);
        Debug.Log("dam base:" + data.damage + "- dam runtime:" + finalStats.damage);
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
}
