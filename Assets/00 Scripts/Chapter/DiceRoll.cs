using System;
using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DiceRoll : MonoBehaviour
{
    [Header("References")]
    public Transform[] diceFaces;
    public Rigidbody rb;

    private int diceIndex = -1;
    private bool hasStoppedRolling;
    private bool delayFinished;

    public static event Action<int, int> OnDiceResult;

    void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    void Update()
    {
        if (!delayFinished)
            return;

        if (!hasStoppedRolling && rb.linearVelocity.sqrMagnitude == 0f)
        {
            hasStoppedRolling = true;
            GetNumberOnTopFace();
        }
    }

    public void RollDice(float throwForce, float rollForce, int index)
    {
        diceIndex = index;
        hasStoppedRolling = false;
        delayFinished = false;

        float randomVariance = UnityEngine.Random.Range(-0.5f, 0.5f);

        rb.AddForce(transform.forward * (throwForce + randomVariance), ForceMode.Impulse);

        float randX = UnityEngine.Random.Range(0f, 1f);
        float randY = UnityEngine.Random.Range(0f, 1f);
        float randZ = UnityEngine.Random.Range(0f, 1f);
        Vector3 randomTorque = new Vector3(randX, randY, randZ).normalized;
        rb.AddTorque(randomTorque * (rollForce + randomVariance), ForceMode.Impulse);

        DelayResult();
    }

    async void DelayResult()
    {
        await Task.Delay(1000);
        delayFinished = true;
    }

    [ContextMenu("Get Top Face")]
    int GetNumberOnTopFace()
    {
        if (diceFaces == null || diceFaces.Length == 0)
            return -1;

        int topFaceIndex = 0;
        float lastYPosition = diceFaces[0].position.y;

        for (int i = 0; i < diceFaces.Length; i++)
        {
            if (diceFaces[i].position.y > lastYPosition)
            {
                lastYPosition = diceFaces[i].position.y;
                topFaceIndex = i;
            }
        }

        int finalResult = topFaceIndex + 1;
        OnDiceResult?.Invoke(diceIndex, finalResult);

        Debug.Log($"Xuc xac so {diceIndex} ra mat: {finalResult}");
        // Destroy(gameObject);
        return finalResult;
    }
}
