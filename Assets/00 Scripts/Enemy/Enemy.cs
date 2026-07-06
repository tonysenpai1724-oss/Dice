using UnityEngine;
using System;
using DG.Tweening;

public class Enemy : GameUnit
{
    public event Action<Enemy> DeathCompleted;

    [Header("Death Fade")]
    public float deathFadeDuration = 0.3f;

    public EnemyType type;
    public EnemyLevel enemyLevel;
    public int damage;
    public EnemyData data;
    public int gridRow;
    public int gridColumn;

    Tween deathFadeTween;

    [Header("anim")]
    public string moveAnim = "Move";
    [Min(0.1f)] public float enemyAttackAnimSpeed = 1f;

    protected override void Awake()
    {
        base.Awake();

        ResetVisualAlpha();

        if (idleAnim == "Idle")
            idleAnim = "IDLE";

        if (dieAnim == "Die")
            dieAnim = "DIE";
    }

    public virtual void Setup(EnemyData newData)
    {
        data = newData;
        type = data.type;
        SetHp(data.hp, data.damage);

        transform.localScale = data.scale;
        enemyAttackAnimSpeed = Mathf.Max(0.1f, data.attackAnimSpeed);
        ResetDeathFade();

        if (skeletonGraphic != null)
        {
            skeletonGraphic.skeletonDataAsset = data.skeletonData;
            skeletonGraphic.Initialize(true);
            ResetVisualAlpha();
            PlayAnimation(idleAnim, true);
        }
        NotifyHpChanged();
    }

    public virtual void Setup(EnemyEntryConfig entry)
    {
        if (entry == null || entry.Data == null)
            return;

        data = entry.Data;
        type = data.type;
        SetHp(entry.GetHp(), entry.GetDamage());

        transform.localScale = data.scale;
        enemyAttackAnimSpeed = Mathf.Max(0.1f, data.attackAnimSpeed);
        ResetDeathFade();

        if (skeletonGraphic != null)
        {
            skeletonGraphic.skeletonDataAsset = data.skeletonData;
            skeletonGraphic.Initialize(true);
            ResetVisualAlpha();
            PlayAnimation(idleAnim, true);
        }
        NotifyHpChanged();
    }

    public virtual void SetHp(int hp, int damage)
    {
        this.damage = damage;
        SetHealth(hp, hp);
    }

    public virtual bool CanAttack()
    {
        if (!IsAlive())
            return false;

        if (type == EnemyType.Range)
            return true;

        return gridColumn <= 1;
    }

    public virtual void MoveTowardPlayer(int amount)
    {
        if (!IsAlive())
            return;
        if (type != EnemyType.Chest)
            gridColumn -= amount;
        else
            gridColumn += amount;
    }

    public override void OnDie()
    {
        NotifyDied();

        if (hpBar != null)
            hpBar.gameObject.SetActive(false);

        PlayAnimation(dieAnim, false);

        if (currentTrack == null)
        {
            BeginDeathFade();
            return;
        }

        currentTrack.Complete += HandleDeathAnimationComplete;
    }

    protected override void OnDestroy()
    {
        deathFadeTween?.Kill();
        base.OnDestroy();
    }

    void HandleDeathAnimationComplete(Spine.TrackEntry trackEntry)
    {
        if (currentTrack != null)
            currentTrack.Complete -= HandleDeathAnimationComplete;

        BeginDeathFade();
    }

    void BeginDeathFade()
    {
        if (skeletonGraphic == null)
        {
            DeathCompleted?.Invoke(this);
            return;
        }

        deathFadeTween?.Kill();
        deathFadeTween = skeletonGraphic
            .DOFade(0f, deathFadeDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() => DeathCompleted?.Invoke(this));
    }

    void ResetDeathFade()
    {
        deathFadeTween?.Kill();
        ResetVisualAlpha();
    }

    void ResetVisualAlpha()
    {
        if (skeletonGraphic == null)
            return;

        Color color = skeletonGraphic.color;
        color.a = 1f;
        skeletonGraphic.color = color;
    }
}

public enum EnemyType
{
    Range,
    Melee,
    Chest,
}
public enum EnemyLevel
{
    Normal,
    MiniBoss,
    Boss,
}
