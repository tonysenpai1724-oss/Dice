
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class DiceThrowController : MonoBehaviour
{
    [Header("Spawn")]
    public Transform spawnPoint;
    public DiceHand hand;
    public Vector3 handLocalPosition = new Vector3(0f, 1.5f, 0f);
    public Vector3 handLocalEuler = new Vector3(-90f, 90f, 90f);

    public int spawnLevel = 1;

    [Header("Shoot")]
    public float shootForce = 12f;
    [Range(0f, 89f)]
    public float minLaunchAngle = 15f;

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

    [Header("Highlight")]
    public Transform diceHighlight;

    void Start()
    {
        SpawnCurrentDice();
    }

    void Update()
    {
        if (IsGameEnded())
            return;

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
        if (IsGameEnded())
            return;

        if (currentDice != null)
            return;

        DiceData data = DiceManager.Instance.GetDiceDataByLevel(spawnLevel);

        currentDice =
            DiceManager.Instance.SpawnDice(
                data,
                spawnPoint.position,
                false
            );
        AttachHighlight(currentDice);
        PrepareHand();
        currentDice.rb.linearVelocity =
            Vector3.zero;
    }
    void AttachHighlight(Dice dice)
    {
        if (diceHighlight == null || dice == null)
            return;

        diceHighlight.SetParent(
            dice.transform,
            false
        );

        diceHighlight.localPosition = new Vector3(0, -0.8f, 6.8f);
        //     highlightOffset;

        diceHighlight.localRotation =
       Quaternion.Euler(90f, 0f, 0f);

        diceHighlight.gameObject.SetActive(true);
    }


    void RotateCurrentDiceToMouse()
    {
        if (Camera.main == null || currentDice == null)
            return;

        Vector3 lookDir = GetLookDirection();
        if (lookDir.sqrMagnitude < 0.0001f)
            return;

        float clampedYaw = GetClampedYaw(lookDir);

        currentDice.transform.rotation =
            Quaternion.Euler(0f, clampedYaw, 0f);
    }
    float GetClampedYaw(Vector3 lookDir)
    {
        float rawYaw = Mathf.Atan2(lookDir.x, lookDir.z) * Mathf.Rad2Deg;
        return Mathf.Clamp(rawYaw, -65f, 65f);
    }

    void Shoot()
    {
        if (waitingForBoard || IsGameEnded())
            return;

        Vector3 launchDir =
            GetAimDirection();
        if (launchDir.sqrMagnitude < 0.0001f)
            return;

        TurnManager.Instance.AddTurn();
        DiceManager.Instance.RegisterBoardDice(
            currentDice
        );
        DiceManager.Instance.SetBoardMergeEnabled(true);
        if (diceHighlight != null)
        {
            diceHighlight.SetParent(this.transform);
            diceHighlight.gameObject.SetActive(false);
        }


        currentDice.Shoot(
            launchDir,
            shootForce
        );

        if (hand != null)
        {
            hand.transform.SetParent(null, true);
            hand.Release();
            // hand.gameObject.SetActive(false);
        }

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
        DiceManager.Instance.SetBoardMergeEnabled(false);

        if (!IsGameEnded())
            SpawnCurrentDice();
    }

    bool IsGameEnded()
    {
        return GameplayManager.Instance != null &&
            GameplayManager.Instance.IsGameEnded;
    }

    void PrepareHand()
    {
        if (hand == null || spawnPoint == null)
            return;

        if (currentDice != null)
        {
            //   hand.gameObject.SetActive(true);
            hand.transform.SetParent(currentDice.transform, true);
            hand.transform.localPosition = handLocalPosition;
            hand.transform.localRotation = Quaternion.Euler(handLocalEuler);
        }

        hand.Prepare();
    }

    Vector3 GetMouseWorldPosition()
    {
        float boardY =
            DiceManager.Instance != null
                ? DiceManager.Instance.GetBoardSurfaceY()
                : 0f;

        Vector2 mousePos =
            Mouse.current.position.ReadValue();

        Ray ray =
            Camera.main.ScreenPointToRay(
                mousePos
            );

        Plane plane =
            new Plane(
                Vector3.up,
                new Vector3(
                    0f,
                    boardY,
                    0f
                )
            );

        if (plane.Raycast(ray, out float dist))
        {
            return ray.GetPoint(dist);
        }

        return Vector3.zero;
    }


    Vector3 GetAimDirection()
    {
        if (currentDice == null)
            return Vector3.forward;

        Vector3 target = GetMouseWorldPosition();

        Vector3 flatDir = target - currentDice.transform.position;
        flatDir.y = 0f;

        if (flatDir.sqrMagnitude < 0.0001f)
            flatDir = currentDice.transform.forward;

        flatDir.Normalize();

        // 🔥 clamp yaw giống hệt rotation
        float clampedYaw = GetClampedYaw(flatDir);

        Vector3 dir =
            Quaternion.Euler(0f, clampedYaw, 0f) * Vector3.forward;

        // giữ arc ném
        Vector3 launchDir =
            (dir * Mathf.Cos(minLaunchAngle * Mathf.Deg2Rad) +
             Vector3.up * Mathf.Sin(minLaunchAngle * Mathf.Deg2Rad)).normalized;

        return launchDir;
    }

    Vector3 GetLookDirection()
    {
        if (currentDice == null)
            return Vector3.forward;

        Vector3 target =
            GetMouseWorldPosition();

        Vector3 flatDir =
            target - currentDice.transform.position;
        flatDir.y = 0f;

        if (flatDir.sqrMagnitude < 0.0001f)
            flatDir = currentDice.transform.forward;

        flatDir.Normalize();

        return flatDir;
    }
}
