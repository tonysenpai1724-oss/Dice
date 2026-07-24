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
    public DiceQueueManager skillQueue;
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
        DiceQueueManager queue,
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
        IsGameEnded = false;
        if (TryShowSpecialLevelPopup())
            yield break;

        StartGame();
        TigerForge.EventManager.StartListening(Constant.EVENT_TIMER_TICK, OnTick);
        TigerForge.EventManager.StartListening(Constant.ON_DICE_AFTER_ATTACK, OnDiceAfterAttack);
        TigerForge.EventManager.EmitEvent(Constant.EVENT_LEVEL_INITED);

    }

    bool TryShowSpecialLevelPopup()
    {
        Level currentLevel = ChapterManager.Instance != null ? ChapterManager.Instance.GetCurrentLevel() : null;
        if (currentLevel == null)
            return false;

        switch (currentLevel.leveltype)
        {
            case LevelType.MagicAltar:
                DiceManager.Instance?.ClearBoard();
                UIManager.Instance?.ShowPopupClonePanel();
                return true;
            case LevelType.Shop:
                DiceManager.Instance?.ClearBoard();
                UIManager.Instance?.ShowPopupShop();
                return true;
            case LevelType.Roll:
                DiceManager.Instance?.ClearBoard();
                UIManager.Instance?.ShowPopupRoll();
                return true;
            case LevelType.RollBuff:
                DiceManager.Instance?.ClearBoard();
                UIManager.Instance?.ShowPopupRollBuff();
                return true;
            case LevelType.Upgrade:
                DiceManager.Instance?.ClearBoard();
                UIManager.Instance?.ShowPopupUpgradeClone();
                return true;
            case LevelType.Jester:
                return true;

        }

        return false;
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

    public List<ChapterRewardChoiceOption> BuildChapterRewardChoices(LevelType levelType)
    {
        ChapterRewardChoiceBuilder builder = new ChapterRewardChoiceBuilder(
            DiceManager.Instance != null ? DiceManager.Instance.diceDatabase : null,
            ChapterDiceSession.GetOrCreate(),
            RuneManager.Instance.RuneSkillDatas
        );
        if (levelType == LevelType.Chest || levelType == LevelType.ChestReward)
            return builder.BuildChestChoices();

        else
            return builder.BuildChoices();
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
            Level completedLevel = ChapterManager.Instance != null ? ChapterManager.Instance.GetCurrentLevel() : null;

            TigerForge.EventManager.EmitEvent(Constant.ON_END_GAME);

            IAchievementController.Instance.UpdateAchievementProgress(EAchievementType.LevelWin);
            if (GameManager.Instance.GameType == EGameType.Endless)
                IAchievementController.Instance.UpdateAchievementProgress(EAchievementType.WinEndlessStage);
            IAchievementController.Instance.UpdateAchievementProgress(EAchievementType.LevelWin);

            PackReward = new PackageResource();
            PackReward.AddResource(new CommonResource(ECommonResource.Coin, 15));
            PackReward.AddResource(new CommonResource(ECommonResource.Gem, 10));
            PackReward.AddResource(new CommonResource(ECommonResource.ActivePoint, 1));

            PackReward.ReceiveResource(EResourceFrom.ReviveIngame);

            List<ChapterRewardChoiceOption> rewardChoices = completedLevel != null ? BuildChapterRewardChoices(completedLevel.leveltype) : null;

            if (ChapterManager.Instance != null)
                ChapterManager.Instance.AdvanceAfterWin();
            else
                IPlayerInfoController.Instance.WinLevel();

            TigerForge.EventManager.EmitEvent(Constant.ON_WIN_LEVEL);

            if (rewardChoices != null && rewardChoices.Count > 0)
                UIManager.Instance.ShowPopupChoice(rewardChoices);
            else
                ContinueAfterWinReward();

        }
        else
        {
            TigerForge.EventManager.EmitEvent(Constant.ON_END_GAME);
            TigerForge.EventManager.EmitEvent(Constant.ON_LOSE_LEVEL);
            if (GameManager.Instance.GameType == EGameType.Endless)
            {
                PackReward = new PackageResource();
                PackReward.AddResource(new CommonResource(ECommonResource.Coin, Score));
                PackReward.AddResource(new CommonResource(ECommonResource.ActivePoint, Score / 10));
            }
            UIManager.Instance.ShowPopupEndGame();
        }
    }

    public void ContinueAfterWinReward()
    {
        if (!winGame)
        {
            UIManager.Instance.ShowPopupEndGame();
            return;
        }

        GameManager.Instance.ContinueGameplay();
    }

    public void ContinueCurrentLevelInScene()
    {
        LevelTime = 180;
        CurrentLevel = ChapterManager.Instance != null ? ChapterManager.Instance.CurrentLevelIndex + 1 : IPlayerInfoController.Instance.CurrentLevel();
        IsGameEnded = false;
        ClearDiceSkillState();
        SetState(EGamePlayState.Cinematic);

        DiceThrowController diceThrowController = FindFirstObjectByType<DiceThrowController>();
        bool popupOnlyGameplay = GameManager.Instance != null && GameManager.Instance.IsCurrentLevelPopupOnlyGameplay();
        if (diceThrowController != null)
        {
            if (popupOnlyGameplay)
                diceThrowController.DisableThrowControllerForPopupOnlyLevel();
            else
                diceThrowController.Clear();
        }

        if (DiceManager.Instance != null)
            DiceManager.Instance.ClearBoard();

        if (TryShowSpecialLevelPopup())
            return;

        PlayerController player = EnemyManager.Instance != null ? EnemyManager.Instance.player : null;
        if (player != null)
            player.InitializeDiceDatas();

        if (DiceManager.Instance != null)
            DiceManager.Instance.ResetBoard();

        if (LevelManager.Instance != null)
            LevelManager.Instance.LoadCurrentLevel();

        if (diceThrowController != null)
            diceThrowController.ResetForNextLevel();

        StartGame();
        TigerForge.EventManager.EmitEvent(Constant.EVENT_LEVEL_INITED);
    }

    public void OnClick(Vector3 pos)
    {
        DebugCustom.LogColor("OnClick", pos);
    }
}



