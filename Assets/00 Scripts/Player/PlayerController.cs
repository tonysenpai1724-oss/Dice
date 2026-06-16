using System.Collections;
using System.Collections.Generic;
using TigerForge;
using UnityEngine;

public class PlayerController : GameUnit
{
    public HeroData data;
    public List<DiceData> diceDatas = new();
    public EquipmentManager equipmentManager;
    public PlayerRuntimeStats runtimeStats;

    public int RuntimeDamage => runtimeStats != null ? runtimeStats.FinalStats.damage : (data != null ? data.damage : 0);
    public int RuntimeDefense => runtimeStats != null ? runtimeStats.FinalStats.defense : (data != null ? data.def : 0);
    public float RuntimeCritDamage => runtimeStats != null ? runtimeStats.FinalStats.critDamage : (data != null ? data.critDmg : 0f);
    public float RuntimeCritRate => runtimeStats != null ? runtimeStats.FinalStats.critRate : (data != null ? data.critRate : 0f);
    public float RuntimeLuck => runtimeStats != null ? runtimeStats.FinalStats.luck : (data != null ? data.luck : 0f);

    public void Setup(HeroData newData)
    {
        data = newData;
        EnsureRuntimeStats();
        InitializeDiceDatas();
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

        if (data == null || data.startDiceLevelConfig == null)
            return;

        if (!data.startDiceLevelConfig.TryGetValue(data.level, out List<DiceData> startDices))
            return;

        if (startDices == null)
            return;

        for (int i = 0; i < startDices.Count; i++)
        {
            if (startDices[i] != null)
                diceDatas.Add(startDices[i]);
        }
    }

    public void AddDiceData(DiceData diceData)
    {
        if (diceData == null)
            return;

        diceDatas.Add(diceData);
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

        EnsureRuntimeStats();
        runtimeStats.SetBaseStats(data);

        EquipmentManager resolvedEquipmentManager = equipmentManager != null
            ? equipmentManager
            : EquipmentManager.Instance;

        List<HeroStatModifier> modifiers = resolvedEquipmentManager != null
            ? resolvedEquipmentManager.BuildRuntimeModifiers()
            : null;

        runtimeStats.SetModifiers(modifiers);

        HeroStatSnapshot finalStats = runtimeStats.FinalStats;
        int currentHpValue = currentHp > 0 ? currentHp : finalStats.hp;
        SetHealth(finalStats.hp, finalStats.hp);
        Debug.Log("dam base:" + data.damage + "- dam runtime:" + finalStats.damage);
    }

    void RefreshStatsFromEquipmentEvent()
    {
        RefreshStatsFromEquipment();
    }

    void EnsureRuntimeStats()
    {
        if (runtimeStats == null)
            runtimeStats = GetComponent<PlayerRuntimeStats>();

        if (runtimeStats == null)
            runtimeStats = gameObject.AddComponent<PlayerRuntimeStats>();
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


