using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class DiceThrower : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DiceRoll dicePrefab;
    [SerializeField] private DiceRoll dicePrefab2;

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
        if (dicePrefab == null || dicePrefab2 == null)
            return;

        foreach (GameObject die in spawnedDice)
        {
            if (die != null)
                Destroy(die);
        }

        spawnedDice.Clear();
        UiHome.Instance.rollPlane.SetActive(true);
        UiHome.Instance.wall.SetActive(false);

        SpawnAndRoll(dicePrefab, dice1Offset, 0);
        SpawnAndRoll(dicePrefab2, dice2Offset, 1);

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
