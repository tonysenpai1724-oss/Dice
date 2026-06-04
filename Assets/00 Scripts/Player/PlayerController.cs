using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Spine.Unity;
using Spine;


public class PlayerController : MonoBehaviour
{
    public int hp;
    public int currentHp;
    public TextMeshProUGUI hpText;
    public SkeletonGraphic skeletonGraphic;
    [Header("anim")]
    public string idleAnim = "Idle";
    //public string moveAnim = "Move";
    public string attackAnim = "Attack";
    public string dieAnim = "Die";
    public string hurtAnim = "Hurt";
    private Spine.TrackEntry currentTrack;

    public HeroData data;
    public void Setup(HeroData newData)
    {
        data = newData;
        hp = data.hp;
        currentHp = data.hp;

        if (skeletonGraphic != null)
        {
            skeletonGraphic.skeletonDataAsset = data.skeletonData;
            skeletonGraphic.Initialize(true);
            PlayAnimation(idleAnim, true);
        }
    }
    void Start()
    {
        Setup(data);
        // SetHp(1000, 1000);
        if (hpText != null)
        {
            hpText.text = currentHp.ToString() + "/" + hp.ToString();
        }
    }
    public void SetHp(int hp, int currentHp)
    {
        this.hp = hp;
        this.currentHp = currentHp;
        if (skeletonGraphic != null)
        {
            //   skeletonGraphic.skeletonDataAsset = data.skeletonData;
            skeletonGraphic.Initialize(true);
            PlayAnimation(idleAnim, true);


        }
    }
    public void PlayAnimation(string animName, bool loop = false)
    {
        if (skeletonGraphic == null)
            return;

        currentTrack = skeletonGraphic.AnimationState.SetAnimation(
            0,
            animName,
            loop
        );
    }
    public void Heal(int amount)
    {
        currentHp += amount;
    }
    public void TakeDamage(int amount)
    {
        currentHp -= amount;
        hpText.text = currentHp.ToString() + "/" + hp.ToString();
        StartCoroutine(PlayHurtDelayed());
        if (currentHp <= 0)
        {
            Die();
        }


    }
    private IEnumerator PlayHurtDelayed()
    {
        yield return new WaitForSeconds(0.5f);

        PlayAnimation(hurtAnim, false);

        if (skeletonGraphic != null)
        {
            skeletonGraphic.AnimationState.AddAnimation(
                0,
                idleAnim,
                true,
                0
            );
        }
    }
    void Die()
    {
        PlayAnimation(dieAnim, false);

        currentTrack.Complete += _ =>
        {
            Debug.Log("Player Died");
            if (GameplayManager.Instance != null)
                GameplayManager.Instance.EndGame(false);
        };
    }
}


