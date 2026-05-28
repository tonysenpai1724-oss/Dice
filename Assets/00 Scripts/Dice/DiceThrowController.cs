
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
                spawnPoint.position,
                false
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
