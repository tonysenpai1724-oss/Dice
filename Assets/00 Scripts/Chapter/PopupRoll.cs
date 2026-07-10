using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupRoll : UIBase
{
    public enum RollGuessType
    {
        Dice1LessThanDice2,
        Dice1GreaterThanDice2,
        Dice1EqualDice2,
    }

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

    RollGuessType? selectedGuess;
    bool rollResolved;
    bool rewardGranted;
    Coroutine rollRoutine;
    readonly Dictionary<int, int> diceResults = new Dictionary<int, int>();

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

        if (txtTitle != null)
            txtTitle.text = "Roll Guess";

        if (txtResult != null)
            txtResult.text = "Choose Dice 1 < Dice 2, >, or =, then roll the dice.";

        if (rewardRoot != null)
            rewardRoot.SetActive(false);
        if (failRoot != null)
            failRoot.SetActive(false);
        if (guessRoot != null)
            guessRoot.SetActive(true);

        SetGuessButtonsInteractable(true);
        SetRewardButtonsInteractable(false);
    }

    void OnChooseGuess(RollGuessType guess)
    {
        if (rollResolved || rollRoutine != null)
            return;

        selectedGuess = guess;
        diceResults.Clear();
        SetGuessButtonsInteractable(false);

        if (txtResult != null)
            txtResult.text = "Rolling 2 dice...";
        if (guessRoot != null)
            guessRoot.SetActive(false);

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
        PlayerStats.Shared.AddTemporaryChapterStat(statType, percentValue, keyLocal, false);

        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
            player.RefreshStatsFromEquipment();

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

    void OnDiceResultReceived(int diceIndex, int roll)
    {
        if (!selectedGuess.HasValue || rollResolved)
            return;

        diceResults[diceIndex] = roll;
        if (diceResults.Count < 2 || !diceResults.ContainsKey(0) || !diceResults.ContainsKey(1))
            return;

        int dice1 = diceResults[0];
        int dice2 = diceResults[1];
        bool isWin = EvaluateGuess(selectedGuess.Value, dice1, dice2);
        rollResolved = true;

        if (txtResult != null)
            txtResult.text = $"Dice 1: {dice1} | Dice 2: {dice2}. " + (isWin ? "Correct! Pick a reward." : "Wrong guess.");

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
}







