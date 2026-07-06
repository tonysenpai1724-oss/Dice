using UnityEngine;
using Sirenix.OdinInspector;
public class RollDiceVisual : MonoBehaviour
{
    public Rigidbody rb;
    public Collider cachedCollider;
    public Transform[] facePoints = new Transform[6];

    public void PrepareForRoll()
    {
        if (cachedCollider != null)
            cachedCollider.enabled = true;

        if (rb == null)
            return;

        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.WakeUp();
    }

    public void Roll(Vector3 force, Vector3 torque)
    {
        PrepareForRoll();

        if (rb == null)
            return;

        rb.AddForce(force, ForceMode.Impulse);
        rb.AddTorque(torque, ForceMode.Impulse);
    }

    public int GetTopFace()
    {
        int bestFace = 1;
        float bestDot = float.NegativeInfinity;

        for (int i = 0; i < facePoints.Length; i++)
        {
            Vector3 normal = GetFaceNormal(i);
            if (normal == Vector3.zero)
                continue;

            float dot = Vector3.Dot(normal, Vector3.up);
            if (dot > bestDot)
            {
                bestDot = dot;
                bestFace = i + 1;
            }
        }

        return bestFace;
    }

    public void SnapToFace(int face)
    {
        int index = Mathf.Clamp(face - 1, 0, facePoints.Length - 1);
        Vector3 normal = GetFaceNormal(index);
        if (normal == Vector3.zero)
            return;

        Quaternion delta = Quaternion.FromToRotation(normal, Vector3.up);
        Quaternion targetRotation = delta * transform.rotation;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.rotation = targetRotation;
            rb.Sleep();
        }

        transform.rotation = targetRotation;
    }

    Vector3 GetFaceNormal(int index)
    {
        if (facePoints == null || index < 0 || index >= facePoints.Length)
            return Vector3.zero;

        Transform facePoint = facePoints[index];
        if (facePoint == null)
            return Vector3.zero;

        Vector3 normal = (facePoint.position - transform.position).normalized;
        return normal;
    }

    [Button("LogTopFace")]
    void LogTopFace()
    {
        int topFace = GetTopFace();
        Debug.Log($"[RollDiceVisual] Top face = {topFace}", this);

        for (int i = 0; i < facePoints.Length; i++)
        {
            Transform facePoint = facePoints[i];
            if (facePoint == null)
            {
                Debug.Log($"[RollDiceVisual] Face {i + 1}: missing marker", this);
                continue;
            }

            Vector3 normal = GetFaceNormal(i);
            float dot = Vector3.Dot(normal, Vector3.up);
            Debug.Log($"[RollDiceVisual] Face {i + 1}: dot={dot:F3}, marker={facePoint.name}, normal={normal}", facePoint);
        }
    }

    [Button("Snap To Face 1")]
    void SnapToFace1() => SnapToFace(1);

    [Button("Snap To Face 2")]
    void SnapToFace2() => SnapToFace(2);

    [Button("Snap To Face 3")]
    void SnapToFace3() => SnapToFace(3);

    [Button("Snap To Face 4")]
    void SnapToFace4() => SnapToFace(4);

    [Button("Snap To Face 5")]
    void SnapToFace5() => SnapToFace(5);

    [Button("Snap To Face 6")]
    void SnapToFace6() => SnapToFace(6);

    void OnDrawGizmosSelected()
    {
        if (facePoints == null)
            return;

        for (int i = 0; i < facePoints.Length; i++)
        {
            Transform facePoint = facePoints[i];
            if (facePoint == null)
                continue;

            Vector3 normal = GetFaceNormal(i);
            float dot = Vector3.Dot(normal, Vector3.up);
            Color color = Color.Lerp(Color.red, Color.green, Mathf.InverseLerp(-1f, 1f, dot));
            Gizmos.color = color;
            Gizmos.DrawSphere(facePoint.position, 0.05f);
            Gizmos.DrawLine(facePoint.position, facePoint.position + normal * 0.2f);
        }
    }
}
