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

    [Header("Roll Visual")]
    public RollDiceVisual rollDicePrefab;
    public Transform rollBoardAnchor;
    public Vector3 rollSpawnOffset = new Vector3(0f, 7f, 0f);
    public float dropSpeed = 18f;
    public Vector3 rollTorqueMin = new Vector3(-28f, -18f, -28f);
    public Vector3 rollTorqueMax = new Vector3(28f, 18f, 28f);
    public float settleVelocityThreshold = 0.2f;
    public float settleAngularThreshold = 0.28f;
    public float settleHoldDuration = 0.45f;
    public float maxRollDuration = 4f;

    [Header("Roll")]
    public float resultDelay = 0.05f;
    public float closeDelayOnMiss = 0.8f;

    RollGuessType? selectedGuess;
    bool rollResolved;
    bool rewardGranted;
    bool shouldCompleteRoomOnHide;
    Coroutine rollRoutine;
    RollDiceVisual activeRollVisual;

    void Awake()
    {
        Instance = this;
        BindButtons();
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
        shouldCompleteRoomOnHide = false;
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

        rollRoutine = StartCoroutine(IERollAndReveal());
    }

    IEnumerator IERollAndReveal()
    {
        UiHome.Instance?.ShowRollPlane(true);
        rollBoardAnchor = UiHome.Instance?.rollPlaneAnchor;
        SpawnRollVisual();
        yield return new WaitForSecondsRealtime(resultDelay);

        int roll = 1;
        if (activeRollVisual != null)
            yield return StartCoroutine(WaitForRollToSettle(activeRollVisual, value => roll = value));

        bool isWin = EvaluateGuess(selectedGuess.GetValueOrDefault(), roll);
        rollResolved = true;

        if (txtResult != null)
            txtResult.text = $"Dice rolled {roll}. " + (isWin ? "Correct! Pick a reward." : "Wrong guess.");

        if (isWin)
        {
            if (rewardRoot != null)
                rewardRoot.SetActive(true);

            SetRewardButtonsInteractable(true);
            rollRoutine = null;
            yield break;
        }

        yield return new WaitForSecondsRealtime(closeDelayOnMiss);
        rollRoutine = null;
    }

    void SpawnRollVisual()
    {
        ClearRollVisual();

        if (rollDicePrefab == null || rollBoardAnchor == null)
            return;

        Vector3 spawnPosition = rollBoardAnchor.position + rollSpawnOffset;
        activeRollVisual = Instantiate(rollDicePrefab, spawnPosition, Random.rotation);

        Vector3 dropForce = Vector3.down * dropSpeed;
        activeRollVisual.Roll(dropForce, GetRandomTorque());
    }

    IEnumerator WaitForRollToSettle(RollDiceVisual visual, System.Action<int> onResolved)
    {
        if (visual == null || visual.rb == null)
        {
            onResolved?.Invoke(1);
            yield break;
        }

        float elapsed = 0f;
        float stableTime = 0f;

        while (elapsed < maxRollDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            bool stable = visual.rb.linearVelocity.sqrMagnitude <= settleVelocityThreshold * settleVelocityThreshold &&
                          visual.rb.angularVelocity.sqrMagnitude <= settleAngularThreshold * settleAngularThreshold;

            if (stable)
            {
                stableTime += Time.unscaledDeltaTime;
                if (stableTime >= settleHoldDuration)
                    break;
            }
            else
            {
                stableTime = 0f;
            }

            yield return null;
        }

        onResolved?.Invoke(visual.GetTopFace());
        yield return new WaitForSecondsRealtime(1f);
        ClearRollVisual();
        UiHome.Instance?.ShowRollPlane(false);
    }

    Vector3 GetRandomTorque()
    {
        return new Vector3(
            Random.Range(rollTorqueMin.x, rollTorqueMax.x),
            Random.Range(rollTorqueMin.y, rollTorqueMax.y),
            Random.Range(rollTorqueMin.z, rollTorqueMax.z)
        );
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
        shouldCompleteRoomOnHide = true;
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
        if (activeRollVisual == null)
            return;

        Destroy(activeRollVisual.gameObject);
        activeRollVisual = null;
    }

    public override void AfterHideAction()
    {
        rollRoutine = null;
        ClearRollVisual();
        GameManager.Instance.CompleteCurrentSpecialLevel(LevelType.Roll);
    }
}
