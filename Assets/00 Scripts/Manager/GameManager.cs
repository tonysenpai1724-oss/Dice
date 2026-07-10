using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UniRx;
using UnityEngine;
using UnityEngine.Rendering;

public class GameManager : Singleton<GameManager>
{

    public bool IsTester;
    public bool Initilized { get; private set; }

    public TelegrameUrlUser UserDecodeFromUrl = new TelegrameUrlUser();
    public string UserId { get; private set; }

    public EGameState GameState => _gameState;
    [SerializeField, ReadOnly]
    EGameState _gameState;
    public Dictionary<EButtonType, bool> DicButtonState { get; private set; }
    public EGameType GameType { get; private set; }
    public EBuildType BuildType { get; private set; }
    public EPlatform Platform { get; private set; }
    Dictionary<string, string> dicCommonConfigs;
    private void Start()
    {
        StartCoroutine(IEInit());
    }
    IEnumerator IEInit()
    {
        DicButtonState = new Dictionary<EButtonType, bool>();
        foreach (var item in Helper.GetListEnum<EButtonType>())
        {
            DicButtonState.Add(item, true);
        }
        yield return null;
        yield return new WaitUntil(() => LoadingPanel.Instance);
        yield return new WaitUntil(() => DataSystem.Instance);
        yield return new WaitUntil(() => AccountManager.Instance);

        var buildVersion = Resources.Load<BuildVersion>("BuildVersion");
        buildVersion.CorrectPlatform();
        BuildType = buildVersion.buildType;
        Platform = buildVersion.platform;
        Debug.Log($"Build {BuildType} version {buildVersion.version}");
#if UNITY_WEBGL
        if (Platform == EPlatform.Telegram)
            TelegramManager.InitializeSDK();
#endif

        yield return StartCoroutine(AccountManager.Instance.InitAccount());
        Debug.Log("Init Account Done");
        if (Platform != EPlatform.Android)
            yield return StartCoroutine(IEGetConfig());
        else
            dicCommonConfigs = new Dictionary<string, string>();
        Debug.Log("Get Config Done");
        yield return StartCoroutine(ITimerController.Instance.IEInit());
        yield return StartCoroutine(IPlayerResource.Instance.IEInit());
        yield return StartCoroutine(IGameSettingController.Instance.IEInit());
        yield return StartCoroutine(IAchievementController.Instance.IEInit());
        yield return StartCoroutine(IPlayerInfoController.Instance.IEInit());
        yield return StartCoroutine(IIAPController.Instance.IEInit());
        yield return StartCoroutine(IDailyQuestController.Instance.IEInit());
        yield return StartCoroutine(ILuckyWheelController.Instance.IEInit());
        if (Platform != EPlatform.Android)
        {
            yield return StartCoroutine(IFriendInviteController.Instance.IEInit());
            yield return StartCoroutine(IPartnerReferalController.Instance.IEInit());
            yield return StartCoroutine(ILeaderboardController.Instance.IEInit());
        }
        yield return StartCoroutine(IMailboxController.Instance.IEInit());

        LoadingPanel.Instance.ShowTextLoading("Loading Complete");
        yield return null;

        AudioManager.Instance.GameInit();
        Initilized = true;
        Observable.Interval(System.TimeSpan.FromSeconds(1), Scheduler.MainThreadIgnoreTimeScale).Subscribe(_ => OnTick()).AddTo(gameObject);
        yield return StartCoroutine(LoadingPanel.Instance.EndLoading());
        GoSceneHome();
    }
    IEnumerator IEGetConfig()
    {
        LoadingPanel.Instance.ShowTextLoading("Get Config");
        int count = 5;
        void LoadConfig()
        {
            count--;
            HTTPManager.Instance.GetCommonConfig(s =>
            {
                dicCommonConfigs = s;
            }, e =>
            {
                if (count > 0)
                    LoadConfig();
            });
        }
        LoadConfig();
        yield return new WaitUntil(() => dicCommonConfigs != null);
    }
    public string GetConfig(string key)
    {
        if (dicCommonConfigs.ContainsKey(key))
            return dicCommonConfigs[key];
        return string.Empty;
    }
    private void Update()
    {
        if (Initilized)
            TigerForge.EventManager.EmitEventData(Constant.EVENT_TIMER_UPDATE, Time.deltaTime);
    }
    private void OnTick()
    {
        if (Initilized)
            TigerForge.EventManager.EmitEvent(Constant.EVENT_TIMER_TICK);
    }
    public void ClearData()
    {
        StartCoroutine(IEClearData());
    }
    IEnumerator IEClearData()
    {
        Initilized = false;
        yield return StartCoroutine(SceneHelper.Instance.IEChangeSceneLoading());
        yield return StartCoroutine(ITimerController.Instance.IEClearData());
        yield return StartCoroutine(IPlayerResource.Instance.IEClearData());
        yield return StartCoroutine(IGameSettingController.Instance.IEClearData());
        yield return StartCoroutine(IAchievementController.Instance.IEClearData());
        yield return StartCoroutine(IPlayerInfoController.Instance.IEClearData());
        yield return StartCoroutine(IIAPController.Instance.IEClearData());
        yield return StartCoroutine(IDailyQuestController.Instance.IEClearData());
        yield return StartCoroutine(ILuckyWheelController.Instance.IEClearData());
        yield return StartCoroutine(IFriendInviteController.Instance.IEClearData());
        yield return StartCoroutine(IPartnerReferalController.Instance.IEClearData());
        yield return StartCoroutine(ILeaderboardController.Instance.IEClearData());
        yield return StartCoroutine(IMailboxController.Instance.IEClearData());
        StartCoroutine(IEInit());
    }
    public void PlayGame(EGameType gameType)
    {
        if (GameState == EGameState.Loading)
            return;

        PackageResource packCost = new PackageResource();
        packCost.AddResource(new CommonResource(ECommonResource.Energy, -1));
        packCost.ReceiveResource(EResourceFrom.SpendIngame, false, () =>
        {
            GameType = gameType;

            Level currentLevel = ChapterManager.Instance != null ? ChapterManager.Instance.GetCurrentLevel() : null;
            if (currentLevel != null)
            {
                switch (currentLevel.leveltype)
                {
                    case LevelType.MagicAltar:
                        UIManager.Instance?.ShowPopupClonePanel();
                        return;
                    case LevelType.Upgrade:
                        UIManager.Instance?.ShowPopupUpgradeDice();

                        return;
                    case LevelType.Shop:
                        UIManager.Instance?.ShowPopupShop();
                        return;
                    case LevelType.Roll:
                        UIManager.Instance?.ShowPopupRoll();
                        return;
                    case LevelType.Jester:
                        UIManager.Instance?.ShowPopupRollBuff();
                        return;
                }
            }

            GoSceneGameplay();
        });
    }

    public void CompleteCurrentSpecialLevel(LevelType expectedLevelType)
    {
        if (ChapterManager.Instance == null)
            return;

        Level currentLevel = ChapterManager.Instance.GetCurrentLevel();
        if (currentLevel == null || currentLevel.leveltype != expectedLevelType)
            return;

        if (ChapterManager.Instance.IsAtFinalPlayableLevel())
            return;

        ChapterManager.Instance.AdvanceAfterWin();
        UIManager.Instance?.RefreshHomeUI();
    }

    public void GoSceneHome()
    {
        SetState(EGameState.Loading);
        StartCoroutine(SceneHelper.Instance.IEReturnHome());
    }

    public void ReplayGame()
    {
        // if (GameState == EGameState.Loading)
        //     return;

        PlayerStats.Shared.ClearTemporaryStats();
        SetState(EGameState.Loading);
        StartCoroutine(IEReplayGame());
    }

    IEnumerator IEReplayGame()
    {
        yield return null;
        yield return StartCoroutine(SceneHelper.Instance.IEGoGameplay());
    }

    void GoSceneGameplay()
    {
        PlayerStats.Shared.ClearTemporaryStats();
        SetState(EGameState.Loading);
        StartCoroutine(SceneHelper.Instance.IEGoGameplay());
    }
    public void SetState(EGameState gameState)
    {
        _gameState = gameState;
    }
    public void SaveData(string key, string value)
    {
        CPlayerPrefs.SetString(key, value);
    }
    public string GetData(string key)
    {
        return CPlayerPrefs.GetString(key);
    }
    public void UpdateButtonState(EButtonType buttonType, bool canInteractive)
    {
        DicButtonState[buttonType] = canInteractive;
        TigerForge.EventManager.EmitEvent(Constant.EVENT_ON_BUTTON_STATE_CHANGE);

    }
    public void SaveLocalData(string key, string value)
    {
        CPlayerPrefs.SetString(key, value);
    }
    public string GetLocalData(string key)
    {
        return CPlayerPrefs.GetString(key);
    }
    public void GetUserDecodeFromUrl(string url)
    {
        UserDecodeFromUrl = TelegramUrlParser.GetUserDecodeFromUrl(url).User;
        UserId = UserDecodeFromUrl.Id.ToString();
    }
    #region Ads
    public void ShowAdsReward(System.Action actionComplete, string placement)
    {
        actionComplete?.Invoke();
        return;
    }
    public void ShowInterAds(System.Action actionClose, string placement)
    {
        actionClose?.Invoke();
        return;
    }
    #endregion
}



