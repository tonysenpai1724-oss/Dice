using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class DiceQueue : MonoBehaviour
{
    [Header("Prefab")]
    public DiceQueueItem itemPrefab;

    [Header("Root")]
    public Transform contentRoot;

    [Header("Layout")]
    public float spacing = 0.5f;
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

    [Header("Conveyor")]
    public Vector3 consumeOffset =
    new Vector3(
        1.2f,
        0f,
        -2.5f
    );

    public float itemMoveDuration = 0.25f;
    public float shiftMoveDuration = 0.2f;
    public float stepDelay = 0.05f;
    public float delayDestoyTime;

    List<DiceQueueItem> items =
        new();

    bool processing;
    List<DiceData> pendingItems =
        new();
    bool flushingPendingItems;

    public void AddDice(DiceData data)
    {
        if (data == null)
            return;

        pendingItems.Add(data);

        if (!flushingPendingItems)
        {
            flushingPendingItems = true;

            StartCoroutine(
                FlushPendingItems()
            );
        }
    }

    public IEnumerator ProcessQueue()
    {
        if (processing)
            yield break;

        processing = true;

        while (true)
        {
            if (items.Count == 0)
            {
                if (pendingItems.Count > 0)
                {
                    yield return null;
                    continue;
                }

                break;
            }

            DiceQueueItem first =
                items[0];

            items.RemoveAt(0);

            if (first != null)
            {
                DiceData diceData = first.data;

                yield return MoveItem(
                    first.transform,
                    first.transform.position + consumeOffset,
                    itemMoveDuration
                );

                if (EnemyManager.Instance != null)
                    EnemyManager.Instance.PlayerAttack(diceData);

                yield return new WaitForSeconds(delayDestoyTime);
                Destroy(
                    first.gameObject
                );
            }

            yield return ShiftItems();

            if (stepDelay > 0f)
            {
                yield return new WaitForSeconds(
                    stepDelay
                );
            }
        }

        if (EnemyManager.Instance != null)
            yield return EnemyManager.Instance.EnemyTurn();

        processing = false;
    }

    IEnumerator FlushPendingItems()
    {
        while (pendingItems.Count > 0)
        {
            yield return new WaitUntil(
                () =>
                    TurnManager.Instance == null ||
                    !TurnManager.Instance.IsResettingBoard
            );

            DiceData data =
                pendingItems[0];

            pendingItems.RemoveAt(0);

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

        flushingPendingItems = false;
    }

    IEnumerator ShiftItems()
    {
        float duration =
            Mathf.Max(
                0.01f,
                shiftMoveDuration
            );

        float timer = 0f;
        Vector3[] starts =
            new Vector3[items.Count];
        Vector3[] targets =
            new Vector3[items.Count];

        for (int i = 0; i < items.Count; i++)
        {
            starts[i] =
                items[i].transform.position;

            targets[i] =
                GetPosition(i);
        }

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer / duration
                );

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == null)
                    continue;

                items[i].transform.position =
                    Vector3.Lerp(
                        starts[i],
                        targets[i],
                        t
                    );
            }

            yield return null;
        }
    }

    IEnumerator MoveItem(
        Transform item,
        Vector3 target,
        float duration
    )
    {
        if (item == null)
            yield break;

        Vector3 start =
            item.position;
        float timer = 0f;
        duration =
            Mathf.Max(
                0.01f,
                duration
            );

        while (timer < duration)
        {
            if (item == null)
                yield break;

            timer += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer / duration
                );

            item.position =
                Vector3.Lerp(
                    start,
                    target,
                    t
                );

            yield return null;
        }
    }

    Vector3 GetPosition(int index)
    {
        return startPosition +
            stackOffset * index;
    }
}
