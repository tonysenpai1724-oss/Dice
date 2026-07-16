
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
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
    public float maxClickShootHoldTime = 0.2f;

    [Header("Board Stable")]
    public float stableTimeRequired = 0.35f;
    public float stopVelocityThreshold = 0.05f;
    public float stopAngularVelocityThreshold = 0.05f;
    public float maxStableWaitTime = 5f;

    [Header("Queue")]
    public DiceQueueManager diceQueue;
    public DiceQueueUI diceQueueUI;

    Dice currentDice;
    Dice hoveredBoardDice;

    bool dragging;
    bool waitingForBoard;
    float mouseDownTime;
    bool pointerStartedOverBoardDice;

    [Header("Highlight")]
    public Transform diceHighlight;
    public bool canLook = true;

    void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsCurrentLevelPopupOnlyGameplay())
        {
            DisableThrowControllerForPopupOnlyLevel();
            return;
        }

        HideThrowVisuals();
        StartCoroutine(SpawnCurrentDiceAfterHeroStartDice());
        TigerForge.EventManager.StartListening(Constant.ON_DRAG_RUNE, OnRuneDragStarted);
        TigerForge.EventManager.StartListening(Constant.ON_DROP_RUNE, OnRuneDragEnded);
        TigerForge.EventManager.StartListening(Constant.ON_END_GAME, Clear);
    }

    IEnumerator SpawnCurrentDiceAfterHeroStartDice()
    {
        yield return null;

        while (DiceManager.Instance != null && DiceManager.Instance.IsSpawningHeroStartDice)
            yield return null;

        SpawnCurrentDice();
    }

    void HideThrowVisuals()
    {
        if (diceHighlight != null)
            diceHighlight.gameObject.SetActive(false);

        if (hand != null)
            hand.gameObject.SetActive(false);
    }

    void DisableThrowControllerForPopupOnlyLevel()
    {
        Clear();

        if (diceHighlight != null)
            diceHighlight.gameObject.SetActive(false);

        if (hand != null)
            hand.gameObject.SetActive(false);

        enabled = false;
    }

    void OnDestroy()
    {
        TigerForge.EventManager.StopListening(Constant.ON_DRAG_RUNE, OnRuneDragStarted);
        TigerForge.EventManager.StopListening(Constant.ON_DROP_RUNE, OnRuneDragEnded);
        TigerForge.EventManager.StopListening(Constant.ON_END_GAME, Clear);

    }

    void OnRuneDragStarted()
    {
        canLook = false;
        dragging = false;
    }

    void OnRuneDragEnded()
    {
        canLook = true;
    }


    void Update()
    {
        if (Time.timeScale < 1)
            return;
        if (IsGameEnded())
            return;


        if (currentDice == null)
            return;

        if (!canLook)
            return;

        UpdateBoardDiceHover();
        RotateCurrentDiceToMouse();

        if (WasPointerPressedThisFrame())
        {
            if (!IsPointerOverBoardMesh())
            {
                dragging = false;
                return;
            }

            dragging = true;
            mouseDownTime = Time.time;
            pointerStartedOverBoardDice = IsPointerOverBoardDice();
        }

        if (WasPointerReleasedThisFrame() && dragging)
        {
            dragging = false;

            if (!IsPointerOverBoardMesh())
                return;

            if (IsAndroid() && pointerStartedOverBoardDice)
                return;

            if (!IsAndroid() && Time.time - mouseDownTime > maxClickShootHoldTime)
                return;

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

        if (!canLook)
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

        DiceQueueUI queueUI =
            diceQueueUI != null
            ? diceQueueUI
            : DiceManager.Instance != null
                ? DiceManager.Instance.diceQueueUI != null
                    ? DiceManager.Instance.diceQueueUI
                    : DiceQueueUI.Instance
                : DiceQueueUI.Instance;

        if (queueUI != null)
        {
            yield return StartCoroutine(
                queueUI.ProcessQueue()
            );
        }
        else
        {
            DiceQueueManager queue =
                diceQueue != null
                ? diceQueue
                : DiceManager.Instance != null
                    ? DiceManager.Instance.diceQueue
                    : null;

            if (queue != null)
            {
                yield return StartCoroutine(
                    queue.ProcessQueue()
                );
            }
        }

        if (TurnManager.Instance != null &&
            TurnManager.Instance.IsResetPending)
        {
            TurnManager.Instance.ResetBoardAfterQueue();
        }

        waitingForBoard = false;
        DiceManager.Instance.SetBoardMergeEnabled(false);

        if (!IsGameEnded())
        {
            HideThrowVisuals();
            StartCoroutine(SpawnCurrentDiceAfterHeroStartDice());
        }
    }

    bool IsGameEnded()
    {
        return GameplayManager.Instance != null &&
            GameplayManager.Instance.IsGameEnded;
    }

    bool IsAndroid()
    {
        return Application.platform == RuntimePlatform.Android;
    }

    bool WasPointerPressedThisFrame()
    {
        if (IsAndroid())
            return Touchscreen.current != null &&
                Touchscreen.current.primaryTouch.press.wasPressedThisFrame;

        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    }

    bool WasPointerReleasedThisFrame()
    {
        if (IsAndroid())
            return Touchscreen.current != null &&
                Touchscreen.current.primaryTouch.press.wasReleasedThisFrame;

        return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
    }

    bool TryGetPointerScreenPosition(out Vector2 screenPosition)
    {
        if (IsAndroid())
        {
            if (Touchscreen.current == null)
            {
                screenPosition = Vector2.zero;
                return false;
            }

            screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        if (Mouse.current == null)
        {
            screenPosition = Vector2.zero;
            return false;
        }

        screenPosition = Mouse.current.position.ReadValue();
        return true;
    }

    public void Clear()
    {
        if (hoveredBoardDice != null)
        {
            hoveredBoardDice.SetHovered(false);
            hoveredBoardDice = null;
        }

        if (currentDice != null)
        {
            currentDice.gameObject.SetActive(false);
        }
    }
    bool IsPointerOverBoardMesh()
    {
        if (Camera.main == null || !TryGetPointerScreenPosition(out Vector2 screenPosition))
            return false;

        if (DiceManager.Instance == null || DiceManager.Instance.boardCollider == null)
            return false;

        Collider boardCollider = DiceManager.Instance.boardCollider;
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            Mathf.Infinity,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide
        );
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider == boardCollider)
                return true;
        }

        return false;
    }

    bool IsPointerOverBoardDice()
    {
        return GetBoardDiceUnderPointer() != null;
    }

    Dice GetBoardDiceUnderPointer()
    {
        if (Camera.main == null || !TryGetPointerScreenPosition(out Vector2 screenPosition))
            return null;

        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            Mathf.Infinity,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide
        );

        for (int i = 0; i < hits.Length; i++)
        {
            Dice dice = hits[i].collider.GetComponentInParent<Dice>();
            if (dice != null && dice != currentDice)
                return dice;
        }

        return null;
    }

    void UpdateBoardDiceHover()
    {
        Dice targetDice = GetBoardDiceUnderPointer();

        if (hoveredBoardDice == targetDice)
            return;

        if (hoveredBoardDice != null)
            hoveredBoardDice.SetHovered(false);

        hoveredBoardDice = targetDice;

        if (hoveredBoardDice != null)
            hoveredBoardDice.SetHovered(true);
    }

    void PrepareHand()
    {
        if (hand == null || spawnPoint == null)
            return;

        if (currentDice != null)
        {
            hand.gameObject.SetActive(true);
            hand.transform.SetParent(currentDice.transform, true);
            hand.transform.localPosition = handLocalPosition;
            hand.transform.localRotation = Quaternion.Euler(handLocalEuler);
        }

        hand.Prepare();
    }

    Vector3 GetPointerWorldPosition()
    {
        if (!TryGetPointerScreenPosition(out Vector2 screenPosition))
            return Vector3.zero;

        float boardY =
            DiceManager.Instance != null
                ? DiceManager.Instance.GetBoardService().GetBoardSurfaceY()
                : 0f;

        Ray ray =
            Camera.main.ScreenPointToRay(
                screenPosition
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

        Vector3 target = GetPointerWorldPosition();

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
            GetPointerWorldPosition();

        Vector3 flatDir =
            target - currentDice.transform.position;
        flatDir.y = 0f;

        if (flatDir.sqrMagnitude < 0.0001f)
            flatDir = currentDice.transform.forward;

        flatDir.Normalize();

        return flatDir;
    }
}



