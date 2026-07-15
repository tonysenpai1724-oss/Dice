using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
public enum RollGuessType
{
    Dice1LessThanDice2,
    Dice1GreaterThanDice2,
    Dice1EqualDice2,
}
public class PopupRoll : UIBase
{


    public static PopupRoll Instance;

    [Header("Guess")]
    public Button buttonLessThanThree;
    public Button buttonGreaterThanThree;
    public Button buttonEqualThree;
    public GameObject guessRoot;

    [Header("Reward")]
    public GameObject rewardRoot;
    public Button buttonAtkReward;
    public Button buttonCritReward;
    public Button buttonHpReward;
    [Header("Fail")]
    public GameObject failRoot;


    [Header("Text")]
    public TextMeshProUGUI txtTitle;
    public TextMeshProUGUI txtResult;

    [Header("Roll")]
    public float closeDelayOnMiss = 0.55f;
    public float resultResolveDelay = 0.2f;
    public List<RollItem> rollItems;
    public RollItem rollItemPrefab;
    public RectTransform rollItemParent;
    public DataRollUI dataRollUI;
    public DiceRoll dice1;
    public DiceRoll dice2;
    public ItemPreviewGenerator previewGenerator;
    public RollItem rollItemChoosed;
    [Header("Result")]
    public Image iconDie1Result;
    public Image iconDie2Result;
    public Image typeIconResult;

    Sprite ownedDice1IconSprite;
    Sprite ownedDice2IconSprite;
    Texture2D ownedDice1IconTexture;
    Texture2D ownedDice2IconTexture;


    RollGuessType? selectedGuess;
    bool rollResolved;
    bool rewardGranted;
    Coroutine rollRoutine;
    readonly Dictionary<int, int> diceResults = new Dictionary<int, int>();

    void Awake()
    {
        Instance = this;
        BindButtons();
        rollItemChoosed.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        DiceRoll.OnDiceResult += OnDiceResultReceived;
    }

    void OnDisable()
    {
        DiceRoll.OnDiceResult -= OnDiceResultReceived;
    }
    public void SetupRollItems()
    {
        if (rollItems == null)
            rollItems = new List<RollItem>();

        foreach (RollItem rollItem in rollItems)
        {
            if (rollItem != null)
                Destroy(rollItem.gameObject);
        }

        rollItems.Clear();

        if (rollItemPrefab == null || rollItemParent == null || dataRollUI == null || dataRollUI.rollItemDatas == null)
            return;

        if (rollItemPrefab.transform.IsChildOf(rollItemParent))
            rollItemPrefab.gameObject.SetActive(false);

        ReleaseCapturedDiceIcons();
        ownedDice1IconSprite = CaptureDiceIcon(dice1, out ownedDice1IconTexture);
        ownedDice2IconSprite = CaptureDiceIcon(dice2, out ownedDice2IconTexture);

        CreateRollItem(RollGuessType.Dice1LessThanDice2, ownedDice1IconSprite, ownedDice2IconSprite);
        CreateRollItem(RollGuessType.Dice1GreaterThanDice2, ownedDice1IconSprite, ownedDice2IconSprite);
        CreateRollItem(RollGuessType.Dice1EqualDice2, ownedDice1IconSprite, ownedDice2IconSprite);
    }

    void CreateRollItem(RollGuessType guessType, Sprite dice1IconSprite, Sprite dice2IconSprite)
    {
        if (!dataRollUI.rollItemDatas.TryGetValue(guessType, out RollItemData rollItemData) || rollItemData == null)
            return;

        RollItem rollItem = Instantiate(rollItemPrefab, rollItemParent);
        rollItem.gameObject.SetActive(true);

        RollGuessType selectedGuessType = guessType;
        rollItem.SetupRollItem(
            rollItemData.bgSprite,
            rollItemData.headerSprite,
            dice1IconSprite != null ? dice1IconSprite : rollItemData.iconDie1Sprite,
            dice2IconSprite != null ? dice2IconSprite : rollItemData.iconDie2Sprite,
            rollItemData.typeIconSprite,
            selectedGuessType,
            () => OnChooseGuess(selectedGuessType));

        rollItems.Add(rollItem);

    }

    RollItemData GetRollItemData(RollGuessType guessType)
    {
        if (dataRollUI == null || dataRollUI.rollItemDatas == null)
            return null;

        dataRollUI.rollItemDatas.TryGetValue(guessType, out RollItemData rollItemData);
        return rollItemData;
    }

    Sprite GetDice1IconSprite(RollItemData fallbackData)
    {
        return ownedDice1IconSprite != null ? ownedDice1IconSprite : fallbackData?.iconDie1Sprite;
    }

    Sprite GetDice2IconSprite(RollItemData fallbackData)
    {
        return ownedDice2IconSprite != null ? ownedDice2IconSprite : fallbackData?.iconDie2Sprite;
    }

    RollGuessType GetResultGuessType(int dice1Value, int dice2Value)
    {
        if (dice1Value < dice2Value)
            return RollGuessType.Dice1LessThanDice2;

        if (dice1Value > dice2Value)
            return RollGuessType.Dice1GreaterThanDice2;

        return RollGuessType.Dice1EqualDice2;
    }

    void ShowRollItemChoosed(RollGuessType guessType)
    {
        if (rollItemChoosed == null)
            return;

        RollItemData rollItemData = GetRollItemData(guessType);
        if (rollItemData == null)
            return;

        rollItemChoosed.gameObject.SetActive(true);
        rollItemChoosed.SetupRollItem(
            rollItemData.bgSprite,
            rollItemData.headerSprite,
            GetDice1IconSprite(rollItemData),
            GetDice2IconSprite(rollItemData),
            rollItemData.typeIconSprite,
            guessType,
            null);
        rollItemChoosed.SetInteractable(false);
    }

    void ShowRollResult(int dice1Value, int dice2Value)
    {
        RollGuessType resultGuessType = GetResultGuessType(dice1Value, dice2Value);
        RollItemData rollItemData = GetRollItemData(resultGuessType);

        SetResultImage(iconDie1Result, GetDice1IconSprite(rollItemData));
        SetResultImage(iconDie2Result, GetDice2IconSprite(rollItemData));
        SetResultImage(typeIconResult, rollItemData?.typeIconSprite);
    }

    void SetResultImage(Image image, Sprite sprite)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.enabled = sprite != null;
    }

    void ClearRollDisplays()
    {
        if (rollItemChoosed != null)
            rollItemChoosed.gameObject.SetActive(false);

        SetResultImage(iconDie1Result, null);
        SetResultImage(iconDie2Result, null);
        SetResultImage(typeIconResult, null);
    }

    Sprite CaptureDiceIcon(DiceRoll dicePrefab, out Texture2D ownedTexture)
    {
        ownedTexture = null;

        if (dicePrefab == null)
            return null;

        if (previewGenerator == null)
            previewGenerator = FindFirstObjectByType<ItemPreviewGenerator>(FindObjectsInactive.Include);

        if (previewGenerator == null)
            return null;

        ownedTexture = previewGenerator.Capture(dicePrefab);
        if (ownedTexture == null)
            return null;

        return Sprite.Create(
            ownedTexture,
            new Rect(0f, 0f, ownedTexture.width, ownedTexture.height),
            new Vector2(0.5f, 0.5f),
            100f);
    }

    void ReleaseCapturedDiceIcons()
    {
        if (ownedDice1IconSprite != null)
            Destroy(ownedDice1IconSprite);
        if (ownedDice2IconSprite != null)
            Destroy(ownedDice2IconSprite);
        if (ownedDice1IconTexture != null)
            Destroy(ownedDice1IconTexture);
        if (ownedDice2IconTexture != null)
            Destroy(ownedDice2IconTexture);

        ownedDice1IconSprite = null;
        ownedDice2IconSprite = null;
        ownedDice1IconTexture = null;
        ownedDice2IconTexture = null;
    }

    void OnDestroy()
    {
        ReleaseCapturedDiceIcons();
    }
    void BindButtons()
    {
        if (buttonLessThanThree != null)
        {
            buttonLessThanThree.onClick.RemoveAllListeners();
            buttonLessThanThree.onClick.AddListener(() => OnChooseGuess(RollGuessType.Dice1LessThanDice2));
        }

        if (buttonGreaterThanThree != null)
        {
            buttonGreaterThanThree.onClick.RemoveAllListeners();
            buttonGreaterThanThree.onClick.AddListener(() => OnChooseGuess(RollGuessType.Dice1GreaterThanDice2));
        }

        if (buttonEqualThree != null)
        {
            buttonEqualThree.onClick.RemoveAllListeners();
            buttonEqualThree.onClick.AddListener(() => OnChooseGuess(RollGuessType.Dice1EqualDice2));
        }

        if (buttonAtkReward != null)
        {
            buttonAtkReward.onClick.RemoveAllListeners();
            buttonAtkReward.onClick.AddListener(() => OnChooseReward(HeroStatType.Damage, 20f, "RollAtk20"));
        }

        if (buttonCritReward != null)
        {
            buttonCritReward.onClick.RemoveAllListeners();
            buttonCritReward.onClick.AddListener(() => OnChooseReward(HeroStatType.CritRate, 10f, "RollCrit10"));
        }

        if (buttonHpReward != null)
        {
            buttonHpReward.onClick.RemoveAllListeners();
            buttonHpReward.onClick.AddListener(() => OnChooseReward(HeroStatType.Hp, 30f, "RollHp30"));
        }
    }

    public override void Show()
    {
        base.Show();

        if (rollRoutine != null)
        {
            StopCoroutine(rollRoutine);
            rollRoutine = null;
        }

        selectedGuess = null;
        rollResolved = false;
        rewardGranted = false;
        diceResults.Clear();

        // if (txtTitle != null)
        //     txtTitle.text = "Roll Guess";

        // if (txtResult != null)
        //     txtResult.text = "Choose Dice 1 < Dice 2, >, or =, then roll the dice.";

        if (rewardRoot != null)
            rewardRoot.SetActive(false);
        if (failRoot != null)
            failRoot.SetActive(false);
        if (guessRoot != null)
            guessRoot.SetActive(true);
        rollItemChoosed.gameObject.SetActive(false);

        SetupRollItems();
        ClearRollDisplays();
        SetGuessButtonsInteractable(true);
        SetRewardButtonsInteractable(false);
    }

    void OnChooseGuess(RollGuessType guess)
    {
        if (rollResolved || rollRoutine != null)
            return;

        selectedGuess = guess;
        diceResults.Clear();
        ShowRollItemChoosed(guess);
        SetGuessButtonsInteractable(false);

        // if (txtResult != null)
        //     txtResult.text = "Rolling 2 dice...";
        if (guessRoot != null)
            guessRoot.SetActive(false);
        rollItemChoosed.gameObject.SetActive(true);
        TigerForge.EventManager.EmitEvent(Constant.EVENT_ROLL_DICE);
    }

    bool EvaluateGuess(RollGuessType guess, int dice1, int dice2)
    {
        return guess switch
        {
            RollGuessType.Dice1LessThanDice2 => dice1 < dice2,
            RollGuessType.Dice1GreaterThanDice2 => dice1 > dice2,
            RollGuessType.Dice1EqualDice2 => dice1 == dice2,
            _ => false,
        };
    }

    void OnChooseReward(HeroStatType statType, float percentValue, string keyLocal)
    {
        if (rewardGranted)
            return;

        rewardGranted = true;
        Debug.Log($"[PopupRoll] Reward chosen stat={statType} value={percentValue} key={keyLocal}");
        ApplyTemporaryChapterStat(statType, percentValue, keyLocal);

        SetRewardButtonsInteractable(false);
        Hide();
    }

    void ApplyTemporaryChapterStat(HeroStatType statType, float percentValue, string keyLocal)
    {
        ChapterDiceSession session = ChapterDiceSession.GetOrCreate();
        PlayerController player = FindFirstObjectByType<PlayerController>();
        int oldCurrentHp = GetCurrentHpForStatChange(player, session);

        PlayerStats.Shared.AddTemporaryChapterStat(statType, percentValue, keyLocal, false);

        if (player != null)
            player.RefreshStatsFromEquipment();

        if (statType != HeroStatType.Hp)
            return;

        int scaledCurrentHp = Mathf.RoundToInt(oldCurrentHp * Mathf.Max(0f, 1f + percentValue / 100f));
        if (oldCurrentHp > 0)
            scaledCurrentHp = Mathf.Max(1, scaledCurrentHp);

        session.SetCurrentHp(scaledCurrentHp);

        if (player != null)
            player.SetHealth(player.hp, scaledCurrentHp);
    }

    int GetCurrentHpForStatChange(PlayerController player, ChapterDiceSession session)
    {
        if (player != null)
            return player.currentHp;

        if (session != null && session.TryGetCurrentHp(out int savedCurrentHp))
            return savedCurrentHp;

        int currentMaxHp = Mathf.RoundToInt(PlayerStats.Shared.GetStatValue(HeroStatType.Hp));
        if (currentMaxHp > 0)
            return currentMaxHp;

        HeroData heroData = session != null ? session.ResolveHeroData() : null;
        return heroData != null ? heroData.hp : 0;
    }
    void SetGuessButtonsInteractable(bool interactable)
    {
        if (buttonLessThanThree != null)
            buttonLessThanThree.interactable = interactable;
        if (buttonGreaterThanThree != null)
            buttonGreaterThanThree.interactable = interactable;
        if (buttonEqualThree != null)
            buttonEqualThree.interactable = interactable;

        if (rollItems == null)
            return;

        foreach (RollItem rollItem in rollItems)
        {
            if (rollItem != null)
                rollItem.SetInteractable(interactable);
        }
    }

    void SetRewardButtonsInteractable(bool interactable)
    {
        if (buttonAtkReward != null)
            buttonAtkReward.interactable = interactable;
        if (buttonCritReward != null)
            buttonCritReward.interactable = interactable;
        if (buttonHpReward != null)
            buttonHpReward.interactable = interactable;
    }

    public void RewriteFace()
    {
        if (!selectedGuess.HasValue)
            return;

        RewriteFaces(Random.Range(1, 7), Random.Range(1, 7));
    }

    public void RewriteDice1Face(int face)
    {
        RewriteFace(0, face);
    }

    public void RewriteDice2Face(int face)
    {
        RewriteFace(1, face);
    }

    public void RewriteFace(int diceIndex, int face)
    {
        if (!selectedGuess.HasValue)
            return;

        diceResults[diceIndex] = Mathf.Clamp(face, 1, 6);
        TryResolveCurrentDiceResults();
    }

    public void RewriteFaces(int dice1Face, int dice2Face)
    {
        if (!selectedGuess.HasValue)
            return;

        diceResults[0] = Mathf.Clamp(dice1Face, 1, 6);
        diceResults[1] = Mathf.Clamp(dice2Face, 1, 6);
        TryResolveCurrentDiceResults();
    }
    void OnDiceResultReceived(int diceIndex, int roll)
    {
        if (!selectedGuess.HasValue || rollResolved)
            return;

        diceResults[diceIndex] = roll;
        TryResolveCurrentDiceResults();
    }
    void TryResolveCurrentDiceResults()
    {
        if (!selectedGuess.HasValue)
            return;

        if (diceResults.Count < 2 || !diceResults.ContainsKey(0) || !diceResults.ContainsKey(1))
            return;

        int dice1 = diceResults[0];
        int dice2 = diceResults[1];
        bool isWin = EvaluateGuess(selectedGuess.Value, dice1, dice2);
        rollResolved = true;
        ShowRollResult(dice1, dice2);

        // if (txtResult != null)
        //     txtResult.text = $"Dice 1: {dice1} | Dice 2: {dice2}. " + (isWin ? "Correct! Pick a reward." : "Wrong guess.");

        if (rollRoutine != null)
        {
            StopCoroutine(rollRoutine);
            rollRoutine = null;
        }

        TigerForge.EventManager.EmitEvent(Constant.EVENT_ON_ROLL_RESULT);
        rollRoutine = StartCoroutine(ResolveRollResult(isWin));
    }
    IEnumerator ResolveRollResult(bool isWin)
    {
        yield return new WaitForSecondsRealtime(resultResolveDelay);
        UiHome.Instance?.ShowRollPlane(false);

        if (isWin)
        {
            if (failRoot != null)
                failRoot.SetActive(false);

            if (rewardRoot != null)
                rewardRoot.SetActive(true);

            SetRewardButtonsInteractable(true);
            rollRoutine = null;
            yield break;
        }
        if (failRoot != null)
            failRoot.SetActive(true);

        if (rewardRoot != null)
            rewardRoot.SetActive(false);
        yield return new WaitForSecondsRealtime(closeDelayOnMiss);
        //  Hide();
        rollRoutine = null;
    }

    public override void AfterHideAction()
    {
        rollRoutine = null;
        diceResults.Clear();
        UiHome.Instance?.ShowRollPlane(false);
        GameManager.Instance.CompleteCurrentSpecialLevel(LevelType.Roll);
    }
    public void Refuse()
    {
        if (rollResolved || rollRoutine != null)
            return;

        Hide();
    }
}







