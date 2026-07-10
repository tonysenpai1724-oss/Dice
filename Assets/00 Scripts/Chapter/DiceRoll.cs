using System;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DiceRoll : MonoBehaviour
{
    [Header("References")]
    public Transform[] diceFaces;
    public Rigidbody rb;

    [Header("Roll Detect")]
    public float settleLinearVelocityThreshold = 0.01f;
    public float settleAngularVelocityThreshold = 0.01f;

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

        if (!hasStoppedRolling &&
            rb.linearVelocity.sqrMagnitude <= settleLinearVelocityThreshold * settleLinearVelocityThreshold &&
            rb.angularVelocity.sqrMagnitude <= settleAngularVelocityThreshold * settleAngularVelocityThreshold)
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

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

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

    [Button("Get Top Face")]
    int GetNumberOnTopFace()
    {
        if (diceFaces == null || diceFaces.Length == 0)
            return -1;

        int topFaceIndex = 0;
        float lastYPosition = diceFaces[0].position.y;

        for (int i = 1; i < diceFaces.Length; i++)
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
        return finalResult;
    }
}
