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

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        ResetTurnCounter();
        UpdateUI();
    }

    public void AddTurn()
    {
        turnsUntilReset--;

        if (turnsUntilReset <= 0)
        {
            ResetBoard();
            ResetTurnCounter();
        }

        UpdateUI();
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
