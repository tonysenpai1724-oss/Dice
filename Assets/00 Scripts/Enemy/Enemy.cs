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
    // public int distanceToPlayer;
    //public int attackRange;
    public int gridRow;
    public int gridColumn;

    Tween deathFadeTween;

    [Header("anim")]
    public string moveAnim = "Move";

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
        // distanceToPlayer = Mathf.Max(0, data.startDistance);
        // attackRange = Mathf.Max(1, data.attackRange);

        transform.localScale = data.scale;
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

        //   distanceToPlayer = Mathf.Max(0, distanceToPlayer - amount);
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
    // Normal,
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
