using System.Collections.Generic;
using UnityEngine;

public class RollDiceVisual : MonoBehaviour
{
    public Rigidbody rb;
    public Collider cachedCollider;
    public MeshRenderer bodyRenderer;
    public List<MeshRenderer> numberDecalMeshes = new();
    public List<GameObject> faceObjects = new();

    public void SetFace(int face)
    {
        for (int i = 0; i < faceObjects.Count; i++)
        {
            if (faceObjects[i] != null)
                faceObjects[i].SetActive(i == face - 1);
        }

        for (int i = 0; i < numberDecalMeshes.Count; i++)
        {
            if (numberDecalMeshes[i] != null)
                numberDecalMeshes[i].gameObject.SetActive(i == face - 1);
        }
    }

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

    public void StopAndSnapUpright()
    {
        if (rb == null)
            return;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.rotation = Quaternion.identity;
        transform.rotation = Quaternion.identity;
        rb.Sleep();
    }
}
