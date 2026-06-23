using UnityEngine;
using System;



public class Enemy : GameUnit
{
    public event Action<Enemy> DeathCompleted;

    public EnemyType type;
    public EnemyLevel enemyLevel;
    public int damage;
    public EnemyData data;
    // public int distanceToPlayer;
    //public int attackRange;
    public int gridRow;
    public int gridColumn;

    [Header("anim")]
    public string moveAnim = "Move";

    protected override void Awake()
    {
        base.Awake();

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

        if (skeletonGraphic != null)
        {
            skeletonGraphic.skeletonDataAsset = data.skeletonData;
            skeletonGraphic.Initialize(true);
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

        gridColumn -= amount;

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
            DeathCompleted?.Invoke(this);
            return;
        }

        currentTrack.Complete += _ => DeathCompleted?.Invoke(this);
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
