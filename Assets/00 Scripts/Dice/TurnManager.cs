using TMPro;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    [Header("Turn")]
    public int maxTurnBeforeReset = 10;
    public int turnsUntilReset;

    [Header("UI")]
    public TextMeshProUGUI resetText;

    [Header("Refs")]
    public DiceQueue diceQueue;

    public bool IsResettingBoard { get; private set; }
    public bool IsResetPending { get; private set; }

    void Awake()
    {
        Instance = this;
        ResetTurnCounter();
    }

    void Start()
    {
        UpdateUI();
    }

    public void AddTurn()
    {
        turnsUntilReset--;

        if (turnsUntilReset <= 0)
        {
            IsResetPending = true;
            UpdateUI();
            return;
        }

        UpdateUI();
    }

    public void ResetBoardAfterQueue()
    {
        if (IsResettingBoard || !IsResetPending)
            return;

        IsResettingBoard = true;
        IsResetPending = false;

        ResetBoard();
        ResetTurnCounter();
        UpdateUI();

        IsResettingBoard = false;
    }

    void UpdateUI()
    {
        resetText.text =
             turnsUntilReset.ToString();
    }

    void ResetBoard()
    {
        DiceManager.Instance.ResetBoard();
    }

    void ResetTurnCounter()
    {
        turnsUntilReset =
            maxTurnBeforeReset;
    }
}
