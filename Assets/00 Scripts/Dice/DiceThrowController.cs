
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class DiceThrowController : MonoBehaviour
{
    [Header("Spawn")]
    public Transform spawnPoint;

    public int spawnLevel = 1;

    [Header("Shoot")]
    public float shootForce = 12f;

    [Header("Board Stable")]
    public float stableTimeRequired = 0.35f;
    public float stopVelocityThreshold = 0.05f;
    public float stopAngularVelocityThreshold = 0.05f;
    public float maxStableWaitTime = 5f;

    [Header("Queue")]
    public DiceQueue diceQueue;

    Dice currentDice;

    bool dragging;
    bool waitingForBoard;

    void Start()
    {
        SpawnCurrentDice();
    }

    void Update()
    {
        if (currentDice == null)
            return;

        RotateCurrentDiceToMouse();

        if (
            Mouse.current.leftButton.wasPressedThisFrame
        )
        {
            dragging = true;
        }

        if (
            Mouse.current.leftButton.wasReleasedThisFrame &&
            dragging
        )
        {
            dragging = false;

            Shoot();
        }
    }

    void SpawnCurrentDice()
    {
        if (currentDice != null)
            return;

        currentDice =
            DiceManager.Instance.SpawnDice(
                spawnLevel,
                spawnPoint.position,
                false
            );

        currentDice.rb.linearVelocity =
            Vector3.zero;
    }

    void RotateCurrentDiceToMouse()
    {
        if (Camera.main == null)
            return;

        Vector3 target =
            GetMouseWorldPosition();

        Vector3 dir =
            target - currentDice.transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            return;

        currentDice.transform.rotation =
            Quaternion.LookRotation(
                dir.normalized,
                Vector3.up
            );
    }

    void Shoot()
    {
        if (waitingForBoard)
            return;

        Vector3 target =
            GetMouseWorldPosition();

        Vector3 dir =
            target -
            currentDice.transform.position;
        TurnManager.Instance.AddTurn();
        DiceManager.Instance.RegisterBoardDice(
            currentDice
        );

        dir.y = 0f;

        currentDice.Shoot(
            dir,
            shootForce
        );

        currentDice = null;

        StartCoroutine(
            WaitForBoardThenProcessQueue()
        );
    }

    IEnumerator WaitForBoardThenProcessQueue()
    {
        waitingForBoard = true;

        float stableTimer = 0f;
        float waitTimer = 0f;

        while (stableTimer < stableTimeRequired &&
            waitTimer < maxStableWaitTime)
        {
            waitTimer += Time.deltaTime;

            if (DiceManager.Instance.IsBoardStable(
                stopVelocityThreshold,
                stopAngularVelocityThreshold
            ))
            {
                stableTimer += Time.deltaTime;
            }
            else
            {
                stableTimer = 0f;
            }

            yield return null;
        }

        DiceQueue queue =
            diceQueue != null
            ? diceQueue
            : DiceManager.Instance.diceQueue;

        if (queue != null)
        {
            yield return StartCoroutine(
                queue.ProcessQueue()
            );
        }

        if (TurnManager.Instance != null &&
            TurnManager.Instance.IsResetPending)
        {
            TurnManager.Instance.ResetBoardAfterQueue();
        }

        waitingForBoard = false;
        SpawnCurrentDice();
    }

    Vector3 GetMouseWorldPosition()
    {
        Vector2 mousePos =
            Mouse.current.position.ReadValue();

        Ray ray =
            Camera.main.ScreenPointToRay(
                mousePos
            );

        Plane plane =
            new Plane(
                Vector3.up,
                Vector3.zero
            );

        if (plane.Raycast(ray, out float dist))
        {
            return ray.GetPoint(dist);
        }

        return Vector3.zero;
    }
}
