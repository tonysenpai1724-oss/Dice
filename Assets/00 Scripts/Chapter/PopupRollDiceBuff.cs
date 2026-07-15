using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public enum RollDiceType
{
    Dice8,
    Dice12,
    Dice20,
}

public class PopupRollDiceBuff : UIBase
{

    public static PopupRollDiceBuff Instance;
    public DataRollUI dataRollUI;
    [SerializeField] private DiceRoll dice8Prefab;
    [SerializeField] private DiceRoll dice12Prefab;
    [SerializeField] private DiceRoll dice20Prefab;

    [Header("Choose Dice")]
    public Button buttonDice8;
    public Image iconDice8;
    public Button buttonDice12;
    public Image iconDice12;
    public Button buttonDice20;
    public Image iconDice20;
    public GameObject chooseRoot;

    [Header("Reward")]
    public GameObject rewardRoot;
    public GameObject RewriteButton;
    public GameObject claimBtn;

    // [Header("Text")]
    // public TextMeshProUGUI txtTitle;
    // public TextMeshProUGUI txtResult;

    [Header("Roll")]
    public float resultResolveDelay = 0.2f;
    public Image rollingBg;
    public Image rollingHeader;
    public TextMeshProUGUI rollingHeaderTxt;

    public TextMeshProUGUI rollingTxt;


    [Header("Result")]
    public GameObject resultRoot;
    public Image icon;
    public ItemPreviewGenerator previewGenerator;
    public Image statRewardImage;
    public TextMeshProUGUI statRewardTxt;

    readonly Dictionary<RollDiceType, Sprite> diceIconSprites = new Dictionary<RollDiceType, Sprite>();
    readonly Dictionary<RollDiceType, Texture2D> diceIconTextures = new Dictionary<RollDiceType, Texture2D>();


    RollDiceType? selectedDiceType;
    RollDiceType? pendingRewardDiceType;
    bool rollResolved;
    bool rewardGranted;
    Coroutine rollRoutine;
    int finalRoll;
    float pendingHpRewardPercent;
    float pendingDamageRewardPercent;

    void Awake()
    {
        Instance = this;
        BindButtons();
    }

    void OnEnable()
    {
        DiceRoll.OnDiceResult += OnDiceResultReceived;
    }

    void OnDisable()
    {
        DiceRoll.OnDiceResult -= OnDiceResultReceived;
    }

    void OnDestroy()
    {
        ReleaseCapturedDiceIcons();
    }

    void SetupDiceIcons()
    {
        ReleaseCapturedDiceIcons();

        CaptureDiceIcon(RollDiceType.Dice8, dice8Prefab);
        CaptureDiceIcon(RollDiceType.Dice12, dice12Prefab);
        CaptureDiceIcon(RollDiceType.Dice20, dice20Prefab);

        SetImageSprite(iconDice8, GetDiceIcon(RollDiceType.Dice8));
        SetImageSprite(iconDice12, GetDiceIcon(RollDiceType.Dice12));
        SetImageSprite(iconDice20, GetDiceIcon(RollDiceType.Dice20));
    }

    void CaptureDiceIcon(RollDiceType diceType, DiceRoll dicePrefab)
    {
        if (dicePrefab == null)
        {
            Debug.LogWarning($"[PopupRollDiceBuff] Missing dice prefab for {diceType}.", this);
            return;
        }

        if (previewGenerator == null)
            previewGenerator = FindFirstObjectByType<ItemPreviewGenerator>(FindObjectsInactive.Include);

        if (previewGenerator == null)
        {
            Debug.LogWarning($"[PopupRollDiceBuff] Missing ItemPreviewGenerator for {diceType}.", this);
            return;
        }

        Texture2D texture = previewGenerator.Capture(dicePrefab);
        if (texture == null)
        {
            Debug.LogWarning($"[PopupRollDiceBuff] Failed to capture dice icon for {diceType}.", this);
            return;
        }

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);

        diceIconTextures[diceType] = texture;
        diceIconSprites[diceType] = sprite;
    }

    Sprite GetDiceIcon(RollDiceType diceType)
    {
        if (diceIconSprites.TryGetValue(diceType, out Sprite sprite) && sprite != null)
            return sprite;

        return GetRollItemBuffData(diceType)?.bgIcon;
    }

    RollItemBuffData GetRollItemBuffData(RollDiceType diceType)
    {
        if (dataRollUI == null || dataRollUI.rollItemBuffDatas == null)
            return null;

        dataRollUI.rollItemBuffDatas.TryGetValue(diceType, out RollItemBuffData rollItemData);
        return rollItemData;
    }

    void ReleaseCapturedDiceIcons()
    {
        foreach (Sprite sprite in diceIconSprites.Values)
        {
            if (sprite != null)
                Destroy(sprite);
        }

        foreach (Texture2D texture in diceIconTextures.Values)
        {
            if (texture != null)
                Destroy(texture);
        }

        diceIconSprites.Clear();
        diceIconTextures.Clear();
    }

    void SetImageSprite(Image image, Sprite sprite)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.enabled = sprite != null;
    }

    void ClearResultDisplay()
    {
        SetImageSprite(icon, null);
        SetImageSprite(rollingBg, null);
        SetImageSprite(rollingHeader, null);
        SetImageSprite(statRewardImage, null);

        if (rollingHeaderTxt != null)
            rollingHeaderTxt.text = string.Empty;
        if (rollingTxt != null)
            rollingTxt.text = string.Empty;
        if (statRewardTxt != null)
            statRewardTxt.text = string.Empty;
    }

    void ShowRollingDisplay(RollDiceType diceType, string message)
    {
        RollItemBuffData rollItemData = GetRollItemBuffData(diceType);

        SetImageSprite(icon, GetDiceIcon(diceType));
        SetImageSprite(rollingBg, rollItemData?.bgSprite);
        SetImageSprite(rollingHeader, rollItemData?.headerSprite);

        if (rollingHeaderTxt != null)
            rollingHeaderTxt.text = GetRollingHeaderText(diceType);
        if (rollingTxt != null)
            rollingTxt.text = message;
    }

    string GetRollingHeaderText(RollDiceType diceType)
    {
        return diceType switch
        {
            RollDiceType.Dice8 => "Prudence",
            RollDiceType.Dice12 => "Ambition",
            RollDiceType.Dice20 => "Greed",
            _ => string.Empty,
        };
    }

    void SetupPendingReward(RollDiceType diceType, bool isWin)
    {
        RollItemBuffData rollItemData = GetRollItemBuffData(diceType);

        pendingRewardDiceType = diceType;
        pendingHpRewardPercent = isWin ? GetHpPenaltyPercent(diceType) : 0f;
        pendingDamageRewardPercent = isWin ? GetDamageBonusPercent(diceType) : 5f;

        SetImageSprite(statRewardImage, rollItemData?.bgSprite);

        if (statRewardTxt != null)
            statRewardTxt.text = $"+{pendingDamageRewardPercent:0.#}%";
    }

    void ClaimPendingReward()
    {
        if (!pendingRewardDiceType.HasValue || rewardGranted)
            return;

        ApplyPendingReward();
        Hide();
    }

    void BindButtons()
    {
        if (buttonDice8 != null)
        {
            buttonDice8.onClick.RemoveAllListeners();
            buttonDice8.onClick.AddListener(() => OnChooseDice(RollDiceType.Dice8));
            SetButtonText(buttonDice8, GetRewardText(RollDiceType.Dice8));
        }

        if (buttonDice12 != null)
        {
            buttonDice12.onClick.RemoveAllListeners();
            buttonDice12.onClick.AddListener(() => OnChooseDice(RollDiceType.Dice12));
            SetButtonText(buttonDice12, GetRewardText(RollDiceType.Dice12));
        }

        if (buttonDice20 != null)
        {
            buttonDice20.onClick.RemoveAllListeners();
            buttonDice20.onClick.AddListener(() => OnChooseDice(RollDiceType.Dice20));
            SetButtonText(buttonDice20, GetRewardText(RollDiceType.Dice20));
        }

        Button claimButton = claimBtn != null ? claimBtn.GetComponent<Button>() : null;
        if (claimButton != null)
        {
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(ClaimPendingReward);
        }
    }
    public void Refuse()
    {
        if (rollResolved || rollRoutine != null)
            return;

        Hide();
    }

    public override void Show()
    {
        base.Show();

        if (rollRoutine != null)
        {
            StopCoroutine(rollRoutine);
            rollRoutine = null;
        }

        selectedDiceType = null;
        pendingRewardDiceType = null;
        rollResolved = false;
        rewardGranted = false;
        finalRoll = 0;
        pendingHpRewardPercent = 0f;
        pendingDamageRewardPercent = 0f;

        if (rewardRoot != null)
            rewardRoot.SetActive(false);
        if (chooseRoot != null)
            chooseRoot.SetActive(true);
        if (RewriteButton != null)
            RewriteButton.SetActive(false);
        if (claimBtn != null)
            claimBtn.SetActive(false);
        if (resultRoot != null)
            resultRoot.SetActive(false);

        SetupDiceIcons();
        ClearResultDisplay();
        SetChooseButtonsInteractable(true);
        SetRewardButtonsInteractable(false);
        DiceThrower.CurrentRollMode = DiceThrower.RollMode.TwoDice;
    }

    void OnChooseDice(RollDiceType diceType)
    {
        if (rollResolved || rollRoutine != null)
            return;

        selectedDiceType = diceType;
        SetChooseButtonsInteractable(false);

        DiceThrower.CurrentRollMode = diceType switch
        {
            RollDiceType.Dice8 => DiceThrower.RollMode.Dice8,
            RollDiceType.Dice12 => DiceThrower.RollMode.Dice12,
            RollDiceType.Dice20 => DiceThrower.RollMode.Dice20,
            _ => DiceThrower.RollMode.TwoDice,
        };

        if (chooseRoot != null)
            chooseRoot.SetActive(false);

        ShowRollingDisplay(diceType, "Your fate is rolling...");

        TigerForge.EventManager.EmitEvent(Constant.EVENT_ROLL_DICE);
    }

    int GetMaxRoll(RollDiceType diceType)
    {
        return diceType switch
        {
            RollDiceType.Dice8 => 8,
            RollDiceType.Dice12 => 12,
            RollDiceType.Dice20 => 20,
            _ => 0,
        };
    }

    float GetHpPenaltyPercent(RollDiceType diceType)
    {
        return diceType switch
        {
            RollDiceType.Dice8 => -20f,
            RollDiceType.Dice12 => -30f,
            RollDiceType.Dice20 => -50f,
            _ => 0f,
        };
    }

    float GetDamageBonusPercent(RollDiceType diceType)
    {
        return diceType switch
        {
            RollDiceType.Dice8 => 80f,
            RollDiceType.Dice12 => 120f,
            RollDiceType.Dice20 => 200f,
            _ => 0f,
        };
    }

    string GetRewardText(RollDiceType diceType)
    {
        return diceType switch
        {
            RollDiceType.Dice8 => "Max: -20% Max HP, +80% ATK",
            RollDiceType.Dice12 => "Max: -30% HP, +120% ATK",
            RollDiceType.Dice20 => "Max: -50% Max HP, +200% ATK",
            _ => string.Empty,
        };
    }

    void ApplyPendingReward()
    {
        if (rewardGranted)
            return;

        if (!pendingRewardDiceType.HasValue)
            return;

        rewardGranted = true;
        RollDiceType diceType = pendingRewardDiceType.Value;
        float hpPenaltyPercent = pendingHpRewardPercent;
        float damageBonusPercent = pendingDamageRewardPercent;
        string keyPrefix = $"RollDiceBuff{diceType}";

        ChapterDiceSession session = ChapterDiceSession.GetOrCreate();
        PlayerController player = FindFirstObjectByType<PlayerController>();
        int oldCurrentHp = GetCurrentHpForStatChange(player, session);

        if (!Mathf.Approximately(hpPenaltyPercent, 0f))
            PlayerStats.Shared.AddTemporaryChapterStat(HeroStatType.Hp, hpPenaltyPercent, $"{keyPrefix}Hp", false);

        PlayerStats.Shared.AddTemporaryChapterStat(HeroStatType.Damage, damageBonusPercent, $"{keyPrefix}Atk", false);

        if (player != null)
            player.RefreshStatsFromEquipment();

        if (Mathf.Approximately(hpPenaltyPercent, 0f))
            return;

        int scaledCurrentHp = Mathf.RoundToInt(oldCurrentHp * Mathf.Max(0f, 1f + hpPenaltyPercent / 100f));
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
    void SetButtonText(Button button, string text)
    {
        if (button == null)
            return;

        // TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
        // if (label != null)
        //     label.text = text;
    }

    void SetChooseButtonsInteractable(bool interactable)
    {
        if (buttonDice8 != null)
            buttonDice8.interactable = interactable;
        if (buttonDice12 != null)
            buttonDice12.interactable = interactable;
        if (buttonDice20 != null)
            buttonDice20.interactable = interactable;
    }

    void SetRewardButtonsInteractable(bool interactable)
    {
        Button claimButton = claimBtn != null ? claimBtn.GetComponent<Button>() : null;
        if (claimButton != null)
            claimButton.interactable = interactable;
    }

    public void RewriteFace()
    {
        if (!selectedDiceType.HasValue)
            return;

        RewriteFace(Random.Range(1, GetMaxRoll(selectedDiceType.Value) + 1));
    }

    public void RewriteFace(int face)
    {
        if (!selectedDiceType.HasValue)
            return;

        finalRoll = Mathf.Clamp(face, 1, GetMaxRoll(selectedDiceType.Value));
        TryResolveCurrentRollResult();
    }
    void OnDiceResultReceived(int diceIndex, int roll)
    {
        if (!selectedDiceType.HasValue || rollResolved || diceIndex != 0)
            return;

        finalRoll = roll;
        TryResolveCurrentRollResult();

    }

    void TryResolveCurrentRollResult()
    {
        if (!selectedDiceType.HasValue)
            return;

        rollResolved = true;
        bool isWin = finalRoll == GetMaxRoll(selectedDiceType.Value);
        ShowRollingDisplay(
            selectedDiceType.Value,
            isWin
                ? "Lucky mortal... Enjoy the power you didn't truly earn!"
                : "Your disappointment is delicious!");
        SetupPendingReward(selectedDiceType.Value, isWin);


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
        if (resultRoot != null)
            resultRoot.SetActive(true);
        if (rewardRoot != null)
            rewardRoot.SetActive(true);

        if (isWin)
        {
            if (RewriteButton != null)
                RewriteButton.SetActive(false);
            if (claimBtn != null)
                claimBtn.SetActive(true);

            SetRewardButtonsInteractable(true);
            rollRoutine = null;
            yield break;
        }
        else
        {

            // if (rewardRoot != null)
            //     rewardRoot.SetActive(false);
            if (RewriteButton != null)
                RewriteButton.SetActive(true);
            if (claimBtn != null)
                claimBtn.SetActive(true);
            SetRewardButtonsInteractable(true);

            rollRoutine = null;
            yield break;

        }

    }

    public override void AfterHideAction()
    {
        rollRoutine = null;
        DiceThrower.CurrentRollMode = DiceThrower.RollMode.TwoDice;
        UiHome.Instance?.ShowRollPlane(false);
        GameManager.Instance.CompleteCurrentSpecialLevel(LevelType.Jester);
    }
}
