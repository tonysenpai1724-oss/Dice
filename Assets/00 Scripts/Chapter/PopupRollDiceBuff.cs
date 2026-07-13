using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupRollDiceBuff : UIBase
{
    public enum RollDiceType
    {
        Dice8,
        Dice12,
        Dice20,
    }

    public static PopupRollDiceBuff Instance;

    [Header("Choose Dice")]
    public Button buttonDice8;
    public Button buttonDice12;
    public Button buttonDice20;
    public GameObject chooseRoot;

    [Header("Reward")]
    public GameObject rewardRoot;

    [Header("Fail")]
    public GameObject failRoot;

    [Header("Text")]
    public TextMeshProUGUI txtTitle;
    public TextMeshProUGUI txtResult;

    [Header("Roll")]
    public float resultResolveDelay = 0.2f;

    RollDiceType? selectedDiceType;
    bool rollResolved;
    bool rewardGranted;
    Coroutine rollRoutine;
    int finalRoll;

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
        rollResolved = false;
        rewardGranted = false;
        finalRoll = 0;

        if (txtTitle != null)
            txtTitle.text = "Roll Dice Buff";

        if (txtResult != null)
            txtResult.text = "Choose a risk reward. Roll max to apply its buff.";

        if (rewardRoot != null)
            rewardRoot.SetActive(false);
        if (chooseRoot != null)
            chooseRoot.SetActive(true);
        if (failRoot != null)
            failRoot.SetActive(false);

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

        if (txtResult != null)
            txtResult.text = "Rolling selected dice...";
        if (chooseRoot != null)
            chooseRoot.SetActive(false);
        if (failRoot != null)
            failRoot.SetActive(false);

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

    void ApplyRollDiceBuff(RollDiceType diceType)
    {
        if (rewardGranted)
            return;

        rewardGranted = true;
        float hpPenaltyPercent = GetHpPenaltyPercent(diceType);
        float damageBonusPercent = GetDamageBonusPercent(diceType);
        string keyPrefix = $"RollDiceBuff{diceType}";

        ChapterDiceSession session = ChapterDiceSession.GetOrCreate();
        PlayerController player = FindFirstObjectByType<PlayerController>();
        int oldCurrentHp = GetCurrentHpForStatChange(player, session);

        PlayerStats.Shared.AddTemporaryChapterStat(HeroStatType.Hp, hpPenaltyPercent, $"{keyPrefix}Hp", false);
        PlayerStats.Shared.AddTemporaryChapterStat(HeroStatType.Damage, damageBonusPercent, $"{keyPrefix}Atk", false);

        if (player != null)
            player.RefreshStatsFromEquipment();

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

        if (txtResult != null)
            txtResult.text = isWin
                ? $"Result: {finalRoll}. Max roll! {GetRewardText(selectedDiceType.Value)}"
                : $"Result: {finalRoll}. Not max roll.";

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
            if (selectedDiceType.HasValue)
                ApplyRollDiceBuff(selectedDiceType.Value);

            if (rewardRoot != null)
                rewardRoot.SetActive(true);

            SetRewardButtonsInteractable(false);
            rollRoutine = null;
            Hide();
            yield break;
        }
        else
        {
            if (failRoot != null)
                failRoot.SetActive(true);

            if (rewardRoot != null)
                rewardRoot.SetActive(false);

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


