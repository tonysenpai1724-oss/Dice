
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
public enum DiceState
{
    Idle,
    Shot,
    FlyingCombo,
    Merging
}
// }
// public abstract class Dice : MonoBehaviour
// {
//     [Header("Data")]
//     public DiceData data;

//     [Header("Components")]
//     public Rigidbody rb;

//     public MeshFilter meshFilter;

//     public MeshRenderer meshRenderer;

//     public List<DecalProjector> decals =
//         new List<DecalProjector>();

//     [Header("State")]
//     public bool merged;
//     public bool wasShot;
//     bool canMerge = true;

//     bool chainChecking;

//     private Vector3 defaultScale;
//     public Vector3 DefaultScale => defaultScale;
//     private Dice comboTarget;
//     private bool comboFlying;
//     private float comboMoveSpeed;
//     private float comboJumpForce;
//     private float comboStartTimer;
//     private float comboDelay = 0.15f;

//     public virtual void Awake()
//     {
//         defaultScale = transform.localScale;

//         rb = GetComponent<Rigidbody>();

//         rb.constraints = RigidbodyConstraints.FreezeRotationX |
//                          RigidbodyConstraints.FreezeRotationZ |
//                          RigidbodyConstraints.FreezePositionY;
//     }

//     public virtual void Setup(DiceData newData)
//     {
//         // Reset scale to default to prevent pooled scale inheritance!
//         transform.localScale = defaultScale;
//         comboFlying = false;
//         comboTarget = null;

//         merged = false;
//         wasShot = false;

//         chainChecking = false;

//         data = newData;

//         meshRenderer.material =
//             data.diceMaterial;

//         foreach (var decal in decals)
//         {
//             decal.material =
//                 data.decalMaterial;
//         }

//         rb.linearVelocity = Vector3.zero;
//         rb.angularVelocity = Vector3.zero;

//         // Reset constraints to default flat constraints on setup
//         rb.constraints = RigidbodyConstraints.FreezeRotationX |
//                          RigidbodyConstraints.FreezeRotationZ |
//                          RigidbodyConstraints.FreezePositionY;
//     }

//     public virtual void Throw(Vector3 force)
//     {
//         comboFlying = false;
//         comboTarget = null;
//         rb.linearVelocity = Vector3.zero;
//         rb.angularVelocity = Vector3.zero;

//         force.y = 0f;

//         rb.linearVelocity = force;

//         // Freeze all rotations and Y position to slide flatly like a billiard ball
//         rb.constraints = RigidbodyConstraints.FreezeRotationX |
//                          RigidbodyConstraints.FreezeRotationY |
//                          RigidbodyConstraints.FreezeRotationZ |
//                          RigidbodyConstraints.FreezePositionY;
//         wasShot = true;
//     }

//     void OnCollisionEnter(Collision collision)
//     {
//         if (merged)
//             return;

//         Dice other =
//             collision.gameObject.GetComponent<Dice>();

//         if (other == null)
//             return;

//         if (other == this)
//             return;

//         if (other.merged)
//             return;

//         if (other.data.level != data.level)
//             return;

//         // Mark both as merged immediately to prevent double collisions
//         if (!wasShot && !other.wasShot)
//             return;
//         merged = true;
//         other.merged = true;
//         Merge(other);
//     }

//     public virtual void Merge(Dice other)
//     {
//         Vector3 mergePos =
//             (transform.position + other.transform.position) * 0.5f;

//         int newLevel =
//             data.level + 1;
//         DiceThrowController controller =
//             FindObjectOfType<DiceThrowController>();

//         if (controller != null)
//         {
//             if (controller.currentDice == this)
//                 controller.currentDice = null;

//             if (controller.currentDice == other)
//                 controller.currentDice = null;
//         }
//         ObjectPool.Instance.Return(gameObject);

//         ObjectPool.Instance.Return(other.gameObject);

//         Dice mergedDice =
//             DiceManager.Instance.SpawnMergedDice(
//                 newLevel,
//                 mergePos
//             );

//         // Run coroutine on the persistent DiceManager instance so it survives GameObject deactivation
//         DiceManager.Instance.StartCoroutine(
//             ChainRoutine(mergedDice)
//         );
//     }

//     IEnumerator ChainRoutine(Dice mergedDice)
//     {
//         DiceThrowController controller = FindObjectOfType<DiceThrowController>();
//         if (controller != null)
//         {
//             controller.activeChainRoutines++;
//         }

//         try
//         {
//             yield return new WaitForSeconds(0.05f);

//             if (mergedDice == null || !mergedDice.gameObject.activeInHierarchy)
//             {
//                 yield break;
//             }

//             Dice target =
//       DiceManager.Instance.FindNearestSameLevelDice(
//           mergedDice,
//           mergedDice.data.level,
//           999f
//       );

//             if (target == null)
//             {
//                 if (controller != null)
//                 {
//                     controller.NotifyBoardSettled();
//                 }
//                 yield break;
//             }

//             float distance =
//       Vector3.Distance(
//           mergedDice.transform.position,
//           target.transform.position
//       );

//             float explosionForce =
//                 Mathf.Clamp(
//                     distance * 4f,
//                     7f,
//                     controller != null
//                         ? controller.comboHorizontalForce
//                         : 12f
//                 );

//             float jumpForce =
//     controller != null
//     ? controller.comboJumpForce
//     : 5f;
//             float travelTime = 0.55f;
//             float arcHeight = 4f;
//             // float jumpForce = 11f;

//             mergedDice.ComboThrow(
//        target,
//        travelTime,
//        arcHeight
//    );
//             // Keep routine active during flight
//             float elapsed = 0f;
//             while (mergedDice != null && mergedDice.gameObject.activeInHierarchy && elapsed < 3.0f)
//             {
//                 elapsed += Time.deltaTime;
//                 if (mergedDice.rb == null || mergedDice.rb.linearVelocity.magnitude <= (controller != null ? controller.stopVelocityThreshold : 0.05f))
//                 {
//                     break;
//                 }
//                 yield return null;
//             }
//         }
//         finally
//         {
//             if (controller != null)
//             {
//                 controller.activeChainRoutines--;
//             }
//         }
//     }


//     public virtual void Skill()
//     {

//     }
//     //     public virtual void ComboThrow(
//     //      Vector3 targetPosition,
//     //      float explosionForce,
//     //      float jumpForce
//     //  )
//     //     {
//     //         rb.linearVelocity = Vector3.zero;
//     //         rb.angularVelocity = Vector3.zero;

//     //         // Full physics
//     //         rb.constraints = RigidbodyConstraints.None;

//     //         Vector3 dir =
//     //             (targetPosition - transform.position);

//     //         dir.y = 0f;
//     //         dir = dir.normalized;

//     //         // Explosion-like launch
//     //         Vector3 velocity =
//     //             dir * explosionForce;

//     //         velocity.y = jumpForce;

//     //         rb.linearVelocity = velocity;

//     //         // Crazy spin
//     //         rb.AddTorque(
//     //             Random.insideUnitSphere * 25f,
//     //             ForceMode.Impulse
//     //         );

//     //         wasShot = true;
//     //     }
//     public virtual void ComboThrow(
//         Dice target,
//         float travelTime,
//         float arcHeight
//     )
//     {
//         if (target == null)
//             return;

//         rb.linearVelocity = Vector3.zero;
//         rb.angularVelocity = Vector3.zero;

//         rb.constraints = RigidbodyConstraints.None;

//         Vector3 start = transform.position;
//         Vector3 end = target.transform.position;

//         Vector3 displacement = end - start;

//         // Horizontal movement
//         Vector3 displacementXZ =
//             new Vector3(
//                 displacement.x,
//                 0f,
//                 displacement.z
//             );

//         // Calculate horizontal velocity
//         Vector3 velocityXZ =
//             displacementXZ / travelTime;

//         // Calculate vertical velocity for arc
//         float velocityY =
//             (displacement.y / travelTime)
//             + 0.5f *
//             Mathf.Abs(Physics.gravity.y) *
//             travelTime;

//         Vector3 finalVelocity =
//             velocityXZ +
//             Vector3.up * velocityY;

//         rb.linearVelocity = finalVelocity;

//         // Extra arc boost
//         rb.AddForce(
//             Vector3.up * arcHeight,
//             ForceMode.Impulse
//         );

//         // Spin
//         rb.AddTorque(
//             Random.insideUnitSphere * 10f,
//             ForceMode.Impulse
//         );

//         wasShot = true;
//     }
//     public virtual void Update()
//     {
//         if (comboFlying)
//         {
//             comboStartTimer -= Time.deltaTime;
//         }
//         if (
//     comboFlying &&
//     comboTarget != null &&
//     comboTarget.gameObject.activeInHierarchy
// )
//         {
//             Vector3 toTarget =
//                 comboTarget.transform.position -
//                 transform.position;

//             float distance = toTarget.magnitude;

//             // stop assist when close
//             if (distance < 0.7f)
//             {
//                 comboFlying = false;
//             }
//             else
//             {
//                 Vector3 flatDir = toTarget;
//                 flatDir.y = 0f;

//                 flatDir.Normalize();

//                 Vector3 vel = rb.linearVelocity;

//                 // VERY LIGHT steering
//                 Vector3 steer =
//                     flatDir * 18f * Time.deltaTime;

//                 vel.x += steer.x;
//                 vel.z += steer.z;

//                 rb.linearVelocity = vel;
//             }
//         }


//     }
// }


public class Dice : MonoBehaviour
{
    [Header("Data")]
    public DiceData data;

    [Header("Components")]
    public Rigidbody rb;
    public Collider cachedCollider;
    public MeshRenderer meshRenderer;
    public List<DecalProjector> decals = new();

    [Header("Physics")]
    public float rbMass = 1.2f;
    public float rbDrag = 0.15f;
    public float rbAngularDrag = 0.4f;
    public float rbMaxAngularVelocity = 35f;
    public PhysicsMaterial dicePhysicMaterial;

    [Header("State")]
    public DiceState state;

    Vector3 defaultScale;
    readonly RigidbodyConstraints groundedConstraints =
        RigidbodyConstraints.FreezePositionY |
        RigidbodyConstraints.FreezeRotationX |
        RigidbodyConstraints.FreezeRotationZ;
    readonly RigidbodyConstraints boardMoveConstraints =
        RigidbodyConstraints.FreezePositionY |
        RigidbodyConstraints.FreezeRotationX |
        RigidbodyConstraints.FreezeRotationZ;
    readonly RigidbodyConstraints flyingConstraints =
        RigidbodyConstraints.None;

    public int Level => data.level;
    public bool canMerge;

    public virtual void Awake()
    {
        defaultScale = transform.localScale;

        rb = GetComponent<Rigidbody>();
        cachedCollider = GetComponent<Collider>();

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode =
            CollisionDetectionMode.Continuous;
        ApplyPhysicsSettings();

        ApplyGroundedConstraints();
    }

    public virtual void Setup(DiceData newData)
    {
        data = newData;
        canMerge = false;

        state = DiceState.Idle;

        transform.localScale = defaultScale;

        meshRenderer.material = data.diceMaterial;

        foreach (var d in decals)
        {
            d.material = data.decalMaterial;
        }

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = false;
        ApplyPhysicsSettings();
        ApplyGroundedConstraints();

        if (cachedCollider != null)
        {
            cachedCollider.enabled = true;
        }
    }

    public virtual void Shoot(Vector3 dir, float force)
    {
        state = DiceState.Shot;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        canMerge = true;
        ApplyBoardMoveConstraints();

        dir.y = 0f;
        dir.Normalize();

        rb.AddForce(
            dir * force,
            ForceMode.Impulse
        );

        rb.AddTorque(
            new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-6f, 6f),
                Random.Range(-10f, 10f)
            ),
            ForceMode.Impulse
        );
    }
    public virtual void Skill()
    {

    }

    public void FreezeForMerge()
    {
        canMerge = false;
        state = DiceState.Merging;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        if (cachedCollider != null)
        {
            cachedCollider.enabled = false;
        }
    }

    public void ApplyGroundedConstraints()
    {
        rb.constraints =
            groundedConstraints;
    }

    public void ApplyBoardMoveConstraints()
    {
        rb.constraints =
            boardMoveConstraints;
    }

    void ApplyPhysicsSettings()
    {
        rb.mass = rbMass;
        rb.linearDamping = rbDrag;
        rb.angularDamping = rbAngularDrag;
        rb.maxAngularVelocity = rbMaxAngularVelocity;

        if (cachedCollider != null &&
            dicePhysicMaterial != null)
        {
            cachedCollider.sharedMaterial =
                dicePhysicMaterial;
        }
    }

    public void ApplyFlyingConstraints()
    {
        rb.constraints =
            flyingConstraints;
    }

    public void SetCollisionEnabled(bool enabled)
    {
        if (cachedCollider != null)
        {
            cachedCollider.enabled = enabled;
        }
    }

    public void SnapUpright()
    {
        Quaternion uprightRotation =
            GetUprightRotation();

        rb.angularVelocity = Vector3.zero;
        transform.rotation = uprightRotation;
        rb.rotation = uprightRotation;
    }

    public void PlaceUpright(
        Vector3 position
    )
    {
        Quaternion uprightRotation =
            GetUprightRotation();

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.SetPositionAndRotation(
            position,
            uprightRotation
        );
        rb.position = position;
        rb.rotation = uprightRotation;
        rb.isKinematic = false;
        ApplyGroundedConstraints();

        if (cachedCollider != null)
        {
            cachedCollider.enabled = true;
        }

        rb.Sleep();
    }

    public Quaternion GetUprightRotation()
    {
        Vector3 euler =
            transform.rotation.eulerAngles;

        return Quaternion.Euler(
            0f,
            euler.y,
            0f
        );
    }

    void OnCollisionEnter(Collision col)
    {
        if (state == DiceState.Merging)
            return;
        if (!gameObject.activeInHierarchy)
            return;
        Dice other =
            col.collider.GetComponent<Dice>();

        if (other == null)
            return;

        if (other == this)
            return;

        if (other.state == DiceState.Merging)
            return;

        if (other.Level != Level)
            return;

        // IMPORTANT
        if (!canMerge && !other.canMerge)
            return;

        DiceManager.Instance.TryMerge(this, other);
    }
}
