using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DiceComboConfig
{
    public float maxComboDistance = 4f;
    public float comboArcHeight = 4f;
    public float comboDuration = 0.7f;
    public float comboSideScatter = 0.45f;
    public Vector2 comboSpinTurnsX = new Vector2(1265f, 2530f);
    public Vector2 comboSpinTurnsY = new Vector2(210f, 630f);
    public Vector2 comboSpinTurnsZ = new Vector2(1265f, 2530f);
    public float comboDistancePerChain = 4f;
    public float maxComboDistanceLimit = 30f;
    public float comboArcPerChain = 3f;
    public float maxComboArcHeight = 12f;
    public float comboDurationPerChain = 0.03f;
    public float maxComboDuration = 0.75f;
    public float diceSpacingRadius = 0.95f;
    public float landingHopRatio = 0.36f;
    public float landingHopRatioShort = 0.26f;
    public float landingHopHeightFactor = 0.22f;
    public float landingHopMaxHeight = 0.85f;
    public float landingSideCarry = 0.18f;
    public float landingRollMaxHeight = 0.05f;
    public float landingSecondHopRatio = 0.7f;
    public float landingSecondHopHeightFactor = 0.4f;
    public float landingSecondHopMaxHeight = 0.45f;
    public float landingCarryPitch = 28f;
    public float landingCarryRoll = 12f;
    public float landingCarryYaw = 10f;
    public float landingSecondHopSpinMin = 10f;
    public float landingSecondHopSpinMax = 28f;
    public float landingHopApexTime = 0.42f;
    public float landingHopHangTime = 0.18f;
    public float landingSecondHopApexTime = 0.4f;
    public float landingLaunchPitch = -16f;
    public float landingLaunchRoll = 8f;
}

public class DiceComboService
{
    static readonly WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();

    readonly BoardService boardService;
    readonly DiceComboConfig config;
    readonly Func<List<Dice>> getBoardDices;
    readonly Func<Dice, Dice, bool> tryMerge;
    readonly Action<IEnumerator> runCoroutine;
    readonly Dictionary<Dice, int> comboChainMap = new Dictionary<Dice, int>();
    readonly Dictionary<Dice, float> comboLastTime = new Dictionary<Dice, float>();

    public DiceComboService(
        BoardService boardService,
        DiceComboConfig config,
        Func<List<Dice>> getBoardDices,
        Func<Dice, Dice, bool> tryMerge,
        Action<IEnumerator> runCoroutine)
    {
        this.boardService = boardService;
        this.config = config;
        this.getBoardDices = getBoardDices;
        this.tryMerge = tryMerge;
        this.runCoroutine = runCoroutine;
    }

    public Dictionary<Dice, int> ComboChainMap => comboChainMap;
    public Dictionary<Dice, float> ComboLastTime => comboLastTime;

    public void TryComboChain(Dice dice)
    {
        if (dice == null)
            return;

        Dice target = FindNearestSameLevelDice(dice);

        if (target == null)
        {
            Vector3 randomTargetPos = boardService.FindRandomClearPositionWithinRadius(
                dice.transform.position,
                config.maxComboDistance,
                dice);

            Vector3 randomDir = randomTargetPos - dice.transform.position;
            randomDir.y = 0f;
            randomDir = randomDir.sqrMagnitude < 0.001f ? Vector3.forward : randomDir.normalized;

            runCoroutine?.Invoke(ComboJumpRoutine(dice, null, randomTargetPos, randomDir, true));
            return;
        }

        Vector3 dir = (target.transform.position - dice.transform.position).normalized;
        int comboCount = comboChainMap.TryGetValue(dice, out int chain) ? chain : 1;

        float dynamicMaxComboDistance = Mathf.Min(
            config.maxComboDistance + comboCount * config.comboDistancePerChain,
            config.maxComboDistanceLimit);

        float dist = Vector3.Distance(dice.transform.position, target.transform.position);
        Vector3 targetPos = dist > dynamicMaxComboDistance
            ? dice.transform.position + dir * dynamicMaxComboDistance
            : target.transform.position;

        targetPos.y = boardService.GetBoardSurfaceY();
        runCoroutine?.Invoke(ComboJumpRoutine(dice, target, targetPos, dir, dist > dynamicMaxComboDistance));
    }

    public Dice FindNearestSameLevelDice(Dice source)
    {
        if (source == null)
            return null;

        List<Dice> boardDices = getBoardDices?.Invoke();
        if (boardDices == null)
            return null;

        Dice nearest = null;
        float best = Mathf.Infinity;

        for (int i = 0; i < boardDices.Count; i++)
        {
            Dice dice = boardDices[i];
            if (dice == null || dice == source || !dice.gameObject.activeInHierarchy)
                continue;

            if (dice.Level != source.Level)
                continue;

            if (dice.state == DiceState.Merging || dice.state == DiceState.FlyingCombo)
                continue;

            float dist = Vector3.Distance(source.transform.position, dice.transform.position);
            if (dist < best)
            {
                best = dist;
                nearest = dice;
            }
        }

        return nearest;
    }

    bool ShouldSwitchToPhysics(Dice dice, Vector3 currentPosition, Vector3 nextPosition, float radius = 0.55f)
    {
        if (dice == null)
            return false;

        Vector3 direction = nextPosition - currentPosition;
        float distance = direction.magnitude;
        if (distance <= 0.001f)
            return false;

        direction /= distance;
        RaycastHit[] hits = Physics.SphereCastAll(
            currentPosition + Vector3.up * 0.05f,
            radius,
            direction,
            distance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide
        );

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null)
                continue;

            Dice other = hitCollider.GetComponentInParent<Dice>();
            if (other == null || other == dice)
                continue;

            if (!other.gameObject.activeInHierarchy)
                continue;

            return true;
        }

        return false;
    }

    static float EaseOutPower(float t, float power)
    {
        t = Mathf.Clamp01(t);
        return 1f - Mathf.Pow(1f - t, power);
    }

    IEnumerator TravelArcLikeMerge(
        Dice dice,
        Vector3 start,
        Vector3 end,
        float boardSurfaceY,
        float duration,
        float arcHeight,
        Vector3 sideOffset,
        Vector3 spinVelocity)
    {
        float t = 0f;

        while (t < 1f)
        {
            if (dice == null)
                yield break;

            t += Time.fixedDeltaTime / Mathf.Max(0.01f, duration);
            float clampedT = Mathf.Clamp01(t);
            float easedT = 1f - Mathf.Pow(1f - clampedT, 2f);

            Vector3 pos = Vector3.Lerp(start, end, easedT);
            float arc = Mathf.Pow(Mathf.Clamp01(Mathf.Sin(clampedT * Mathf.PI)), 0.7f);
            pos.y = boardSurfaceY + arc * arcHeight;
            pos += sideOffset * Mathf.Sin(clampedT * Mathf.PI);

            dice.rb.linearVelocity = Vector3.zero;

            Vector3 rotationStep = spinVelocity * (1f - Mathf.Pow(clampedT, 1.8f)) * Time.fixedDeltaTime;
            dice.rb.MoveRotation(dice.rb.rotation * Quaternion.Euler(rotationStep));
            dice.rb.MovePosition(pos);

            yield return waitForFixedUpdate;
        }
    }

    static Vector3 EvaluateParabolicPosition(
        Vector3 start,
        Vector3 end,
        float progress,
        float boardSurfaceY,
        float arcHeight)
    {
        float clampedProgress = Mathf.Clamp01(progress);
        Vector3 position = Vector3.Lerp(start, end, clampedProgress);
        position.y = boardSurfaceY + Mathf.Sin(clampedProgress * Mathf.PI) * arcHeight;
        return position;
    }

    static Vector3 GetFrameVelocity(Vector3 previousPosition, Vector3 currentPosition)
    {
        return (currentPosition - previousPosition) / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
    }

    static Quaternion AdvanceAirTumbleRotation(
        Quaternion currentRotation,
        Vector3 previousPosition,
        Vector3 currentPosition,
        float tumbleDegreesPerUnit)
    {
        Quaternion nextRotation = currentRotation;
        Vector3 travelDelta = currentPosition - previousPosition;

        if (travelDelta.sqrMagnitude > 0.000001f)
        {
            Vector3 moveDir = travelDelta.normalized;
            Vector3 tumbleAxis = Vector3.Cross(Vector3.up, moveDir).normalized;
            if (tumbleAxis.sqrMagnitude <= 0.001f)
                tumbleAxis = Vector3.Cross(Vector3.right, moveDir).normalized;

            float tumbleDegrees = travelDelta.magnitude * tumbleDegreesPerUnit;
            nextRotation = Quaternion.AngleAxis(tumbleDegrees, tumbleAxis) * nextRotation;
        }

        return nextRotation;
    }

    static Quaternion ApplyImpactLean(
        Quaternion currentRotation,
        Vector3 moveDirection,
        float sideSign,
        float pitchDegrees,
        float rollDegrees)
    {
        Vector3 flatMoveDirection = moveDirection;
        flatMoveDirection.y = 0f;
        flatMoveDirection = flatMoveDirection.sqrMagnitude <= 0.001f ? Vector3.forward : flatMoveDirection.normalized;

        Vector3 rightAxis = Vector3.Cross(Vector3.up, flatMoveDirection).normalized;
        if (rightAxis.sqrMagnitude <= 0.001f)
            rightAxis = Vector3.right;

        Quaternion pitchRotation = Quaternion.AngleAxis(pitchDegrees, rightAxis);
        Quaternion rollRotation = Quaternion.AngleAxis(rollDegrees * sideSign, flatMoveDirection);
        return rollRotation * pitchRotation * currentRotation;
    }

    static float GetImpactBlend(float progress, float impactWindow)
    {
        if (impactWindow <= 0f)
            return 0f;

        float normalized = Mathf.Clamp01(progress / impactWindow);
        return 1f - normalized;
    }

    // ============================================================
    // AdvanceGroundRollRotation - FIXED: Không còn spinning
    // ============================================================
   static Quaternion AdvanceGroundRollRotation(
    Quaternion currentRotation,
    Vector3 previousPosition,
    Vector3 currentPosition,
    float settleBlend,  // 0 → 1
    ref float wobbleTimer,
    ref float wobbleSpeed,
    ref float wobbleMagnitude,
    ref bool isWobbling,
    ref int framesSinceStop)
{
    Quaternion nextRotation = currentRotation;
    Vector3 groundDelta = currentPosition - previousPosition;
    groundDelta.y = 0f;

    // ========================================
    // PHASE 1: ROLL CHẬM DẦN (0 → 0.7)
    // ========================================
    if (settleBlend < 0.7f && groundDelta.sqrMagnitude > 0.0001f)
    {
        Vector3 moveDir = groundDelta.normalized;
        Vector3 rollAxis = Vector3.Cross(Vector3.up, moveDir).normalized;
        
        if (rollAxis.sqrMagnitude > 0.0001f)
        {
            // roll strength GIẢM DẦN VỀ 0
            float rollStrength = Mathf.Lerp(380f, 20f, Mathf.Clamp01(settleBlend * 1.8f));
            float rollDegrees = groundDelta.magnitude * rollStrength;
            
            if (rollDegrees > 0.02f)
            {
                nextRotation = Quaternion.AngleAxis(rollDegrees, rollAxis) * nextRotation;
            }
        }
    }

    // ========================================
    // PHASE 2: MICRO WOBBLE (0.5 → 1.0)
    // ========================================
    if (settleBlend >= 0.5f)
    {
        float wobbleStart = Mathf.Clamp01((settleBlend - 0.5f) / 0.3f); // 0→1 từ 0.5→0.8
        float wobbleEnd = Mathf.Clamp01((settleBlend - 0.8f) / 0.2f);   // 0→1 từ 0.8→1.0
        
        // Bắt đầu wobble khi gần dừng
        if (wobbleStart > 0f)
        {
            // Khởi tạo wobble nếu chưa có
            if (!isWobbling)
            {
                isWobbling = true;
                wobbleTimer = 0f;
                wobbleSpeed = UnityEngine.Random.Range(180f, 320f);
                wobbleMagnitude = UnityEngine.Random.Range(0.6f, 1.8f);
                framesSinceStop = 0;
            }
            
            // Wobble intensity: tăng lên rồi giảm về 0
            float wobbleIntensity = Mathf.Sin(wobbleStart * Mathf.PI) * (1f - wobbleEnd);
            float currentWobbleMag = wobbleMagnitude * wobbleIntensity * 0.08f;
            
            if (currentWobbleMag > 0.001f)
            {
                wobbleTimer += Time.fixedDeltaTime;
                framesSinceStop = 0;
                
                // Tạo wobble bằng cách xoay quanh 2 trục ngẫu nhiên
                Vector3 wobbleAxis1 = Vector3.Cross(Vector3.up, 
                    new Vector3(Mathf.Sin(wobbleTimer * 0.7f), 0, Mathf.Cos(wobbleTimer * 0.5f))).normalized;
                Vector3 wobbleAxis2 = Vector3.Cross(wobbleAxis1, Vector3.up).normalized;
                
                float wobbleAngle1 = Mathf.Sin(wobbleTimer * wobbleSpeed) * currentWobbleMag;
                float wobbleAngle2 = Mathf.Cos(wobbleTimer * wobbleSpeed * 0.7f + 1.2f) * currentWobbleMag * 0.6f;
                
                Quaternion wobbleRot = Quaternion.AngleAxis(wobbleAngle1, wobbleAxis1) *
                                       Quaternion.AngleAxis(wobbleAngle2, wobbleAxis2);
                
                nextRotation = wobbleRot * nextRotation;
            }
            else
            {
                // Wobble đã tắt hẳn, bắt đầu đếm frame
                framesSinceStop++;
            }
        }
    }

    return nextRotation;
}
static Quaternion GetNearestStableDiceRotation(Quaternion currentRotation)
{
    Vector3[] localFaceAxes = {
        Vector3.right, Vector3.left,
        Vector3.up, Vector3.down,
        Vector3.forward, Vector3.back
    };

    Vector3 bestLocalDown = Vector3.down;
    float bestDot = -2f;

    for (int i = 0; i < localFaceAxes.Length; i++)
    {
        Vector3 worldAxis = currentRotation * localFaceAxes[i];
        float dot = Vector3.Dot(worldAxis, Vector3.down);
        if (dot > bestDot)
        {
            bestDot = dot;
            bestLocalDown = localFaceAxes[i];
        }
    }

    Vector3 currentWorldDown = currentRotation * bestLocalDown;
    Quaternion fullAlignment = GetShortestArcRotation(currentWorldDown, Vector3.down);
    return fullAlignment * currentRotation;
}

 // ============================================================
static Quaternion GetShortestArcRotation(Vector3 fromDirection, Vector3 toDirection)
{
    Vector3 from = fromDirection.normalized;
    Vector3 to = toDirection.normalized;
    float dot = Mathf.Clamp(Vector3.Dot(from, to), -1f, 1f);

    if (dot > 0.999999f)
        return Quaternion.identity;

    if (dot < -0.999999f)
    {
        Vector3 fallbackAxis = Vector3.Cross(from, Vector3.right);
        if (fallbackAxis.sqrMagnitude < 0.0001f)
            fallbackAxis = Vector3.Cross(from, Vector3.forward);
        fallbackAxis.Normalize();
        return Quaternion.AngleAxis(180f, fallbackAxis);
    }

    Vector3 axis = Vector3.Cross(from, to).normalized;
    float angleDegrees = Mathf.Acos(dot) * Mathf.Rad2Deg;
    return Quaternion.AngleAxis(angleDegrees, axis);
}

    static Quaternion ApplyStableFaceSettle(
        Quaternion currentRotation,
        float settleBlend,
        ref Quaternion lockedTargetRotation,
        ref bool hasLockedTarget)
    {
        if (settleBlend <= 0f)
            return currentRotation;

        // CHỈ tính target 1 LẦN DUY NHẤT
        if (!hasLockedTarget)
        {
            lockedTargetRotation = GetNearestStableDiceRotation(currentRotation);
            hasLockedTarget = true;
        }

        float remainingAngle = Quaternion.Angle(currentRotation, lockedTargetRotation);
        if (remainingAngle <= 0.25f)
            return lockedTargetRotation;

        float settleResponse = Mathf.Lerp(0.45f, 0.95f, Mathf.Clamp01(settleBlend));
        float maxDegreesPerFrame = remainingAngle * settleResponse;

        return Quaternion.RotateTowards(currentRotation, lockedTargetRotation, maxDegreesPerFrame);
    }

    // ============================================================
    // CalculateLandingRotation - GIỮ NGUYÊN
    // ============================================================
    private Quaternion CalculateLandingRotation(Dice dice, Vector3 fromPosition, Vector3 toPosition, float boardSurfaceY)
    {
        Vector3 moveDir = (toPosition - fromPosition);
        moveDir.y = 0f;
        moveDir = moveDir.sqrMagnitude < 0.001f ? Vector3.forward : moveDir.normalized;
        
        Vector3[] possibleDownFaces = {
            Vector3.down,
            Vector3.up,
            Vector3.forward,
            Vector3.back,
            Vector3.right,
            Vector3.left
        };
        
        int faceIndex = 0;
        float bestAlignment = -1f;
        
        for (int i = 0; i < possibleDownFaces.Length; i++)
        {
            Vector3 faceWorld = -possibleDownFaces[i];
            float alignment = Vector3.Dot(faceWorld, moveDir);
            
            if (alignment > bestAlignment)
            {
                bestAlignment = alignment;
                faceIndex = i;
            }
        }
        
        if (UnityEngine.Random.value < 0.3f)
        {
            int randomIndex = UnityEngine.Random.Range(0, possibleDownFaces.Length);
            if (randomIndex != faceIndex)
                faceIndex = randomIndex;
        }
        
        Vector3 chosenDownFace = possibleDownFaces[faceIndex];
        Quaternion baseRotation = Quaternion.FromToRotation(chosenDownFace, Vector3.down);
        
        float randomYaw = UnityEngine.Random.Range(0f, 360f);
        Quaternion yawRotation = Quaternion.AngleAxis(randomYaw, Vector3.up);
        
        Quaternion finalRotation = yawRotation * baseRotation;
        
        Vector3 currentForward = finalRotation * Vector3.forward;
        Vector3 flatForward = currentForward;
        flatForward.y = 0f;
        flatForward.Normalize();
        
        if (flatForward.sqrMagnitude > 0.001f)
        {
            float angleToMove = Vector3.SignedAngle(flatForward, moveDir, Vector3.up);
            if (Mathf.Abs(angleToMove) > 5f)
            {
                Quaternion yawCorrection = Quaternion.AngleAxis(angleToMove * 0.3f, Vector3.up);
                finalRotation = yawCorrection * finalRotation;
            }
        }
        
        return finalRotation;
    }

    // ============================================================
    // ComboJumpRoutine - Phase 4 FIXED
    // ============================================================
    public IEnumerator ComboJumpRoutine(Dice dice, Dice target, Vector3 targetPos, Vector3 dir, bool shouldFullBounce)
    {
        if (dice == null) yield break;

        dice.state = DiceState.FlyingCombo;
        dice.canMerge = true;
        dice.SetCollisionEnabled(true);
        dice.rb.isKinematic = false;
        dice.ApplyFlyingConstraints();
        dice.rb.linearVelocity = Vector3.zero;
        dice.rb.angularVelocity = Vector3.zero;
        dice.rb.Sleep();

        Vector3 start = dice.transform.position;
        Vector3 finalDestination = targetPos;
        float boardSurfaceY = boardService.GetBoardSurfaceY();
        finalDestination.y = boardSurfaceY;

        bool canAimForTarget = target != null &&
                               target.gameObject.activeInHierarchy &&
                               target.Level == dice.Level &&
                               !target.isMerging;

        if (!canAimForTarget)
            finalDestination = boardService.FindClearPosition(finalDestination, dice, config.diceSpacingRadius);

        Vector3 jumpDir = dir.sqrMagnitude > 0.001f ? dir.normalized : Vector3.forward;
        float comboCount = comboChainMap.TryGetValue(dice, out int currentChain) ? currentChain + 1 : 1;
        comboChainMap[dice] = (int)comboCount;
        comboLastTime[dice] = Time.time;

        float dynamicDuration = Mathf.Min(config.comboDuration + comboCount * config.comboDurationPerChain, config.maxComboDuration);
        float dynamicArcHeight = Mathf.Min(config.comboArcHeight + comboCount * config.comboArcPerChain, config.maxComboArcHeight);

        float totalDist = Vector3.Distance(start, finalDestination);
        float bounceDist = shouldFullBounce ? Mathf.Min(3.2f, totalDist * 0.6f) : Mathf.Min(1.5f, totalDist * 0.3f);

        Vector3 impactPoint1 = finalDestination - jumpDir * bounceDist;
        impactPoint1.y = boardSurfaceY;

        Vector3 sideOffset = Vector3.Cross(Vector3.up, jumpDir) * UnityEngine.Random.Range(-config.comboSideScatter, config.comboSideScatter);

        Vector3 spinVelocity = new Vector3(
            UnityEngine.Random.Range(360, 720),
            UnityEngine.Random.Range(90, 360),
            UnityEngine.Random.Range(360, 720));

        float t = 0f;
        bool merged = false;
        float mergeDistance = Mathf.Max(1.2f, config.diceSpacingRadius * 1.25f);

        // ========================================================================
        // PHA 1: BAY CHÍNH TRÊN KHÔNG
        // ========================================================================
        while (t < 1f)
        {
            if (dice == null) yield break;

            t += Time.fixedDeltaTime / Mathf.Max(0.01f, dynamicDuration);
            float clampedT = Mathf.Clamp01(t);
            float easedT = 1f - Mathf.Pow(1f - clampedT, 2f);

            Vector3 pos = Vector3.Lerp(start, impactPoint1, easedT);
            float arc = Mathf.Pow(Mathf.Clamp01(Mathf.Sin(clampedT * Mathf.PI)), 0.7f);
            pos.y = boardSurfaceY + arc * dynamicArcHeight;
            pos += sideOffset * Mathf.Sin(clampedT * Mathf.PI);

            dice.rb.linearVelocity = Vector3.zero;

            Vector3 rotationStep = spinVelocity * (1f - Mathf.Pow(clampedT, 1.8f)) * Time.fixedDeltaTime;
            dice.rb.MoveRotation(dice.rb.rotation * Quaternion.Euler(rotationStep));
            
            dice.rb.MovePosition(pos);

            if (target != null && target.gameObject.activeInHierarchy)
            {
                float distToTarget = Vector3.Distance(pos, target.transform.position);
                if (distToTarget <= mergeDistance && target.Level == dice.Level && !target.isMerging && !dice.isMerging)
                {
                    comboChainMap[target] = (int)comboCount;
                    if (tryMerge != null && tryMerge.Invoke(dice, target))
                    {
                        merged = true;
                        break;
                    }
                }
            }

            yield return waitForFixedUpdate;
        }

        if (merged || dice == null) yield break;

        comboChainMap.Remove(dice);
        if (target != null) comboChainMap.Remove(target);

        finalDestination = boardService.FindClearPosition(finalDestination, dice, config.diceSpacingRadius);
        finalDestination.y = boardSurfaceY;

        // ========================================================================
        // PHA 2: BOUNCE 1
        // ========================================================================
        Vector3 impactTravelDir = impactPoint1 - start;
        impactTravelDir.y = 0f;
        impactTravelDir = impactTravelDir.sqrMagnitude <= 0.001f ? jumpDir : impactTravelDir.normalized;

        float bounceSideSign = UnityEngine.Random.value < 0.5f ? -1f : 1f;
        float bounceAngle = UnityEngine.Random.Range(28f, 58f) * bounceSideSign;
        Vector3 bounce1Dir = Quaternion.AngleAxis(bounceAngle, Vector3.up) * impactTravelDir;
        bounce1Dir.y = 0f;
        bounce1Dir = bounce1Dir.sqrMagnitude <= 0.001f ? impactTravelDir : bounce1Dir.normalized;

        float remainingDistance = Vector3.Distance(impactPoint1, finalDestination);
        float bounce1Distance = shouldFullBounce
            ? Mathf.Clamp(remainingDistance * 0.62f, 1.2f, 2.6f)
            : Mathf.Clamp(remainingDistance * 0.48f, 0.55f, 1.35f);

        Vector3 impactPoint2 = impactPoint1 + bounce1Dir * bounce1Distance;
        impactPoint2.y = boardSurfaceY;

        Vector3 toFinalDir = finalDestination - impactPoint2;
        toFinalDir.y = 0f;
        if (toFinalDir.sqrMagnitude <= 0.001f)
            toFinalDir = Vector3.Lerp(-bounce1Dir, impactTravelDir, 0.5f);
        toFinalDir.Normalize();

        float bounce2DistanceToStop = shouldFullBounce
            ? Mathf.Clamp(Vector3.Distance(impactPoint2, finalDestination) * 0.38f, 0.24f, 0.85f)
            : Mathf.Clamp(Vector3.Distance(impactPoint2, finalDestination) * 0.28f, 0.12f, 0.45f);
        Vector3 impactPoint3 = finalDestination - toFinalDir * bounce2DistanceToStop;
        impactPoint3.y = boardSurfaceY;

        float bounce1Duration = shouldFullBounce ? 0.36f : 0.24f;
        float bounce1Height = shouldFullBounce
            ? Mathf.Clamp(dynamicArcHeight * 0.32f, 0.95f, 1.75f)
            : Mathf.Clamp(dynamicArcHeight * 0.2f, 0.35f, 0.7f);
        float bounce1Timer = 0f;
        Vector3 bounce1ImpactPush = impactPoint1 + impactTravelDir * 0.1f;
        Vector3 previousBounce1Point = impactPoint1;

        while (bounce1Timer < 1f)
        {
            if (dice == null) yield break;

            bounce1Timer += Time.fixedDeltaTime / bounce1Duration;
            float progress = Mathf.Clamp01(bounce1Timer);

            Vector3 targetPosStep;
            if (progress < 0.12f)
            {
                float impactProgress = progress / 0.12f;
                targetPosStep = Vector3.Lerp(impactPoint1, bounce1ImpactPush, EaseOutPower(impactProgress, 1.6f));
                targetPosStep.y = boardSurfaceY + Mathf.Sin(impactProgress * Mathf.PI) * 0.035f;
            }
            else
            {
                float airProgress = (progress - 0.12f) / 0.88f;
                targetPosStep = EvaluateParabolicPosition(
                    bounce1ImpactPush,
                    impactPoint2,
                    EaseOutPower(airProgress, 1.15f),
                    boardSurfaceY,
                    bounce1Height);
            }

            dice.rb.linearVelocity = GetFrameVelocity(previousBounce1Point, targetPosStep);
            dice.rb.angularVelocity = Vector3.zero;
            dice.rb.MovePosition(targetPosStep);

            Quaternion targetRotation = AdvanceAirTumbleRotation(
                dice.rb.rotation,
                previousBounce1Point,
                targetPosStep,
                58f);

            float bounce1ImpactBlend = GetImpactBlend(progress, 0.22f);
            if (bounce1ImpactBlend > 0f)
            {
                targetRotation = ApplyImpactLean(
                    targetRotation,
                    bounce1Dir,
                    bounceSideSign,
                    20f * bounce1ImpactBlend,
                    26f * bounce1ImpactBlend);
            }

            dice.rb.MoveRotation(targetRotation);
            previousBounce1Point = targetPosStep;
            yield return waitForFixedUpdate;
        }

        // ========================================================================
        // PHA 3: BOUNCE 2
        // ========================================================================
        float bounce2Duration = shouldFullBounce ? 0.2f : 0.16f;
        float bounce2Height = shouldFullBounce ? 0.24f : 0.12f;
        float bounce2Timer = 0f;
        Vector3 bounce2ImpactPush = impactPoint2 + toFinalDir * 0.05f;
        Vector3 previousBounce2Point = impactPoint2;

        while (bounce2Timer < 1f)
        {
            if (dice == null) yield break;

            bounce2Timer += Time.fixedDeltaTime / bounce2Duration;
            float progress = Mathf.Clamp01(bounce2Timer);

            Vector3 targetPosStep;
            if (progress < 0.16f)
            {
                float impactProgress = progress / 0.16f;
                targetPosStep = Vector3.Lerp(impactPoint2, bounce2ImpactPush, EaseOutPower(impactProgress, 1.4f));
                targetPosStep.y = boardSurfaceY + Mathf.Sin(impactProgress * Mathf.PI) * 0.018f;
            }
            else
            {
                float airProgress = (progress - 0.16f) / 0.84f;
                targetPosStep = EvaluateParabolicPosition(
                    bounce2ImpactPush,
                    impactPoint3,
                    EaseOutPower(airProgress, 1.08f),
                    boardSurfaceY,
                    bounce2Height);
            }

            dice.rb.linearVelocity = GetFrameVelocity(previousBounce2Point, targetPosStep);
            dice.rb.angularVelocity = Vector3.zero;
            dice.rb.MovePosition(targetPosStep);

            Quaternion targetRotation = AdvanceAirTumbleRotation(
                dice.rb.rotation,
                previousBounce2Point,
                targetPosStep,
                42f);

            float bounce2ImpactBlend = GetImpactBlend(progress, 0.2f);
            if (bounce2ImpactBlend > 0f)
            {
                targetRotation = ApplyImpactLean(
                    targetRotation,
                    toFinalDir,
                    bounceSideSign,
                    10f * bounce2ImpactBlend,
                    12f * bounce2ImpactBlend);
            }

            dice.rb.MoveRotation(targetRotation);
            previousBounce2Point = targetPosStep;
            yield return waitForFixedUpdate;
        }

 // ========================================================================
// PHA 4: BOUNCE 3 + SETTLE - PHYSICS ONLY + FINAL SNAP
// ========================================================================
float bounce3Duration = shouldFullBounce ? 0.3f : 0.22f;
float bounce3LiftHeight = shouldFullBounce ? 0.025f : 0.015f;
float bounce3Timer = 0f;
Vector3 previousGroundPoint = impactPoint3;

// State cho settle
SettleState settleState;
if (!settleStates.TryGetValue(dice, out settleState))
{
    settleState = new SettleState();
    settleStates[dice] = settleState;
}
// RESET STATE cho jump mới
settleState.isWobbling = false;
settleState.wobbleTimer = 0f;
settleState.wobbleSpeed = UnityEngine.Random.Range(180f, 320f);
settleState.wobbleMagnitude = UnityEngine.Random.Range(0.6f, 1.8f);
settleState.framesSinceStop = 0;

// Không có target rotation trong suốt quá trình
bool snapped = false;

while (bounce3Timer < 1f)
{
    if (dice == null) yield break;
    
    bounce3Timer += Time.fixedDeltaTime / bounce3Duration;
    float progress = Mathf.Clamp01(bounce3Timer);
    float easedBounceProgress = EaseOutPower(progress, 1.6f);
    
    // Position
    Vector3 targetPosStep = Vector3.Lerp(impactPoint3, finalDestination, easedBounceProgress);
    float heightOffset = Mathf.Sin(progress * Mathf.PI) * bounce3LiftHeight * (1f - progress);
    targetPosStep.y = boardSurfaceY + heightOffset;
    
    // Velocity
    dice.rb.linearVelocity = GetFrameVelocity(previousGroundPoint, targetPosStep);
    dice.rb.angularVelocity = Vector3.zero;
    dice.rb.MovePosition(targetPosStep);
    
    // Rotation: settleBlend tăng từ 0→1
    float settleBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 1f, progress));
    
    // CHỈ ROLL + WOBBLE, KHÔNG ROTATE TOWARDS
    Quaternion targetRotation = AdvanceGroundRollRotation(
        dice.rb.rotation,
        previousGroundPoint,
        targetPosStep,
        settleBlend,
        ref settleState.wobbleTimer,
        ref settleState.wobbleSpeed,
        ref settleState.wobbleMagnitude,
        ref settleState.isWobbling,
        ref settleState.framesSinceStop);
    
    dice.rb.MoveRotation(targetRotation);
    previousGroundPoint = targetPosStep;
    
    // ========================================
    // KIỂM TRA ĐIỀU KIỆN SNAP
    // ========================================
    // Snap khi: 
    // 1. Đã gần đến cuối (progress > 0.95)
    // 2. VÀ (đã dừng di chuyển HOẶC wobble đã tắt)
    if (progress > 0.95f && !snapped)
    {
        float distToTarget = Vector3.Distance(targetPosStep, finalDestination);
        bool isAlmostStopped = distToTarget < 0.01f;
        bool wobbleFinished = settleState.framesSinceStop > 3; // 3 frame không wobble
        
        if (isAlmostStopped && wobbleFinished)
        {
            // ========================================
            // SNAP VÀO FRAME CUỐI CÙNG
            // ========================================
            Quaternion finalRotation = GetNearestStableDiceRotation(dice.rb.rotation);
            dice.rb.MoveRotation(finalRotation);
            snapped = true;
            
            // Đánh dấu đã snap để không snap lại
        }
    }
    
    yield return waitForFixedUpdate;
}

// Nếu chưa snap được (hiếm khi xảy ra), snap ở cuối cùng
if (!snapped)
{
    Quaternion finalRotation = GetNearestStableDiceRotation(dice.rb.rotation);
    dice.rb.MoveRotation(finalRotation);
}

// Final: apply constraints và sleep
dice.ApplyGroundedConstraints();
dice.rb.Sleep();
dice.state = DiceState.Idle;
    }

    public IEnumerator RecoverUprightRoutine(Dice dice)
    {
        float duration = 0.35f;
        Rigidbody rigidbody = dice.rb;
        float t = 0f;
        Quaternion startRot = dice.transform.rotation;
        Quaternion targetRot = Quaternion.Euler(0f, dice.transform.eulerAngles.y, 0f);

        while (t < 1f)
        {
            if (dice == null)
                yield break;

            t += Time.deltaTime / duration;
            dice.transform.rotation = Quaternion.Slerp(startRot, targetRot, 1f - Mathf.Pow(1f - t, 3f));
            rigidbody.linearVelocity = Vector3.Lerp(rigidbody.linearVelocity, Vector3.zero, Time.deltaTime * 8f);
            rigidbody.angularVelocity = Vector3.Lerp(rigidbody.angularVelocity, Vector3.zero, Time.deltaTime * 10f);
            yield return null;
        }

        dice.transform.rotation = targetRot;
        rigidbody.linearVelocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
    }

    static Vector3 GetNearestStableDownFace(Quaternion currentRotation)
    {
        Vector3[] localFaceAxes = {
            Vector3.right, Vector3.left,
            Vector3.up, Vector3.down,
            Vector3.forward, Vector3.back
        };
        
        Vector3 bestLocalDown = Vector3.down;
        float bestDot = -2f;
        
        for (int i = 0; i < localFaceAxes.Length; i++)
        {
            Vector3 worldAxis = currentRotation * localFaceAxes[i];
            float dot = Vector3.Dot(worldAxis, Vector3.down);
            if (dot > bestDot)
            {
                bestDot = dot;
                bestLocalDown = localFaceAxes[i];
            }
        }
        
        return bestLocalDown;
    }
private class SettleState
{
    public float wobbleTimer;
    public float wobbleSpeed;
    public float wobbleMagnitude;
    public bool isWobbling;
    public int framesSinceStop;
}

// Dictionary để lưu state cho từng dice
private Dictionary<Dice, SettleState> settleStates = new Dictionary<Dice, SettleState>();

} 