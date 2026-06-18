using Cinemachine;
using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using static Cinemachine.DocumentationSortingAttribute;
using System.Linq;

public class GameplayManager : MonoBehaviour
{
    public EGamePlayState State => state;
    [SerializeField, ReadOnly] protected EGamePlayState state;
    public EGamePlayState LastState { get; private set; }
    public bool winGame { get; private set; }
    public int CurrentLevel { get; set; }
    public int LevelTime { get; private set; }
    public bool IsGameEnded;
    public int Score { get; private set; }
    public PackageResource PackReward { get; private set; }

    [Header("Dice Skill Runtime")]
    public DiceData skillDiceData;
    public DiceQueue skillQueue;
    public EnemyManager skillEnemyManager;
    public PlayerController skillPlayer;
    public Dice skillDice;
    public Enemy skillTargetEnemy;
    public int skillDamage;
    public int skillAttackCount;
    public bool skillSkipAttack;
    public int skillAfterAttackDamageAllEnemies;
    public static GameplayManager Instance;

    public int DiceDamage
    {
        get
        {
            int baseDamage = skillDiceData != null ? Mathf.Max(0, skillDiceData.damage) : 0;

            if (skillPlayer == null)
                return baseDamage;

            return Mathf.Max(0, baseDamage + skillPlayer.RuntimeDamage);
        }
    }
    public void Awake()
    {
        Instance = this;
    }

    public void BeginDiceSkill(
        DiceData diceData,
        DiceQueue queue,
        Dice dice,
        EnemyManager enemyManager,
        PlayerController player,
        Enemy targetEnemy
    )
    {
        ClearDiceSkillState();

        skillDiceData = diceData;
        skillQueue = queue;
        skillDice = dice;
        skillEnemyManager = enemyManager;
        skillPlayer = player;
        skillTargetEnemy = targetEnemy;
        skillDamage = DiceDamage;
        skillAttackCount = diceData != null ? Mathf.Max(1, diceData.attackCount) : 1;
    }

    public void AddDamage(int amount)
    {
        skillDamage = Mathf.Max(0, skillDamage + amount);
    }

    public void SetDamage(int amount)
    {
        skillDamage = Mathf.Max(0, amount);
    }

    public void AddAttackCount(int amount)
    {
        skillAttackCount = Mathf.Max(1, skillAttackCount + amount);
    }

    public void SetAttackCount(int amount)
    {
        skillAttackCount = Mathf.Max(1, amount);
    }

    public int GetAttackCount()
    {
        return Mathf.Max(1, skillAttackCount);
    }

    public void CancelAttack()
    {
        SetDamage(0);
        skillSkipAttack = true;
    }

    public Enemy GetTargetEnemy()
    {
        if (skillTargetEnemy != null &&
            skillTargetEnemy.gameObject.activeInHierarchy &&
            skillTargetEnemy.IsAlive())
        {
            return skillTargetEnemy;
        }

        return skillEnemyManager != null ? skillEnemyManager.GetNearestAliveEnemy() : null;
    }

    public void AddDamageAllEnemiesAfterAttack(int amount)
    {
        Debug.Log("AddDamageAllEnemiesAfterAttack: " + amount);
        if (amount <= 0)
            return;

        skillAfterAttackDamageAllEnemies += amount;
    }

    public void RunAfterAttackActions()
    {
        TigerForge.EventManager.EmitEvent(Constant.ON_DICE_AFTER_ATTACK);

        if (skillAfterAttackDamageAllEnemies > 0)
            OnDiceAfterAttack();
    }

    void OnDiceAfterAttack()
    {
        if (skillAfterAttackDamageAllEnemies <= 0 || skillEnemyManager == null)
            return;

        DamageAllEnemiesEffect damageAllEnemiesEffect =
            skillEnemyManager.effectManager?.AddEffect<DamageAllEnemiesEffect>();
        Debug.Log("OnDiceAfterAttack: " + skillAfterAttackDamageAllEnemies);

        if (damageAllEnemiesEffect != null)
            damageAllEnemiesEffect.Apply(skillAfterAttackDamageAllEnemies);

        skillAfterAttackDamageAllEnemies = 0;
    }

    public void ClearDiceSkillState()
    {
        skillDiceData = null;
        skillQueue = null;
        skillEnemyManager = null;
        skillPlayer = null;
        skillDice = null;
        skillTargetEnemy = null;
        skillDamage = 0;
        skillAttackCount = 1;
        skillSkipAttack = false;
        skillAfterAttackDamageAllEnemies = 0;
    }
    public void SpeedGame()
    {
        Time.timeScale += 0.5f;
        if (Time.timeScale > 2)
        {
            Time.timeScale = 1;
        }
        TigerForge.EventManager.EmitEvent(Constant.On_Speed_Changed);
    }

    public IEnumerator IEInit()
    {
        DebugCustom.LogColor("Init Level");
        SetState(EGamePlayState.Cinematic);

        IAchievementController.Instance.UpdateAchievementProgress(EAchievementType.LevelPlay);

        LevelTime = 180;
        // CurrentLevel = IPlayerInfoController.Instance.CurrentLevel();
        CurrentLevel = ChapterManager.Instance != null ? ChapterManager.Instance.CurrentLevelIndex + 1 : IPlayerInfoController.Instance.CurrentLevel();
        yield return new WaitUntil(() => ResolutionManager.Instance);
        yield return new WaitUntil(() => ResolutionManager.Instance.IsInitilized);
        yield return new WaitUntil(() => UIManager.Instance.uIGameplay);
        if (GameManager.Instance.GameType == EGameType.Endless)
            IAchievementController.Instance.UpdateAchievementProgress(EAchievementType.EndlessPlay);
        UIManager.Instance.uIGameplay.Initialize();
        StartGame();
        IsGameEnded = false;
        TigerForge.EventManager.StartListening(Constant.EVENT_TIMER_TICK, OnTick);
        TigerForge.EventManager.StartListening(Constant.ON_DICE_AFTER_ATTACK, OnDiceAfterAttack);
        TigerForge.EventManager.EmitEvent(Constant.EVENT_LEVEL_INITED);

    }

    void OnDestroy()
    {
        TigerForge.EventManager.StopListening(Constant.ON_DICE_AFTER_ATTACK, OnDiceAfterAttack);
    }
    public void StartGame()
    {
        winGame = false;
        SetState(EGamePlayState.Running);
    }
    private void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.X))
        {
            for (int i = 0; i < 10; i++)
            {
                OnTick();
            }
        }
#endif
    }
    void OnTick()
    {
        if (state == EGamePlayState.Running && LevelTime > 0)
        {
            if (LevelTime <= 0)
                EndGame(false);
        }

    }
    public void SetState(EGamePlayState _state)
    {
        state = _state;
        DebugCustom.LogColor("GamePlayState", State);
        if (state != EGamePlayState.Pause)
        {
            LastState = state;
            Time.timeScale = 1;
        }
        else
            Time.timeScale = 0;

        TigerForge.EventManager.EmitEvent(Constant.ON_GAME_STATE_CHANGE);
    }

    // public List<ChapterRewardChoiceOption> BuildChapterRewardChoices()
    // {
    //     List<ChapterRewardChoiceOption> rewardPool = new List<ChapterRewardChoiceOption>();
    //     List<ChapterRewardChoiceOption> finalOptions = new List<ChapterRewardChoiceOption>();

    //     DiceManager diceManager = DiceManager.Instance;
    //     ChapterDiceSession diceSession = ChapterDiceSession.GetOrCreate();

    //     if (diceManager != null && diceManager.diceDatabase != null && diceSession != null)
    //     {
    //         // Upgrade Dice rewards
    //         List<DiceData> upgradeable = diceSession.GetUpgradeableDiceOptions(diceManager.diceDatabase);
    //         for (int i = 0; i < upgradeable.Count; i++)
    //         {
    //             DiceData source = upgradeable[i];
    //             DiceData target = diceManager.diceDatabase.GetDiceData(source.level + 1, source.type);

    //             if (target != null)
    //             {
    //                 rewardPool.Add(new ChapterRewardChoiceOption
    //                 {
    //                     type = ChapterRewardChoiceType.UpgradeDice,
    //                     title = $"Upgrade {source.diceName}",
    //                     description = $"Upgrade to Lv{target.level}",
    //                     sourceDice = source,
    //                     targetDice = target
    //                 });
    //             }
    //         }

    //         // Add Dice rewards
    //         List<DiceData> addable = diceSession.GetAddableDiceOptions(diceManager.diceDatabase);
    //         for (int i = 0; i < addable.Count; i++)
    //         {
    //             DiceData dice = addable[i];

    //             rewardPool.Add(new ChapterRewardChoiceOption
    //             {
    //                 type = ChapterRewardChoiceType.AddDice,
    //                 title = $"Add {dice.diceName}",
    //                 description = $"Gain 1 new {dice.type} dice",
    //                 targetDice = dice
    //             });
    //         }
    //     }

    //     // Rune rewards
    //     List<RuneSkillData> runes = RuneManager.Instance.RuneSkillDatas;
    //     Debug.Log(runes.Count);
    //     if (runes != null)
    //     {
    //         for (int i = 0; i < runes.Count; i++)
    //         {
    //             RuneSkillData rune = runes[i];

    //             rewardPool.Add(new ChapterRewardChoiceOption
    //             {
    //                 type = ChapterRewardChoiceType.AddRune,
    //                 title = $"Add Rune {rune.TargetType}",
    //                 description = "Gain 1 rune for this run",
    //                 runeSkill = rune
    //             });
    //         }
    //     }

    //     // Random 3 unique rewards
    //     while (finalOptions.Count < 3)
    //     {
    //         if (!AddRandomUniqueRewardOption(finalOptions, rewardPool))
    //             break;
    //     }

    //     return finalOptions;
    // }
    public List<ChapterRewardChoiceOption> BuildChapterRewardChoices()
    {
        List<ChapterRewardChoiceOption> options = new List<ChapterRewardChoiceOption>();

        int safety = 30;

        while (options.Count < 3 && safety-- > 0)
        {
            ChapterRewardChoiceOption option = GenerateRandomReward();

            if (option == null)
                continue;

            if (!ContainsSameRewardOption(options, option))
            {
                options.Add(option);
            }
        }

        return options;
    }
    ChapterRewardChoiceOption GenerateRandomReward()
    {
        DiceManager diceManager = DiceManager.Instance;
        ChapterDiceSession diceSession = ChapterDiceSession.GetOrCreate();
        RuneSkillData[] runes = Resources.LoadAll<RuneSkillData>("00 Scripts/SO/Rune");

        int rand = Random.Range(0, 3);

        switch (rand)
        {
            case 0: // Upgrade Dice
                if (diceManager != null && diceManager.diceDatabase != null && diceSession != null)
                {
                    List<DiceData> upgradeable = diceSession.GetUpgradeableDiceOptions(diceManager.diceDatabase);

                    if (upgradeable.Count > 0)
                    {
                        DiceData source = upgradeable[Random.Range(0, upgradeable.Count)];
                        DiceData target = diceManager.diceDatabase.GetDiceData(source.level + 1, source.type);

                        if (target != null)
                        {
                            return new ChapterRewardChoiceOption
                            {
                                type = ChapterRewardChoiceType.UpgradeDice,
                                title = $"Upgrade {source.diceName}",
                                description = $"Upgrade to Lv{target.level}",
                                sourceDice = source,
                                targetDice = target
                            };
                        }
                    }
                }
                break;

            case 1: // Add Dice
                if (diceManager != null && diceManager.diceDatabase != null && diceSession != null)
                {
                    List<DiceData> addable = diceSession.GetAddableDiceOptions(diceManager.diceDatabase);

                    if (addable.Count > 0)
                    {
                        DiceData dice = addable[Random.Range(0, addable.Count)];

                        return new ChapterRewardChoiceOption
                        {
                            type = ChapterRewardChoiceType.AddDice,
                            title = $"Add {dice.diceName}",
                            description = $"Gain 1 new {dice.type} dice",
                            targetDice = dice
                        };
                    }
                }
                break;

            case 2: // Rune
                if (runes != null && runes.Length > 0)
                {
                    RuneSkillData rune = runes[Random.Range(0, runes.Length)];

                    return new ChapterRewardChoiceOption
                    {
                        type = ChapterRewardChoiceType.AddRune,
                        title = $"Add Rune {rune.TargetType}",
                        description = "Gain 1 rune for this run",
                        runeSkill = rune
                    };
                }
                break;
        }

        return null;
    }

    bool AddRandomUniqueRewardOption(List<ChapterRewardChoiceOption> currentOptions, List<ChapterRewardChoiceOption> sourcePool)
    {
        if (currentOptions == null || sourcePool == null || sourcePool.Count == 0)
            return false;
        List<ChapterRewardChoiceOption> candidates = new List<ChapterRewardChoiceOption>();
        for (int i = 0; i < sourcePool.Count; i++)
        {
            ChapterRewardChoiceOption candidate = sourcePool[i];
            if (candidate == null)
                continue;
            if (ContainsSameRewardOption(currentOptions, candidate))
                continue;
            candidates.Add(candidate);
        }
        if (candidates.Count == 0)
            return false;
        currentOptions.Add(candidates[Random.Range(0, candidates.Count)]);
        return true;
    }
    bool ContainsSameRewardOption(List<ChapterRewardChoiceOption> currentOptions, ChapterRewardChoiceOption candidate)
    {
        if (currentOptions == null || candidate == null)
            return false;
        for (int i = 0; i < currentOptions.Count; i++)
        {
            ChapterRewardChoiceOption current = currentOptions[i];
            if (current == null)
                continue;
            if (current.type != candidate.type)
                continue;
            switch (candidate.type)
            {
                case ChapterRewardChoiceType.UpgradeDice:
                    if (current.sourceDice == candidate.sourceDice && current.targetDice == candidate.targetDice)
                        return true;
                    break;
                case ChapterRewardChoiceType.AddDice:
                    if (current.targetDice == candidate.targetDice)
                        return true;
                    break;
                case ChapterRewardChoiceType.AddRune:
                    if (current.runeSkill == candidate.runeSkill)
                        return true;
                    break;
            }
        }
        return false;
    }
    public void ApplyChapterRewardChoice(ChapterRewardChoiceOption option)
    {
        if (option == null)
            return;

        ChapterDiceSession diceSession = ChapterDiceSession.GetOrCreate();

        switch (option.type)
        {
            case ChapterRewardChoiceType.UpgradeDice:
                if (option.sourceDice != null && option.targetDice != null)
                    diceSession.UpgradeDiceData(option.sourceDice, option.targetDice);
                break;
            case ChapterRewardChoiceType.AddDice:
                if (option.targetDice != null)
                    diceSession.AddDiceData(option.targetDice);
                break;
            case ChapterRewardChoiceType.AddRune:
                if (option.runeSkill != null)
                    RuneManager.Instance?.TryAddRune(option.runeSkill);
                break;
        }
    }
    public void EndGame(bool win)
    {
        if (IsGameEnded)
            return;

        DebugCustom.LogColor("End Game");
        winGame = win;
        SetState(EGamePlayState.GameOver);
        IsGameEnded = true;

        if (winGame)
        {
            TigerForge.EventManager.EmitEvent(Constant.ON_END_GAME);
            // if (ChapterManager.Instance != null)
            //     ChapterManager.Instance.AdvanceAfterWin();
            // else
            //     IPlayerInfoController.Instance.WinLevel();

            IAchievementController.Instance.UpdateAchievementProgress(EAchievementType.LevelWin);
            if (GameManager.Instance.GameType == EGameType.Endless)
                IAchievementController.Instance.UpdateAchievementProgress(EAchievementType.WinEndlessStage);
            IAchievementController.Instance.UpdateAchievementProgress(EAchievementType.LevelWin);

            PackReward = new PackageResource();
            PackReward.AddResource(new CommonResource(ECommonResource.Coin, 15));
            PackReward.AddResource(new CommonResource(ECommonResource.Gem, 10));
            PackReward.AddResource(new CommonResource(ECommonResource.ActivePoint, 1));

            PackReward.ReceiveResource(EResourceFrom.ReviveIngame);
            List<ChapterRewardChoiceOption> rewardChoices = BuildChapterRewardChoices();
            if (rewardChoices != null && rewardChoices.Count > 0)
                UIManager.Instance.ShowPopupChoice(rewardChoices);
            else
                UIManager.Instance.ShowPopupEndGame();

        }
        else
        {
            TigerForge.EventManager.EmitEvent(Constant.ON_END_GAME);
            if (GameManager.Instance.GameType == EGameType.Endless)
            {
                PackReward = new PackageResource();
                PackReward.AddResource(new CommonResource(ECommonResource.Coin, Score));
                PackReward.AddResource(new CommonResource(ECommonResource.ActivePoint, Score / 10));
            }
            UIManager.Instance.ShowPopupEndGame();
        }
    }
    public void OnClick(Vector3 pos)
    {
        DebugCustom.LogColor("OnClick", pos);
    }
}



