using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class RollDiceVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Transform[] facePoints = new Transform[6];
    [SerializeField] private Rigidbody body;

    [Header("Main Jump")]
    [SerializeField] private float spawnHeight = 5.6f;
    [SerializeField] private float mainArcHeight = 2f;
    [SerializeField] private float mainDuration = 0.3f;
    [SerializeField] private float mainForwardDistance = 1.05f;
    [SerializeField] private float sideScatter = 0.24f;

    [Header("Landing")]
    [SerializeField] private float landingUprightBlend = 0.08f;

    [Header("Bounce")]
    [SerializeField] private int bounceCount = 3;
    [SerializeField] private float[] bounceHeights = { 0.72f, 0.3f, 0.1f };
    [SerializeField] private float[] bounceDurations = { 0.22f, 0.16f, 0.1f };
    [SerializeField] private float bounceTravelDistance = 0.72f;
    [SerializeField] private float bounceTravelDecay = 0.68f;
    [SerializeField] private float bounceSideRandom = 0.12f;
    [SerializeField] private float bounceHeightNoise = 0.015f;

    [Header("Bounce Spin")]
    [SerializeField] private Vector3[] bounceSpinByStep =
    {
        new Vector3(85f, 20f, 110f),
        new Vector3(35f, 10f, 42f),
        new Vector3(10f, 4f, 12f),
    };
    [SerializeField] private float bounceRotationBlend = 0.45f;

    [Header("Spin")]
    [SerializeField] private Vector3 mainSpinDegrees = new Vector3(620f, 120f, 640f);

    [Header("Settle")]
    [SerializeField] private float settleDuration = 0.22f;
    [SerializeField] private float settleScaleAmount = 0.03f;
    [SerializeField] private float settleBottomAlignBlend = 1f;
    [SerializeField] private float settleNoise = 0.003f;

    [Header("Visual")]
    [SerializeField] private Vector3 baseScale = Vector3.one;
    [SerializeField] private float squashAmount = 0.08f;
    [SerializeField] private float stretchAmount = 0.06f;

    public int CurrentFace { get; private set; } = 1;
    public bool IsFinished { get; private set; } = true;
    public event Action Finished;

    enum RollPhase
    {
        Idle,
        MainJump,
        Bounce,
        Settle,
    }

    RollPhase phase = RollPhase.Idle;
    float phaseTime;
    int bounceIndex;
    Vector3 boardPosition;
    Vector3 mainJumpStart;
    Vector3 mainJumpEnd;
    Vector3 bounceStart;
    Vector3 bounceEnd;
    Vector3 settleStart;
    Vector3 moveDirection = Vector3.forward;
    Quaternion segmentStartRotation;
    Quaternion targetRotation = Quaternion.identity;
    Quaternion landedRotation = Quaternion.identity;
    Quaternion bounceStartRotation = Quaternion.identity;
    Quaternion settleStartRotation = Quaternion.identity;
    Quaternion settleTargetRotation = Quaternion.identity;

    void Awake()
    {
        if (visualRoot == null)
            visualRoot = transform;

        if (body == null)
            body = GetComponent<Rigidbody>();

        body.isKinematic = true;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        ApplyScale(baseScale);
    }

    void FixedUpdate()
    {
        if (phase == RollPhase.Idle)
            return;

        phaseTime += Time.fixedDeltaTime;
        TickPhase();
    }

    public void ClearFinishedListeners()
    {
        Finished = null;
    }

    public void SpawnAndRoll(Vector3 position, Vector3 direction, int targetFace)
    {
        EnsureBody();

        boardPosition = position;
        moveDirection = GetPlanarDirection(direction);
        CurrentFace = Mathf.Clamp(targetFace, 1, 6);
        targetRotation = GetTargetRotation(CurrentFace);
        landedRotation = Quaternion.identity;
        bounceStartRotation = Quaternion.identity;
        settleStartRotation = Quaternion.identity;
        settleTargetRotation = Quaternion.identity;
        bounceIndex = 0;
        IsFinished = false;

        body.isKinematic = true;
        body.useGravity = false;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;

        mainJumpStart = boardPosition + Vector3.up * spawnHeight;
        mainJumpEnd = boardPosition + moveDirection * mainForwardDistance + GetSideScatter();
        mainJumpEnd.y = boardPosition.y;

        segmentStartRotation = UnityEngine.Random.rotation;
        MoveBody(mainJumpStart, segmentStartRotation);
        phase = RollPhase.MainJump;
        phaseTime = 0f;
        ApplyScale(GetStretchScale(stretchAmount));
    }

    public void ForceFace(int face)
    {
        EnsureBody();
        CurrentFace = Mathf.Clamp(face, 1, 6);
        targetRotation = GetTargetRotation(CurrentFace);
        MoveBody(transform.position, targetRotation);
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        ApplyScale(baseScale);
        CompleteRoll();
    }

    public int GetTopFace()
    {
        if (facePoints == null || facePoints.Length < 6)
            return CurrentFace;

        int bestFace = 1;
        float bestDot = float.NegativeInfinity;

        for (int i = 0; i < facePoints.Length; i++)
        {
            if (facePoints[i] == null)
                continue;

            Vector3 normal = (facePoints[i].position - transform.position).normalized;
            float dot = Vector3.Dot(normal, Vector3.up);
            if (dot > bestDot)
            {
                bestDot = dot;
                bestFace = i + 1;
            }
        }

        return bestFace;
    }

    void TickPhase()
    {
        switch (phase)
        {
            case RollPhase.MainJump:
                EvaluateMainJump();
                break;
            case RollPhase.Bounce:
                EvaluateBounce();
                break;
            case RollPhase.Settle:
                EvaluateSettle();
                break;
        }
    }

    void EvaluateMainJump()
    {
        float t = Mathf.Clamp01(phaseTime / Mathf.Max(0.01f, mainDuration));
        float horizontalT = EaseOutQuad(t);
        Vector3 position = Vector3.Lerp(mainJumpStart, mainJumpEnd, horizontalT);
        position.y = boardPosition.y + Mathf.Lerp(spawnHeight, 0f, t) + Mathf.Sin(t * Mathf.PI) * mainArcHeight;

        Vector3 rotationEuler = mainSpinDegrees * horizontalT;
        Quaternion rotation = segmentStartRotation * Quaternion.Euler(rotationEuler);
        MoveBody(position, rotation);
        ApplyScale(Vector3.LerpUnclamped(GetStretchScale(stretchAmount), GetSquashScale(squashAmount), t));

        if (t < 1f)
            return;

        boardPosition = mainJumpEnd;
        landedRotation = Quaternion.Slerp(body.rotation, GetUprightRotation(body.rotation), landingUprightBlend);
        BeginBounce();
    }

    void BeginBounce()
    {
        if (bounceIndex >= Mathf.Max(1, bounceCount))
        {
            BeginSettle();
            return;
        }

        bounceStart = boardPosition;
        float forwardDistance = bounceTravelDistance * Mathf.Pow(bounceTravelDecay, bounceIndex);
        Vector3 randomOffset = GetBounceOffset();
        bounceEnd = bounceStart + moveDirection * forwardDistance + randomOffset;
        bounceEnd.y = boardPosition.y;
        bounceStartRotation = bounceIndex == 0 ? landedRotation : transform.rotation;
        phase = RollPhase.Bounce;
        phaseTime = 0f;
    }

    void EvaluateBounce()
    {
        float duration = GetBounceDuration(bounceIndex);
        float height = GetBounceHeight(bounceIndex);
        float t = Mathf.Clamp01(phaseTime / Mathf.Max(0.01f, duration));
        float currentBounceT = EaseOutQuad(t);
        float spinT = Mathf.SmoothStep(0f, 1f, t);

        Vector3 horizontalPos = Vector3.Lerp(bounceStart, bounceEnd, currentBounceT);
        float noise = Mathf.Sin(t * 7f) * bounceHeightNoise;
        float currentY = boardPosition.y + Mathf.Sin(t * Mathf.PI) * height + noise;
        Vector3 position = new Vector3(horizontalPos.x, currentY, horizontalPos.z);

        Quaternion rotation = GetBounceRotation(spinT);
        MoveBody(position, rotation);
        ApplyScale(EvaluateBounceScale(t));

        if (t < 1f)
            return;

        boardPosition = bounceEnd;
        bounceIndex++;
        BeginBounce();
    }

    void BeginSettle()
    {
        settleStart = boardPosition;
        settleStartRotation = transform.rotation;
        settleTargetRotation = GetBottomFaceContactRotation(settleStartRotation);
        phase = RollPhase.Settle;
        phaseTime = 0f;
    }

    void EvaluateSettle()
    {
        float t = Mathf.Clamp01(phaseTime / Mathf.Max(0.01f, settleDuration));
        float eased = EaseOutCubic(t);
        Quaternion rotation = Quaternion.Slerp(settleStartRotation, settleTargetRotation, eased);

        float settleOffset = Mathf.Sin((1f - t) * Mathf.PI) * settleNoise;
        Vector3 position = settleStart + Vector3.up * settleOffset * (1f - eased);

        MoveBody(position, rotation);
        ApplyScale(Vector3.LerpUnclamped(GetSquashScale(squashAmount * settleScaleAmount), baseScale, eased));

        if (t < 1f)
            return;

        MoveBody(settleStart, settleTargetRotation);
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        ApplyScale(baseScale);
        CompleteRoll();
    }

    Quaternion GetBounceRotation(float spinT)
    {
        Vector3 spinEuler = GetBounceSpinEuler(bounceIndex);
        Quaternion spinRotation = Quaternion.Euler(spinEuler * spinT);
        Quaternion targetBounceRotation = bounceStartRotation * spinRotation;
        return Quaternion.Slerp(bounceStartRotation, targetBounceRotation, spinT * bounceRotationBlend);
    }

    Vector3 GetBounceSpinEuler(int index)
    {
        if (bounceSpinByStep != null && bounceSpinByStep.Length > 0)
            return bounceSpinByStep[Mathf.Min(index, bounceSpinByStep.Length - 1)];

        return new Vector3(60f, 16f, 70f);
    }

    Quaternion GetBottomFaceContactRotation(Quaternion fromRotation)
    {
        Vector3 bottomNormal = GetBottomFaceNormal(fromRotation);
        Quaternion alignBottomToBoard = Quaternion.FromToRotation(bottomNormal, Vector3.down);
        Quaternion boardContactRotation = alignBottomToBoard * fromRotation;
        return Quaternion.Slerp(fromRotation, boardContactRotation, settleBottomAlignBlend);
    }

    Vector3 GetBottomFaceNormal(Quaternion rotation)
    {
        if (facePoints == null || facePoints.Length < 6)
            return rotation * Vector3.down;

        int bottomIndex = -1;
        float lowestDot = float.PositiveInfinity;

        for (int i = 0; i < facePoints.Length; i++)
        {
            if (facePoints[i] == null)
                continue;

            Vector3 normal = (facePoints[i].position - transform.position).normalized;
            float dot = Vector3.Dot(normal, Vector3.up);
            if (dot < lowestDot)
            {
                lowestDot = dot;
                bottomIndex = i;
            }
        }

        if (bottomIndex < 0 || facePoints[bottomIndex] == null)
            return rotation * Vector3.down;

        return (facePoints[bottomIndex].position - transform.position).normalized;
    }

    void MoveBody(Vector3 position, Quaternion rotation)
    {
        body.MovePosition(position);
        body.MoveRotation(rotation);
        transform.SetPositionAndRotation(position, rotation);
    }

    void CompleteRoll()
    {
        phase = RollPhase.Idle;
        phaseTime = 0f;
        IsFinished = true;
        Finished?.Invoke();
    }

    float GetBounceHeight(int index)
    {
        if (bounceHeights != null && bounceHeights.Length > 0)
            return bounceHeights[Mathf.Min(index, bounceHeights.Length - 1)];

        return 0.2f;
    }

    float GetBounceDuration(int index)
    {
        if (bounceDurations != null && bounceDurations.Length > 0)
            return bounceDurations[Mathf.Min(index, bounceDurations.Length - 1)];

        return 0.15f;
    }

    Vector3 GetBounceOffset()
    {
        Vector3 side = Vector3.Cross(Vector3.up, moveDirection).normalized;
        Vector3 sideOffset = side * UnityEngine.Random.Range(-bounceSideRandom, bounceSideRandom);
        float forwardJitter = UnityEngine.Random.Range(-0.03f, 0.05f);
        return sideOffset + moveDirection * forwardJitter;
    }

    Vector3 GetSideScatter()
    {
        Vector3 side = Vector3.Cross(Vector3.up, moveDirection).normalized;
        return side * UnityEngine.Random.Range(-sideScatter, sideScatter);
    }

    Quaternion GetUprightRotation(Quaternion sourceRotation)
    {
        Vector3 flatForward = Vector3.ProjectOnPlane(sourceRotation * Vector3.forward, Vector3.up);
        if (flatForward.sqrMagnitude < 0.0001f)
            flatForward = Vector3.forward;

        return Quaternion.LookRotation(flatForward.normalized, Vector3.up);
    }

    Quaternion GetTargetRotation(int face)
    {
        switch (face)
        {
            case 1: return Quaternion.identity;
            case 2: return Quaternion.Euler(0f, 0f, -90f);
            case 3: return Quaternion.Euler(90f, 0f, 0f);
            case 4: return Quaternion.Euler(-90f, 0f, 0f);
            case 5: return Quaternion.Euler(0f, 0f, 90f);
            case 6: return Quaternion.Euler(180f, 0f, 0f);
            default: return Quaternion.identity;
        }
    }

    Vector3 GetPlanarDirection(Vector3 direction)
    {
        Vector3 planar = new Vector3(direction.x, 0f, direction.z);
        if (planar.sqrMagnitude < 0.0001f)
            planar = Vector3.forward;

        return planar.normalized;
    }

    Vector3 EvaluateBounceScale(float t)
    {
        if (t < 0.22f)
            return Vector3.LerpUnclamped(GetSquashScale(squashAmount * 0.7f), GetStretchScale(stretchAmount * 0.35f), t / 0.22f);

        return Vector3.LerpUnclamped(GetStretchScale(stretchAmount * 0.35f), baseScale, (t - 0.22f) / 0.78f);
    }

    Vector3 GetSquashScale(float amount)
    {
        return Vector3.Scale(baseScale, new Vector3(1f + amount, 1f - amount, 1f + amount));
    }

    Vector3 GetStretchScale(float amount)
    {
        return Vector3.Scale(baseScale, new Vector3(1f - amount * 0.5f, 1f + amount, 1f - amount * 0.5f));
    }

    void ApplyScale(Vector3 localScale)
    {
        if (visualRoot != null)
            visualRoot.localScale = localScale;
    }

    void EnsureBody()
    {
        if (body == null)
            body = GetComponent<Rigidbody>();
    }

    static float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }

    static float EaseOutCubic(float t)
    {
        float u = 1f - t;
        return 1f - u * u * u;
    }
}
