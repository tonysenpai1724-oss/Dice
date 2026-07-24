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
public enum DiceType
{
    Normal,
    Dodge,
    Poison,
    Heal,
    Shield,
    Backstab,
    Coin,
    BlindStrike,
    Stun,
    Bomb,
    Enemy,
    MultiAttack,
    BonusAtk,
    TripleAttack,
    Ambush,
    X2BonusAtk,
    MagicCoin,
    Counter,
    Armor,
    Vulnerable,
    Exhaust,


}
public class Dice : PoolingObject
{
    [Header("Data")]
    public DiceData data;
    public bool isMerging;
    public DiceType type;

    [Header("Components")]
    public Rigidbody rb;
    public Collider cachedCollider;
    public MeshRenderer meshRenderer;
    //public List<DecalProjector> decals = new();
    // public List<DecalProjector> decals2 = new();
    public List<MeshRenderer> decalMeshes = new();
    public List<MeshRenderer> decalMeshes2 = new();
    public List<MeshRenderer> decalMeshes3 = new();
    public bool preferMeshDecals = true;

    [Header("Physics")]
    public float rbMass = 1.2f;
    public float rbDrag = 0.22f;
    public float rbAngularDrag = 0.7f;
    public float rbMaxAngularVelocity = 45f;
    public PhysicsMaterial dicePhysicMaterial;
    public float driftStopVelocity = 0.18f;
    public float driftStopAngularVelocity = 0.22f;
    public float driftStopDelay = 0.08f;
    public float headOnImpactAssist = 0.75f;
    public float chainedImpactAssistMultiplier = 0.55f;
    public float headOnImpactDotThreshold = 0.9f;
    public float shotSpinForwardTorque = 7f;
    public float shotSpinYawTorque = 2f;
    public float shotSpinRandomTorque = 1f;


    [Header("State")]
    public DiceState state;

    Vector3 defaultScale;
    Material outlineMaterial;
    bool isHovered;
    float slowMoveTimer;
    float shotStopGraceTimer;
    Vector3 lastPlanarVelocity;
    readonly RigidbodyConstraints groundedConstraints =
        RigidbodyConstraints.FreezePositionY |
        RigidbodyConstraints.FreezeRotationX |
        RigidbodyConstraints.FreezeRotationZ;
    readonly RigidbodyConstraints boardMoveConstraints =
        RigidbodyConstraints.FreezePositionY |
        RigidbodyConstraints.FreezeRotationX |
        RigidbodyConstraints.FreezeRotationZ;
    readonly RigidbodyConstraints landingConstraints =
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
            CollisionDetectionMode.ContinuousDynamic;
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
        this.type = data.type;

        Material primaryDecalMaterial = data.decalMaterial.Count > 0
            ? data.decalMaterial[0]
            : null;
        Material secondaryDecalMaterial = data.decalMaterial.Count > 1
            ? data.decalMaterial[1]
            : null;
        int decalCount = data.decalMaterial.Count;

        bool useProjectorPrimary = !preferMeshDecals || decalMeshes.Count == 0;
        // foreach (var d in decals)
        // {
        //     if (d == null)
        //         continue;

        //     bool enabled = useProjectorPrimary && primaryDecalMaterial != null;
        //     d.gameObject.SetActive(enabled);
        //     d.enabled = enabled;
        //     if (primaryDecalMaterial != null)
        //         d.material = primaryDecalMaterial;
        // }

        foreach (var d in decalMeshes)
        {
            if (d == null)
                continue;

            bool enabled = !useProjectorPrimary && primaryDecalMaterial != null && decalCount == 1;
            d.gameObject.SetActive(enabled);
            d.enabled = enabled;
            if (primaryDecalMaterial != null)
                d.sharedMaterial = primaryDecalMaterial;
        }

        bool useProjectorSecondary = !preferMeshDecals || decalMeshes2.Count == 0;
        // foreach (var d in decals2)
        // {
        //     if (d == null)
        //         continue;

        //     bool enabled = useProjectorSecondary && secondaryDecalMaterial != null && decalCount == 2;
        //     d.gameObject.SetActive(enabled);
        //     d.enabled = enabled;
        //     if (secondaryDecalMaterial != null)
        //         d.material = secondaryDecalMaterial;
        // }

        foreach (var d in decalMeshes2)
        {
            if (d == null)
                continue;

            bool enabled = !useProjectorSecondary && secondaryDecalMaterial != null && decalCount == 2;
            d.gameObject.SetActive(enabled);
            d.enabled = enabled;
            if (secondaryDecalMaterial != null)
                d.sharedMaterial = secondaryDecalMaterial;
        }
        foreach (var d in decalMeshes3)
        {
            if (d == null)
                continue;

            bool enabled = !useProjectorSecondary && primaryDecalMaterial != null && decalCount == 2;
            d.gameObject.SetActive(enabled);
            d.enabled = enabled;
            if (primaryDecalMaterial != null)
                d.sharedMaterial = primaryDecalMaterial;
        }



        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = false;
        shotStopGraceTimer = 0f;
        lastPlanarVelocity = Vector3.zero;
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
        slowMoveTimer = 0f;
        shotStopGraceTimer = 0.18f;

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

    public virtual void ActivateSkill()
    {
        if (data == null)
            return;

        data.ExecuteSkill();
    }

    public override void Despawn()
    {
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        base.Despawn();
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

    public void ApplyLandingConstraints()
    {
        rb.constraints =
            landingConstraints;
    }

    void FixedUpdate()
    {
        CachePlanarVelocity();
        StopSlowDrift();
    }

    void CachePlanarVelocity()
    {
        if (rb == null || rb.isKinematic)
        {
            lastPlanarVelocity = Vector3.zero;
            return;
        }

        lastPlanarVelocity = rb.linearVelocity;
        lastPlanarVelocity.y = 0f;
    }

    void StopSlowDrift()
    {
        if (rb == null || rb.isKinematic)
            return;

        if (state == DiceState.Merging ||
            state == DiceState.FlyingCombo)
        {
            slowMoveTimer = 0f;
            return;
        }

        if (shotStopGraceTimer > 0f)
        {
            shotStopGraceTimer -= Time.fixedDeltaTime;
            slowMoveTimer = 0f;
            return;
        }

        float velocityLimit = driftStopVelocity * driftStopVelocity;
        float angularVelocityLimit = driftStopAngularVelocity * driftStopAngularVelocity;

        bool isMovingSlowly =
            rb.linearVelocity.sqrMagnitude <= velocityLimit &&
            rb.angularVelocity.sqrMagnitude <= angularVelocityLimit;

        if (!isMovingSlowly)
        {
            slowMoveTimer = 0f;
            return;
        }

        slowMoveTimer += Time.fixedDeltaTime;

        if (slowMoveTimer < driftStopDelay)
            return;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.Sleep();
        slowMoveTimer = 0f;
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

        ApplyOutlineColor(
            isHovered
                ? data.targetColor
                : data.baseOutlineColor
        );
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
        ApplyHeadOnImpactAssist(col);
        TryMergeCollision(col);
    }

    void OnCollisionStay(Collision col)
    {
        ApplyHeadOnImpactAssist(col);
        TryMergeCollision(col);
    }

    void ApplyHeadOnImpactAssist(Collision col)
    {
        if (rb == null)
            return;

        if (state != DiceState.Shot && shotStopGraceTimer <= 0f)
            return;

        float impactAssistMultiplier =
            state == DiceState.Shot
                ? 1f
                : chainedImpactAssistMultiplier;

        Dice other =
            col.collider.GetComponentInParent<Dice>();

        if (other == null || other == this || other.rb == null)
            return;

        Vector3 planarVelocity = GetImpactPlanarVelocity();

        ApplyImpactAssistToDice(
            other,
            planarVelocity,
            impactAssistMultiplier
        );
    }

    Vector3 GetImpactPlanarVelocity()
    {
        Vector3 planarVelocity = rb.linearVelocity;
        planarVelocity.y = 0f;

        if (lastPlanarVelocity.sqrMagnitude > planarVelocity.sqrMagnitude)
            planarVelocity = lastPlanarVelocity;

        return planarVelocity;
    }

    void ApplyImpactAssistToDice(
        Dice other,
        Vector3 planarVelocity,
        float impactAssistMultiplier
    )
    {
        if (other == null || other == this || other.rb == null)
            return;

        if (other.state == DiceState.Merging || other.state == DiceState.FlyingCombo)
            return;

        if (planarVelocity.sqrMagnitude <= 0.01f)
            return;

        Vector3 hitDirection =
            other.transform.position - transform.position;
        hitDirection.y = 0f;

        if (hitDirection.sqrMagnitude <= 0.0001f)
            return;

        Vector3 moveDirection = planarVelocity.normalized;
        Vector3 impactDirection = hitDirection.normalized;

        float movingTowardTargetDot = Vector3.Dot(
            moveDirection,
            impactDirection
        );

        if (movingTowardTargetDot <= 0f)
            return;

        other.rb.WakeUp();
        other.slowMoveTimer = 0f;
        other.shotStopGraceTimer = Mathf.Max(
            other.shotStopGraceTimer,
            0.18f
        );

        Vector3 otherPlanarVelocity = other.rb.linearVelocity;
        otherPlanarVelocity.y = 0f;

        float targetPushSpeed =
            planarVelocity.magnitude *
            headOnImpactAssist *
            Mathf.Max(0f, impactAssistMultiplier);

        float currentPushSpeed = Vector3.Dot(
            otherPlanarVelocity,
            moveDirection
        );
        currentPushSpeed = Mathf.Max(0f, currentPushSpeed);

        if (currentPushSpeed >= targetPushSpeed)
            return;

        Vector3 targetPlanarVelocity =
            otherPlanarVelocity +
            moveDirection *
            (targetPushSpeed - currentPushSpeed);

        Vector3 targetVelocity = other.rb.linearVelocity;
        targetVelocity.x = targetPlanarVelocity.x;
        targetVelocity.z = targetPlanarVelocity.z;

        other.rb.linearVelocity = targetVelocity;
    }

    void TryMergeCollision(Collision col)
    {
        if (state == DiceState.Merging)
            return;

        if (!gameObject.activeInHierarchy)
            return;

        if (DiceManager.Instance == null)
            return;

        Dice other =
            col.collider.GetComponentInParent<Dice>();

        if (other == null)
            return;

        if (other == this)
            return;

        if (other.state == DiceState.Merging)
            return;

        if (other.Level != Level)
            return;

        if (!canMerge && !other.canMerge)
            return;

        DiceManager.Instance.TryMerge(this, other);
    }
}
