using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Spine.Unity;
using Spine;



public class Enemy : MonoBehaviour
{
    public EnemyType type;
    public int hp;
    public int currentHp;
    public int damage;
    public EnemyData data;
    public int distanceToPlayer;
    public int attackRange;
    public SkeletonGraphic skeletonGraphic;
    public HPBar hpBar;

    [Header("anim")]
    public string idleAnim = "IDLE";
    public string moveAnim = "Move";
    public string attackAnim = "Attack";
    public string dieAnim = "DIE";
    public string hurtAnim = "Hurt";
    private Spine.TrackEntry currentTrack;


    public virtual void Setup(EnemyData newData)
    {
        data = newData;
        type = data.type;
        SetHp(data.hp, data.damage);
        distanceToPlayer = Mathf.Max(0, data.startDistance);
        attackRange = Mathf.Max(1, data.attackRange);

        transform.localScale = data.scale;

        if (skeletonGraphic != null)
        {
            skeletonGraphic.skeletonDataAsset = data.skeletonData;
            skeletonGraphic.Initialize(true);
            PlayAnimation(idleAnim, true);


        }
        UpdateHpBar();
    }
    public void PlayAnimation(string animName, bool loop = false)
    {
        if (skeletonGraphic == null)
            return;

        animName = AnimationNameUtility.ResolveAnimationName(
            skeletonGraphic.Skeleton?.Data?.Animations,
            animName
        );
        currentTrack = skeletonGraphic.AnimationState.SetAnimation(
            0,
            animName,
            loop
        );
    }


    public virtual void SetHp(int hp, int damage)
    {
        this.hp = hp;
        this.damage = damage;
        currentHp = hp;
    }

    public virtual bool IsAlive()
    {
        return currentHp > 0;
    }

    public virtual void TakeDamage(int amount)
    {
        if (!IsAlive())
            return;

        currentHp -= amount;

        UpdateHpBar();



        if (currentHp <= 0)
        {
            Die();
            return;
        }
        PlayAnimation(hurtAnim, false);

        if (skeletonGraphic != null)
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
        }

        // StartCoroutine(PlayHurtDelayed());
    }
    private IEnumerator PlayHurtDelayed()
    {
        yield return new WaitForSeconds(0.5f);

        PlayAnimation(hurtAnim, false);

        if (skeletonGraphic != null)
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
        }
    }

    public virtual bool CanAttack()
    {
        if (!IsAlive())
            return false;

        if (type == EnemyType.Range)
            return true;

        return distanceToPlayer <= attackRange;
    }

    private void UpdateHpBar()
    {
        if (hpBar != null)
            hpBar.SetHp(currentHp, hp);

    }

    public virtual void MoveTowardPlayer(int amount)
    {
        if (!IsAlive())
            return;

        distanceToPlayer = Mathf.Max(0, distanceToPlayer - amount);
    }

    public virtual void Die()
    {
        hpBar.gameObject.SetActive(false);
        PlayAnimation(dieAnim, false);

        currentTrack.Complete += _ =>
        {
            Debug.Log("Enemy Died");
            EnemyManager.Instance?.RemoveEnemy(this);
            Destroy(gameObject);
        };
        //  EnemyManager.Instance?.RemoveEnemy(this);
    }
}


public enum EnemyType
{
    // Normal,
    Melee,
    Range,
    MiniBoss,
    Boss,
}
