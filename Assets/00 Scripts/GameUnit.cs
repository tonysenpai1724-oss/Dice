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
    public EffectManager effectManager;

    [Header("anim")]
    public string idleAnim = "Idle";
    public string attackAnim = "Attack";
    public string dieAnim = "Die";
    public string hurtAnim = "Hurt";
    [Header("Combat")]
    public float aimAttackSpeed = 1f;

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
        if (amount <= 0 || !IsAlive())
            return;

        GameUnitDamageEvent damageEvent = new(this, amount);
        OnBeforeDamage?.Invoke(damageEvent);

        if (damageEvent.Cancelled || damageEvent.Amount <= 0)
            return;

        amount = damageEvent.Amount;

        currentHp = Mathf.Max(0, currentHp - amount);
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
        if (skeletonGraphic == null)
            return null;

        animName = AnimationNameUtility.ResolveAnimationName(
            skeletonGraphic.Skeleton?.Data?.Animations,
            animName
        );

        currentTrack = skeletonGraphic.AnimationState.SetAnimation(
            0,
            animName,
            loop
        );

        return currentTrack;
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
        if (skeletonGraphic == null)
            return;

        skeletonGraphic.AnimationState.AddAnimation(
            0,
            AnimationNameUtility.ResolveAnimationName(
                skeletonGraphic.Skeleton?.Data?.Animations,
                idleAnim
            ),
            true,
            0
        );
    }

    void UpdateHpBar(GameUnit unit, int current, int max)
    {
        if (hpBar != null)
            hpBar.SetHp(current, max);
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

