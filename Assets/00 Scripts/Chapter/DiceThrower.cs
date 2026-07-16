using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class DiceThrower : MonoBehaviour
{
    public enum RollMode
    {
        TwoDice,
        Dice8,
        Dice12,
        Dice20,
    }

    public static RollMode CurrentRollMode = RollMode.TwoDice;

    [Header("References")]
    [SerializeField] private DiceRoll dicePrefab;
    [SerializeField] private DiceRoll dicePrefab2;
    [SerializeField] private DiceRoll dice8Prefab;
    [SerializeField] private DiceRoll dice12Prefab;
    [SerializeField] private DiceRoll dice20Prefab;

    [Header("Physics Settings")]
    [SerializeField] private float throwForce = 5f;
    [SerializeField] private float rollForce = 10f;
    [SerializeField] private Vector3 dice1Offset = new Vector3(-0.2f, 0f, 0f);
    [SerializeField] private Vector3 dice2Offset = new Vector3(0.2f, 0f, 0f);
    [SerializeField] private float wallDelay = 1f;

    readonly List<GameObject> spawnedDice = new List<GameObject>();

    void Start()
    {
        TigerForge.EventManager.StartListening(Constant.EVENT_ROLL_DICE, RollAllDice);
        TigerForge.EventManager.StartListening(Constant.EVENT_ON_ROLL_RESULT, DestroyAllDice);
    }

    void OnDestroy()
    {
        TigerForge.EventManager.StopListening(Constant.EVENT_ROLL_DICE, RollAllDice);
        TigerForge.EventManager.StopListening(Constant.EVENT_ON_ROLL_RESULT, DestroyAllDice);
    }

    public void DestroyAllDice()
    {
        foreach (GameObject die in spawnedDice)
        {
            if (die != null)
                Destroy(die);
        }

        spawnedDice.Clear();
    }

    public async void RollAllDice()
    {
        DestroyAllDice();
        if (UiHome.Instance != null && UiHome.Instance.wall != null)
        {

            UiHome.Instance.rollPlane.SetActive(true);
            UiHome.Instance.wall.SetActive(false);
        }

        switch (CurrentRollMode)
        {
            case RollMode.Dice8:
                if (dice8Prefab != null)
                    SpawnAndRoll(dice8Prefab, Vector3.zero, 0);
                break;
            case RollMode.Dice12:
                if (dice12Prefab != null)
                    SpawnAndRoll(dice12Prefab, Vector3.zero, 0);
                break;
            case RollMode.Dice20:
                if (dice20Prefab != null)
                    SpawnAndRoll(dice20Prefab, Vector3.zero, 0);
                break;
            default:
                if (dicePrefab == null || dicePrefab2 == null)
                    return;

                SpawnAndRoll(dicePrefab, dice1Offset, 0);
                SpawnAndRoll(dicePrefab2, dice2Offset, 1);
                break;
        }

        await Task.Delay(Mathf.RoundToInt(wallDelay * 1000f));

        if (UiHome.Instance != null && UiHome.Instance.wall != null)
            UiHome.Instance.wall.SetActive(true);
    }

    void SpawnAndRoll(DiceRoll prefab, Vector3 localOffset, int index)
    {
        Vector3 spawnPosition = transform.position + transform.TransformDirection(localOffset);
        DiceRoll newDie = Instantiate(prefab, spawnPosition, transform.rotation);
        spawnedDice.Add(newDie.gameObject);
        newDie.RollDice(throwForce, rollForce, index);
    }
}
