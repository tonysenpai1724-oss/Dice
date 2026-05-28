using System.Collections.Generic;
using UnityEngine;

public class DiceQueue : MonoBehaviour
{
    [Header("Prefab")]
    public DiceQueueItem itemPrefab;

    [Header("Root")]
    public Transform contentRoot;

    [Header("Layout")]
    public float spacing = 0.5f;

    List<DiceQueueItem> items =
        new();

    public void AddDice(DiceData data)
    {
        DiceQueueItem item =
            Instantiate(itemPrefab);

        item.SetDice(data);

        int index =
            items.Count;

        Transform t =
            item.transform;

        Vector3 pos =
            startPosition +
            stackOffset * index;

        t.position = pos;

        t.rotation =
            Quaternion.Euler(
                8f,
                -406f,
                -10f
            );

        t.localScale =
            Vector3.one * 100f;

        items.Add(item);
    }
    public Vector3 startPosition =
    new Vector3(
        -18.7f,
        11f,
        -11.5f
    );

    public Vector3 stackOffset =
        new Vector3(
            0.3f,
            0f,
            1.5f
        );
}