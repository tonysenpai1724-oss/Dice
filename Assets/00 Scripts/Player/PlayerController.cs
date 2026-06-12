using System.Collections;
using UnityEngine;


public class PlayerController : GameUnit
{
    public HeroData data;
    public void Setup(HeroData newData)
    {
        data = newData;
        SetHealth(data.hp, data.hp);

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
    }
    public void SetHp(int hp, int currentHp)
    {
        SetHealth(hp, currentHp);

        if (skeletonGraphic != null)
        {
            //   skeletonGraphic.skeletonDataAsset = data.skeletonData;
            skeletonGraphic.Initialize(true);
            PlayAnimation(idleAnim, true);


        }
    }

    // protected override void PlayHurtAnimation()
    // {
    //     StartCoroutine(PlayHurtDelayed());
    // }

    IEnumerator PlayHurtDelayed()
    {
        yield return new WaitForSeconds(0f);
        PlayAnimation(hurtAnim, false);
        QueueIdleAnimation();
    }

    public override void OnDie()
    {
        NotifyDied();
        PlayAnimation(dieAnim, false);

        if (currentTrack == null)
        {
            EndGame();
            return;
        }

        currentTrack.Complete += _ => EndGame();
    }

    void EndGame()
    {
        Debug.Log("Player Died");
        if (GameplayManager.Instance != null)
            GameplayManager.Instance.EndGame(false);
    }
}
