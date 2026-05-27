using UnityEngine;
using System.Collections.Generic;

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance;

    public GameObject prefab;

    private Queue<GameObject> pool =
        new Queue<GameObject>();

    private void Awake()
    {
        Instance = this;
    }

    public GameObject Get()
    {
        return Get(
            Vector3.zero,
            Quaternion.identity
        );
    }

    public GameObject Get(
        Vector3 position,
        Quaternion rotation
    )
    {
        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();

            obj.transform.SetPositionAndRotation(
                position,
                rotation
            );

            obj.SetActive(true);

            return obj;
        }

        return Instantiate(
            prefab,
            position,
            rotation
        );
    }

    public void Return(GameObject obj)
    {
        if (obj == null)
            return;

        Rigidbody rb =
            obj.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;
        }

        obj.SetActive(false);

        pool.Enqueue(obj);
    }
}
