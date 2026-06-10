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

    public event Action<GameUnit, int, int> HpChanged;
    public event Action<GameUnitDamageEvent> BeforeDamage;
    public event Action<GameUnit, int> Damaged;
    public event Action<GameUnit, int> Healed;
    public event Action<GameUnit> TurnStarted;
    public event Action<GameUnit> Died;

    protected TrackEntry currentTrack;
    bool deathNotified;

    protected virtual void Awake()
    {
        effectManager = GetComponent<EffectManager>();
        if (effectManager == null)
            effectManager = gameObject.AddComponent<EffectManager>();

        HpChanged += UpdateHpBar;
    }

    protected virtual void OnDestroy()
    {
        HpChanged -= UpdateHpBar;
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

    public virtual void TakeDamage(int amount)
    {
        if (amount <= 0 || !IsAlive())
            return;

        GameUnitDamageEvent damageEvent = new(this, amount);
        BeforeDamage?.Invoke(damageEvent);

        if (damageEvent.Cancelled || damageEvent.Amount <= 0)
            return;

        amount = damageEvent.Amount;

        currentHp = Mathf.Max(0, currentHp - amount);
        Damaged?.Invoke(this, amount);
        NotifyHpChanged();

        if (currentHp <= 0)
        {
            Die();
            return;
        }

        PlayHurtAnimation();
    }

    public virtual void Heal(int amount)
    {
        if (amount <= 0 || !IsAlive())
            return;

        currentHp = Mathf.Min(hp, currentHp + amount);
        Healed?.Invoke(this, amount);
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

    public virtual void Die()
    {
        NotifyDied();
        PlayAnimation(dieAnim, false);
    }

    public void BeginTurn()
    {
        if (IsAlive())
            TurnStarted?.Invoke(this);
    }

    protected void NotifyHpChanged()
    {
        HpChanged?.Invoke(this, currentHp, hp);
    }

    protected void NotifyDied()
    {
        if (deathNotified)
            return;

        deathNotified = true;
        Died?.Invoke(this);
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
