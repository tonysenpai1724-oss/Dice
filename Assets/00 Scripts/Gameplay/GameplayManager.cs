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
            if (ChapterManager.Instance != null)
                ChapterManager.Instance.AdvanceAfterWin();
            else
                IPlayerInfoController.Instance.WinLevel();
            IAchievementController.Instance.UpdateAchievementProgress(EAchievementType.LevelWin);
            if (GameManager.Instance.GameType == EGameType.Endless)
                IAchievementController.Instance.UpdateAchievementProgress(EAchievementType.WinEndlessStage);
            IAchievementController.Instance.UpdateAchievementProgress(EAchievementType.LevelWin);

            PackReward = new PackageResource();
            PackReward.AddResource(new CommonResource(ECommonResource.Coin, 15));
            PackReward.AddResource(new CommonResource(ECommonResource.Gem, 10));
            PackReward.AddResource(new CommonResource(ECommonResource.ActivePoint, 1));

            PackReward.ReceiveResource(EResourceFrom.ReviveIngame);
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