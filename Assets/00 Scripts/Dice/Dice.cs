
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
public enum DiceState
{
    Idle,
    Shot,
    FlyingCombo,
    Merging
}


public class Dice : MonoBehaviour
{
    [Header("Data")]
    public DiceData data;
    public bool isMerging;

    [Header("Components")]
    public Rigidbody rb;
    public Collider cachedCollider;
    public MeshRenderer meshRenderer;
    public List<DecalProjector> decals = new();
    public List<DecalProjector> decals2 = new();

    [Header("Physics")]
    public float rbMass = 1.2f;
    public float rbDrag = 0.15f;
    public float rbAngularDrag = 0.4f;
    public float rbMaxAngularVelocity = 35f;
    public PhysicsMaterial dicePhysicMaterial;


    [Header("State")]
    public DiceState state;

    Vector3 defaultScale;
    Material outlineMaterial;
    bool isHovered;
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
        isMerging = false;

        state = DiceState.Idle;
        isHovered = false;

        transform.localScale = defaultScale;

        meshRenderer.material = data.diceMaterial;
        CacheOutlineMaterial();
        ApplyOutlineColor(data.baseOutlineColor);

        foreach (var d in decals)
        {
            d.material = data.decalMaterial[0];
        }
        if (data.decalMaterial.Count > 1)
        {
            foreach (var d in decals2)
            {
                d.gameObject.SetActive(true);
                d.material = data.decalMaterial[1];
            }
        }
        else
        {
            foreach (var d in decals2)
            {
                d.gameObject.SetActive(false);
            }
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

    void CacheOutlineMaterial()
    {
        if (meshRenderer == null)
            return;

        Material[] materials =
            meshRenderer.materials;

        if (materials == null || materials.Length <= 1)
            return;

        outlineMaterial = materials[1];
    }

    void ApplyOutlineColor(Color color)
    {
        if (outlineMaterial == null)
            return;

        outlineMaterial.SetColor(
            "_outlineColor",
            color
        );
    }
    public void SetHovered(bool value)
    {
        SetHoverState(value);
    }

    void SetHoverState(bool hovered)
    {
        if (data == null)
            return;

        isHovered = hovered;
        Debug.Log("Hover state: " + isHovered);

        ApplyOutlineColor(
            isHovered
                ? data.targetColor
                : data.baseOutlineColor
        );
    }

    void OnMouseEnter()
    {
        Debug.Log("Mouse Enter");
        SetHoverState(true);
    }

    void OnMouseExit()
    {
        SetHoverState(false);
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


