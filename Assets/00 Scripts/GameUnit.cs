using System;
using Spine;
using Spine.Unity;
using UnityEngine;

public abstract class GameUnit : MonoBehaviour
{
    public int hp;
    public int currentHp;
    public HPBar hpBar;
    public SkeletonGraphic skeletonGraphic;
    public SkeletonAnimation skeletonAnimation;
    public EffectManager effectManager;

    [Header("anim")]
    public string idleAnim = "Idle";
    public string attackAnim = "Attack";
    public string dieAnim = "Die";
    public string hurtAnim = "Hurt";
    [Header("Combat")]
    public float aimAttackSpeed = 1f;

    [Header("HP Bar Layout")]
    [SerializeField] bool autoLayoutHpBarToSpine = true;
    [SerializeField] Vector2 hpBarScreenOffset = new Vector2(0f, 24f);
    [SerializeField] float hpBarWidthScale = 0.85f;
    [SerializeField] float hpBarMinWidth = 90f;
    [SerializeField] float hpBarMaxWidth = 260f;

    [Header("Floating Damage Text")]
    [SerializeField] Vector2 damageTextScreenOffset = new Vector2(0f, 48f);
    [SerializeField] Color damageTextColor = new Color(1f, 0.2f, 0.08f, 1f);
    [SerializeField] Color criticalDamageTextColor = Color.yellow;

    public event Action<GameUnit, int, int> OnHpChanged;
    public event Action<GameUnitDamageEvent> OnBeforeDamage;
    public event Action<GameUnit, int> OnDamaged;
    public event Action<GameUnit, int> OnHealed;
    public event Action<GameUnit> OnTurnStarted;
    public event Action<GameUnit> OnDied;

    protected TrackEntry currentTrack;
    bool deathNotified;

    protected virtual void Awake()
    {
        effectManager = GetComponent<EffectManager>();
        if (effectManager == null)
            effectManager = gameObject.AddComponent<EffectManager>();

        if (skeletonGraphic == null)
            skeletonGraphic = GetComponentInChildren<SkeletonGraphic>();

        if (skeletonAnimation == null)
            skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();

        OnHpChanged += UpdateHpBar;
    }

    protected virtual void OnDestroy()
    {
        OnHpChanged -= UpdateHpBar;
    }

    public virtual void SetHealth(int maxHp, int newCurrentHp)
    {
        hp = Mathf.Max(0, maxHp);
        currentHp = Mathf.Clamp(newCurrentHp, 0, hp);
        NotifyHpChanged();
    }

    public virtual bool IsAlive()
    {
        return currentHp > 0;
    }

    public virtual void OnTakeDamage(int amount)
    {
        OnTakeDamage(amount, false);
    }

    public virtual void OnTakeDamage(int amount, bool isCritical)
    {
        if (amount <= 0 || !IsAlive())
            return;

        GameUnitDamageEvent damageEvent = new(this, amount);
        OnBeforeDamage?.Invoke(damageEvent);

        if (damageEvent.Cancelled || damageEvent.Amount <= 0)
            return;

        amount = damageEvent.Amount;

        currentHp = Mathf.Max(0, currentHp - amount);
        ShowDamageFloatingText(amount, isCritical);
        OnDamaged?.Invoke(this, amount);
        TigerForge.EventManager.EmitEventData(
            Constant.ON_UNIT_DAMAGED,
            new GameUnitAmountEventData(this, amount)
        );
        NotifyHpChanged();

        if (currentHp <= 0)
        {
            OnDie();
            return;
        }

        PlayHurtAnimation();
    }

    public virtual void OnHeal(int amount)
    {
        if (amount <= 0 || !IsAlive())
            return;

        currentHp = Mathf.Min(hp, currentHp + amount);
        OnHealed?.Invoke(this, amount);
        TigerForge.EventManager.EmitEventData(
            Constant.ON_UNIT_HEALED,
            new GameUnitAmountEventData(this, amount)
        );
        NotifyHpChanged();
    }

    public virtual TrackEntry PlayAnimation(string animName, bool loop = false)
    {
        if (skeletonGraphic != null && skeletonGraphic.AnimationState != null)
        {
            animName = AnimationNameUtility.ResolveAnimationName(
                skeletonGraphic.Skeleton?.Data?.Animations,
                animName
            );

            currentTrack = skeletonGraphic.AnimationState.SetAnimation(0, animName, loop);
            return currentTrack;
        }

        if (skeletonAnimation != null && skeletonAnimation.AnimationState != null)
        {
            animName = AnimationNameUtility.ResolveAnimationName(
                skeletonAnimation.Skeleton?.Data?.Animations,
                animName
            );

            currentTrack = skeletonAnimation.AnimationState.SetAnimation(0, animName, loop);
            return currentTrack;
        }

        return null;
    }

    public virtual void OnDie()
    {
        NotifyDied();
        PlayAnimation(dieAnim, false);
    }

    public void BeginTurn()
    {
        if (IsAlive())
        {
            OnTurnStarted?.Invoke(this);
            TigerForge.EventManager.EmitEventData(
                Constant.ON_UNIT_TURN_STARTED,
                this
            );
        }
    }

    protected void NotifyHpChanged()
    {
        OnHpChanged?.Invoke(this, currentHp, hp);
        TigerForge.EventManager.EmitEventData(
            Constant.ON_UNIT_HP_CHANGED,
            new GameUnitHpEventData(this, currentHp, hp)
        );
    }

    protected void NotifyDied()
    {
        if (deathNotified)
            return;

        deathNotified = true;
        OnDied?.Invoke(this);
        TigerForge.EventManager.EmitEventData(
            Constant.ON_UNIT_DIED,
            this
        );
    }

    protected virtual void PlayHurtAnimation()
    {
        PlayAnimation(hurtAnim, false);
        QueueIdleAnimation();
    }

    protected void QueueIdleAnimation()
    {
        if (skeletonGraphic != null && skeletonGraphic.AnimationState != null)
        {
            skeletonGraphic.AnimationState.AddAnimation(
                0,
                AnimationNameUtility.ResolveAnimationName(
                    skeletonGraphic.Skeleton?.Data?.Animations,
                    idleAnim
                ),
                true,
                0
            );
            return;
        }

        if (skeletonAnimation != null && skeletonAnimation.AnimationState != null)
        {
            skeletonAnimation.AnimationState.AddAnimation(
                0,
                AnimationNameUtility.ResolveAnimationName(
                    skeletonAnimation.Skeleton?.Data?.Animations,
                    idleAnim
                ),
                true,
                0
            );
        }
    }

    protected virtual void SetVisualAlpha(float alpha)
    {
        if (skeletonGraphic != null)
        {
            Color color = skeletonGraphic.color;
            color.a = alpha;
            skeletonGraphic.color = color;
        }

        if (skeletonAnimation != null && skeletonAnimation.Skeleton != null)
        {
            Color color = skeletonAnimation.Skeleton.GetColor();
            color.a = alpha;
            skeletonAnimation.Skeleton.SetColor(color);
        }
    }

    void UpdateHpBar(GameUnit unit, int current, int max)
    {
        if (hpBar != null)
            hpBar.SetHp(current, max);
    }

    void ShowDamageFloatingText(int amount, bool isCritical)
    {
        if (amount <= 0 || DiceManager.Instance == null)
            return;

        if (!TryGetSpineScreenBounds(out Rect screenBounds))
            return;

        Vector2 screenPosition = new Vector2(
            screenBounds.center.x + damageTextScreenOffset.x,
            screenBounds.yMax + damageTextScreenOffset.y
        );

        Color textColor = isCritical ? criticalDamageTextColor : damageTextColor;
        DiceManager.Instance.SpawnFloatingTextDmg(screenPosition, $"-{amount}", textColor);
    }

    protected void RefreshHpBarLayout()
    {
        RefreshHpBarLayout(Vector2.zero);
    }

    protected void RefreshHpBarLayout(Vector2 extraScreenOffset)
    {
        if (!autoLayoutHpBarToSpine || hpBar == null)
            return;

        PrepareSpineBoundsMeasurement();

        RectTransform hpBarRect = hpBar.transform as RectTransform;
        RectTransform parentRect = hpBarRect != null ? hpBarRect.parent as RectTransform : null;
        if (hpBarRect == null || parentRect == null)
            return;

        if (!TryGetSpineScreenBounds(out Rect screenBounds))
            return;

        Canvas canvas = hpBarRect.GetComponentInParent<Canvas>();
        Camera canvasCamera = GetCanvasCamera(canvas);
        Vector2 screenPosition = new Vector2(
            screenBounds.center.x + hpBarScreenOffset.x + extraScreenOffset.x,
            screenBounds.yMax + hpBarScreenOffset.y + extraScreenOffset.y
        );

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(parentRect, screenPosition, canvasCamera, out Vector3 worldPosition))
            hpBarRect.position = worldPosition;

    }

    void PrepareSpineBoundsMeasurement()
    {
        if (skeletonGraphic != null)
        {
            skeletonGraphic.Update(0f);
            skeletonGraphic.UpdateMesh();
        }

        Canvas.ForceUpdateCanvases();
    }

    bool TryGetSpineScreenBounds(out Rect screenBounds)
    {
        if (TryGetSkeletonGraphicScreenBounds(out screenBounds))
            return true;

        if (TryGetSkeletonAnimationScreenBounds(out screenBounds))
            return true;

        screenBounds = default;
        return false;
    }

    bool TryGetSkeletonGraphicScreenBounds(out Rect screenBounds)
    {
        screenBounds = default;
        if (skeletonGraphic == null)
            return false;

        RectTransform rectTransform = skeletonGraphic.rectTransform;
        if (rectTransform == null)
            return false;

        Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
        Camera camera = GetCanvasCamera(canvas);

        Bounds meshBounds = skeletonGraphic.MeshGenerator.GetMeshBounds();
        if (meshBounds.size.sqrMagnitude > 0f)
        {
            Vector3 min = meshBounds.min;
            Vector3 max = meshBounds.max;
            Vector3[] meshCorners =
            {
                rectTransform.TransformPoint(new Vector3(min.x, min.y, 0f)),
                rectTransform.TransformPoint(new Vector3(min.x, max.y, 0f)),
                rectTransform.TransformPoint(new Vector3(max.x, min.y, 0f)),
                rectTransform.TransformPoint(new Vector3(max.x, max.y, 0f))
            };

            if (TryBuildScreenBounds(meshCorners, camera, out screenBounds))
                return true;
        }

        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        return TryBuildScreenBounds(corners, camera, out screenBounds);
    }

    bool TryGetSkeletonAnimationScreenBounds(out Rect screenBounds)
    {
        screenBounds = default;
        if (skeletonAnimation == null)
            return false;

        Renderer renderer = skeletonAnimation.GetComponent<Renderer>();
        Camera camera = Camera.main;
        if (renderer == null || camera == null)
            return false;

        Bounds bounds = renderer.bounds;
        if (bounds.size.sqrMagnitude <= 0f)
            return false;

        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        Vector3[] corners =
        {
            new Vector3(min.x, min.y, min.z),
            new Vector3(min.x, min.y, max.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(min.x, max.y, max.z),
            new Vector3(max.x, min.y, min.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(max.x, max.y, max.z)
        };

        return TryBuildScreenBounds(corners, camera, out screenBounds);
    }

    bool TryBuildScreenBounds(Vector3[] worldCorners, Camera camera, out Rect screenBounds)
    {
        screenBounds = default;
        if (worldCorners == null || worldCorners.Length == 0)
            return false;

        float minX = float.PositiveInfinity;
        float minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float maxY = float.NegativeInfinity;

        for (int i = 0; i < worldCorners.Length; i++)
        {
            Vector3 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, worldCorners[i]);
            minX = Mathf.Min(minX, screenPoint.x);
            minY = Mathf.Min(minY, screenPoint.y);
            maxX = Mathf.Max(maxX, screenPoint.x);
            maxY = Mathf.Max(maxY, screenPoint.y);
        }

        if (!float.IsFinite(minX) || !float.IsFinite(minY) || !float.IsFinite(maxX) || !float.IsFinite(maxY))
            return false;

        screenBounds = Rect.MinMaxRect(minX, minY, maxX, maxY);
        return screenBounds.width > 0f && screenBounds.height > 0f;
    }

    Camera GetCanvasCamera(Canvas canvas)
    {
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }
}

public sealed class GameUnitHpEventData
{
    public GameUnit Unit { get; }
    public int CurrentHp { get; }
    public int MaxHp { get; }

    public GameUnitHpEventData(GameUnit unit, int currentHp, int maxHp)
    {
        Unit = unit;
        CurrentHp = currentHp;
        MaxHp = maxHp;
    }
}

public sealed class GameUnitAmountEventData
{
    public GameUnit Unit { get; }
    public int Amount { get; }

    public GameUnitAmountEventData(GameUnit unit, int amount)
    {
        Unit = unit;
        Amount = amount;
    }
}

public sealed class GameUnitDamageEvent
{
    public GameUnit Target { get; }
    public int Amount { get; set; }
    public bool Cancelled { get; private set; }

    public GameUnitDamageEvent(GameUnit target, int amount)
    {
        Target = target;
        Amount = Mathf.Max(0, amount);
    }

    public void Cancel()
    {
        Cancelled = true;
        Amount = 0;
    }
}
