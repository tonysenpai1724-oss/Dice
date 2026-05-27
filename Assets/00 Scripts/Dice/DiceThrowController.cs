// using UnityEngine;

// #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
// using UnityEngine.InputSystem;
// #endif

// public class DiceThrowController : MonoBehaviour
// {
//     [Header("Dice")]
//     public Dice currentDice;

//     public Transform spawnPoint;

//     public int spawnLevel = 1;

//     public bool autoRespawn = true;

//     [Header("Throw")]
//     public float shootStrength = 25f;

//     [Header("Board Check")]
//     public float stableTimeRequired = 0.5f;

//     public float stopVelocityThreshold = 0.05f;

//     private bool dragging;

//     private bool waitingForStop;

//     private float stableTimer;

//     [HideInInspector]
//     public int activeChainRoutines = 0;
//     private float maxWaitTimer;
//     [Header("Combo Chain")]
//     public float comboHorizontalForce = 8;
//     public float comboJumpForce = 6f;


//     void Start()
//     {
//         TrySpawnPlayerDice();
//     }

//     void Update()
//     {
//         // Wait until all dice stop moving before spawning next dice
//         if (waitingForStop)
//         {
//             maxWaitTimer += Time.deltaTime;

//             bool allStopped =
//                 activeChainRoutines == 0 &&
//                 AreAllDiceStopped();

//             bool forceSettle = maxWaitTimer >= 4f;

//             if (allStopped)
//             {
//                 stableTimer += Time.deltaTime;

//                 if (stableTimer >= stableTimeRequired || forceSettle)
//                 {
//                     waitingForStop = false;

//                     stableTimer = 0f;
//                     maxWaitTimer = 0f;

//                     if (autoRespawn)
//                     {
//                         TrySpawnPlayerDice();
//                     }
//                 }
//             }
//             else
//             {
//                 stableTimer = 0f;

//                 // fail-safe
//                 if (forceSettle)
//                 {
//                     waitingForStop = false;

//                     stableTimer = 0f;
//                     maxWaitTimer = 0f;

//                     if (autoRespawn)
//                     {
//                         TrySpawnPlayerDice();
//                     }
//                 }
//             }
//         }

//         // no dice ready yet
//         if (currentDice == null)
//             return;

// #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER

//         // =========================
//         // NEW INPUT SYSTEM
//         // =========================

//         if (Mouse.current != null &&
//             Mouse.current.leftButton.wasPressedThisFrame)
//         {
//             dragging = true;
//         }

//         if (Mouse.current != null &&
//             Mouse.current.leftButton.wasReleasedThisFrame &&
//             dragging)
//         {
//             ShootCurrentDice();

//             dragging = false;
//         }

// #else

//         // =========================
//         // LEGACY INPUT SYSTEM
//         // =========================

//         if (Input.GetMouseButtonDown(0))
//         {
//             dragging = true;
//         }

//         if (Input.GetMouseButtonUp(0) && dragging)
//         {
//             ShootCurrentDice();

//             dragging = false;
//         }

// #endif
//     }

//     void ShootCurrentDice()
//     {
//         if (currentDice == null)
//             return;

//         Vector3 target = GetMouseWorldPosition();

//         Vector3 origin = currentDice.transform.position;

//         Vector3 dir = target - origin;
//         dir.y = 0f;

//         if (dir.sqrMagnitude < 0.0001f)
//         {
//             dir = Vector3.forward;
//         }
//         else
//         {
//             dir = dir.normalized;
//         }

//         Vector3 force = dir * shootStrength;

//         Dice shotDice = currentDice;

//         // remove current reference immediately
//         currentDice = null;

//         // shoot
//         shotDice.Throw(force);
//         stableTimer = 0f;
//         maxWaitTimer = 0f;

//         // now wait until board settles
//         waitingForStop = true;
//     }

//     Vector3 GetMouseWorldPosition()
//     {
// #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER

//         Vector2 mousePos =
//             Mouse.current != null
//             ? Mouse.current.position.ReadValue()
//             : Vector2.zero;

//         Ray ray =
//             Camera.main.ScreenPointToRay(mousePos);

// #else

//         Ray ray =
//             Camera.main.ScreenPointToRay(Input.mousePosition);

// #endif

//         Plane plane = new Plane(Vector3.up, Vector3.zero);

//         if (plane.Raycast(ray, out float distance))
//         {
//             return ray.GetPoint(distance);
//         }

//         return Vector3.zero;
//     }

//     void TrySpawnPlayerDice()
//     {
//         if (currentDice != null)
//             return;

//         if (DiceManager.Instance == null)
//             return;

//         Vector3 pos =
//             spawnPoint != null
//             ? spawnPoint.position
//             : new Vector3(0f, 0.5f, 0f);

//         if (DiceManager.Instance.spawnPlane != null)
//         {
//             pos.y = DiceManager.Instance.spawnPlane.bounds.max.y + 0.5f;
//         }

//         int level =
//             Mathf.Clamp(
//                 spawnLevel,
//                 1,
//                 DiceManager.Instance.diceDatabase.Count
//             );

//         Dice d =
//             DiceManager.Instance.SpawnDice(level, pos);

//         if (d != null)
//         {
//             currentDice = d;

//             // ensure fresh dice starts still
//             if (currentDice.rb != null)
//             {
//                 currentDice.rb.linearVelocity = Vector3.zero;
//                 currentDice.rb.angularVelocity = Vector3.zero;
//             }
//         }
//     }

//     bool AreAllDiceStopped()
//     {
//         Dice[] allDice = FindObjectsOfType<Dice>();

//         for (int i = 0; i < allDice.Length; i++)
//         {
//             Dice d = allDice[i];

//             if (d == null)
//                 continue;

//             if (!d.gameObject.activeInHierarchy)
//                 continue;

//             if (d.rb == null)
//                 continue;

//             // still moving
//             if (
//     d.rb.linearVelocity.magnitude > stopVelocityThreshold ||
//     d.rb.angularVelocity.magnitude > stopVelocityThreshold
// )
//             {
//                 return false;
//             }
//         }

//         return true;
//     }
//     public void NotifyBoardSettled()
//     {
//         waitingForStop = true;
//     }
// }
using UnityEngine;
using UnityEngine.InputSystem;

public class DiceThrowController : MonoBehaviour
{
    [Header("Spawn")]
    public Transform spawnPoint;

    public int spawnLevel = 1;

    [Header("Shoot")]
    public float shootForce = 12f;

    Dice currentDice;

    bool dragging;

    void Start()
    {
        SpawnCurrentDice();
    }

    void Update()
    {
        if (currentDice == null)
            return;

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
        currentDice =
            DiceManager.Instance.SpawnDice(
                spawnLevel,
                spawnPoint.position
            );

        currentDice.rb.linearVelocity =
            Vector3.zero;
    }

    void Shoot()
    {
        Vector3 target =
            GetMouseWorldPosition();

        Vector3 dir =
            target -
            currentDice.transform.position;

        dir.y = 0f;

        currentDice.Shoot(
            dir,
            shootForce
        );

        currentDice = null;

        Invoke(
            nameof(SpawnCurrentDice),
            1f
        );
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