
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupRoll : UIBase
{
    public enum RollGuessType
    {
        LessThanThree,
        GreaterThanThree,
        EqualThree,
    }

    public static PopupRoll Instance;

    [Header("Guess")]
    public Button buttonLessThanThree;
    public Button buttonGreaterThanThree;
    public Button buttonEqualThree;

    [Header("Reward")]
    public GameObject rewardRoot;
    public Button buttonAtkReward;
    public Button buttonCritReward;
    public Button buttonHpReward;

    [Header("Text")]
    public TextMeshProUGUI txtTitle;
    public TextMeshProUGUI txtResult;


    RollGuessType? selectedGuess;
    bool rollResolved;
    bool rewardGranted;
    Coroutine rollRoutine;

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
        if (buttonLessThanThree != null)
        {
            buttonLessThanThree.onClick.RemoveAllListeners();
            buttonLessThanThree.onClick.AddListener(() => OnChooseGuess(RollGuessType.LessThanThree));
        }

        if (buttonGreaterThanThree != null)
        {
            buttonGreaterThanThree.onClick.RemoveAllListeners();
            buttonGreaterThanThree.onClick.AddListener(() => OnChooseGuess(RollGuessType.GreaterThanThree));
        }

        if (buttonEqualThree != null)
        {
            buttonEqualThree.onClick.RemoveAllListeners();
            buttonEqualThree.onClick.AddListener(() => OnChooseGuess(RollGuessType.EqualThree));
        }

        if (buttonAtkReward != null)
        {
            buttonAtkReward.onClick.RemoveAllListeners();
            buttonAtkReward.onClick.AddListener(() => OnChooseReward(HeroStatType.Damage, 0.2f, "RollAtk20"));
        }

        if (buttonCritReward != null)
        {
            buttonCritReward.onClick.RemoveAllListeners();
            buttonCritReward.onClick.AddListener(() => OnChooseReward(HeroStatType.CritRate, 0.1f, "RollCrit10"));
        }

        if (buttonHpReward != null)
        {
            buttonHpReward.onClick.RemoveAllListeners();
            buttonHpReward.onClick.AddListener(() => OnChooseReward(HeroStatType.Hp, 0.3f, "RollHp30"));
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
        ClearRollVisual();

        if (txtTitle != null)
            txtTitle.text = "Roll Guess";

        if (txtResult != null)
            txtResult.text = "Choose <3, >3, or =3, then roll the dice.";

        if (rewardRoot != null)
            rewardRoot.SetActive(false);

        SetGuessButtonsInteractable(true);
        SetRewardButtonsInteractable(false);
    }

    void OnChooseGuess(RollGuessType guess)
    {
        if (rollResolved || rollRoutine != null)
            return;

        selectedGuess = guess;
        SetGuessButtonsInteractable(false);

        if (txtResult != null)
            txtResult.text = "Rolling...";
        TigerForge.EventManager.EmitEvent(Constant.EVENT_ROLL_DICE);


    }

    bool EvaluateGuess(RollGuessType guess, int roll)
    {
        return guess switch
        {
            RollGuessType.LessThanThree => roll < 3,
            RollGuessType.GreaterThanThree => roll > 3,
            RollGuessType.EqualThree => roll == 3,
            _ => false,
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

    void SetGuessButtonsInteractable(bool interactable)
    {
        if (buttonLessThanThree != null)
            buttonLessThanThree.interactable = interactable;
        if (buttonGreaterThanThree != null)
            buttonGreaterThanThree.interactable = interactable;
        if (buttonEqualThree != null)
            buttonEqualThree.interactable = interactable;
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

    void ClearRollVisual()
    {
    }

    void OnDiceResultReceived(int diceIndex, int roll)
    {
        if (!selectedGuess.HasValue || rollResolved || diceIndex != 0)
            return;

        bool isWin = EvaluateGuess(selectedGuess.Value, roll);
        rollResolved = true;

        if (txtResult != null)
            txtResult.text = $"Dice rolled {roll}. " + (isWin ? "Correct! Pick a reward." : "Wrong guess.");

        if (rollRoutine != null)
        {
            StopCoroutine(rollRoutine);
            rollRoutine = null;
        }
    }


    public override void AfterHideAction()
    {
        rollRoutine = null;
        ClearRollVisual();
        UiHome.Instance?.ShowRollPlane(false);
        GameManager.Instance.CompleteCurrentSpecialLevel(LevelType.Roll);
    }
}




