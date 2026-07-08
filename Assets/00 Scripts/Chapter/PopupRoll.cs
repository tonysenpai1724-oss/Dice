
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
    public Vector3 rollSpawnOffset = new Vector3(0f, 0f, 0f);
    public Vector3 rollDirectionMin = new Vector3(-0.8f, 0f, 0.35f);
    public Vector3 rollDirectionMax = new Vector3(0.8f, 0f, 1f);
    public float rollResultDelay = 1.5f;
    public float visualCleanupDelay = 0.25f;
    public bool forceRuntimeTuning = true;

    [Header("Roll")]
    public float resultDelay = 0.03f;
    public float closeDelayOnMiss = 0.55f;

    RollGuessType? selectedGuess;
    bool rollResolved;
    bool rewardGranted;
    Coroutine rollRoutine;
    RollDiceVisual activeRollVisual;

    void Awake()
    {
        Instance = this;
        if (forceRuntimeTuning)
            ApplyRecommendedTuning();
        BindButtons();
    }

    [Sirenix.OdinInspector.Button("Apply Recommended Tuning")]
    public void ApplyRecommendedTuning()
    {
        rollSpawnOffset = Vector3.zero;
        rollDirectionMin = new Vector3(-0.8f, 0f, 0.35f);
        rollDirectionMax = new Vector3(0.8f, 0f, 1f);
        rollResultDelay = 1.5f;
        visualCleanupDelay = 0.25f;
        resultDelay = 0.03f;
        closeDelayOnMiss = 0.55f;
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

        if (forceRuntimeTuning)
            ApplyRecommendedTuning();

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

        rollRoutine = StartCoroutine(IERollAndReveal());
    }

    IEnumerator IERollAndReveal()
    {
        UiHome.Instance?.ShowRollPlane(true);
        rollBoardAnchor = UiHome.Instance != null ? UiHome.Instance.rollPlaneAnchor : rollBoardAnchor;
        int resolvedFace = Random.Range(1, 7);
        SpawnRollVisual(resolvedFace);
        yield return new WaitForSecondsRealtime(resultDelay);

        bool animationFinished = activeRollVisual == null;
        System.Action onRollVisualFinished = () => animationFinished = true;

        if (activeRollVisual != null)
        {
            activeRollVisual.ClearFinishedListeners();
            activeRollVisual.Finished += onRollVisualFinished;
            yield return new WaitUntil(() => animationFinished || activeRollVisual == null);

            if (activeRollVisual != null)
            {
                activeRollVisual.Finished -= onRollVisualFinished;
            }
        }
        else
        {
            yield return new WaitForSecondsRealtime(rollResultDelay);
        }

        int roll = activeRollVisual != null ? activeRollVisual.CurrentFace : resolvedFace;
        bool isWin = EvaluateGuess(selectedGuess.GetValueOrDefault(), roll);
        rollResolved = true;

        if (txtResult != null)
            txtResult.text = $"Dice rolled {roll}. " + (isWin ? "Correct! Pick a reward." : "Wrong guess.");

        yield return new WaitForSecondsRealtime(visualCleanupDelay);
        ClearRollVisual();
        UiHome.Instance?.ShowRollPlane(false);

        if (isWin)
        {
            if (rewardRoot != null)
                rewardRoot.SetActive(true);

            SetRewardButtonsInteractable(true);
            rollRoutine = null;
            yield break;
        }

        yield return new WaitForSecondsRealtime(closeDelayOnMiss);
        Hide();
        rollRoutine = null;
    }

    void SpawnRollVisual(int targetFace)
    {
        ClearRollVisual();

        if (rollDicePrefab == null || rollBoardAnchor == null)
            return;

        Vector3 spawnPosition = rollBoardAnchor.position + rollSpawnOffset;
        Vector3 rollDirection = GetRollDirection();
        activeRollVisual = Instantiate(rollDicePrefab, spawnPosition, Quaternion.identity, rollBoardAnchor);
        activeRollVisual.SpawnAndRoll(spawnPosition, rollDirection, targetFace);
    }

    Vector3 GetRollDirection()
    {
        Vector3 direction = new Vector3(
            Random.Range(rollDirectionMin.x, rollDirectionMax.x),
            0f,
            Random.Range(rollDirectionMin.z, rollDirectionMax.z)
        );

        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector3.forward;

        return direction.normalized;
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
        if (activeRollVisual == null)
            return;

        Destroy(activeRollVisual.gameObject);
        activeRollVisual = null;
    }

    public override void AfterHideAction()
    {
        rollRoutine = null;
        ClearRollVisual();
        UiHome.Instance?.ShowRollPlane(false);
        GameManager.Instance.CompleteCurrentSpecialLevel(LevelType.Roll);
    }
}


