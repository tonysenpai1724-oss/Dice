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
        }

        if (buttonDice12 != null)
        {
            buttonDice12.onClick.RemoveAllListeners();
            buttonDice12.onClick.AddListener(() => OnChooseDice(RollDiceType.Dice12));
        }

        if (buttonDice20 != null)
        {
            buttonDice20.onClick.RemoveAllListeners();
            buttonDice20.onClick.AddListener(() => OnChooseDice(RollDiceType.Dice20));
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
            txtResult.text = "Choose Dice 8, Dice 12, or Dice 20. Only max roll wins.";

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

    void OnChooseReward(HeroStatType statType, float percentValue, string keyLocal)
    {
        if (rewardGranted)
            return;

        rewardGranted = true;
        PlayerStats.Shared.AddTemporaryChapterStat(statType, percentValue, keyLocal, false);
        SetRewardButtonsInteractable(false);
        Hide();
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

    void OnDiceResultReceived(int diceIndex, int roll)
    {
        if (!selectedDiceType.HasValue || rollResolved || diceIndex != 0)
            return;

        finalRoll = roll;
        rollResolved = true;
        bool isWin = finalRoll == GetMaxRoll(selectedDiceType.Value);

        if (txtResult != null)
            txtResult.text = isWin
                ? $"Result: {finalRoll}. Max roll!"
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
            if (rewardRoot != null)
                rewardRoot.SetActive(true);

            SetRewardButtonsInteractable(true);
            rollRoutine = null;
            yield break;
        }
        else
        {
            if (failRoot != null)
                failRoot.SetActive(true);

            if (rewardRoot != null)
                rewardRoot.SetActive(false);
            yield break;

        }

        rollRoutine = null;
    }

    public override void AfterHideAction()
    {
        rollRoutine = null;
        DiceThrower.CurrentRollMode = DiceThrower.RollMode.TwoDice;
        UiHome.Instance?.ShowRollPlane(false);
        GameManager.Instance.CompleteCurrentSpecialLevel(LevelType.Jester);
    }
}
