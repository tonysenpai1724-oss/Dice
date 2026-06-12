using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerController : GameUnit
{
    public HeroData data;
    public List<DiceData> diceDatas = new();

    public void Setup(HeroData newData)
    {
        data = newData;
        InitializeDiceDatas();
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

    public void InitializeDiceDatas()
    {
        diceDatas.Clear();

        if (data == null || data.startDiceLevelConfig == null)
            return;

        if (!data.startDiceLevelConfig.TryGetValue(data.level, out List<DiceData> startDices))
            return;

        if (startDices == null)
            return;

        for (int i = 0; i < startDices.Count; i++)
        {
            if (startDices[i] != null)
                diceDatas.Add(startDices[i]);
        }
    }

    public void AddDiceData(DiceData diceData)
    {
        if (diceData == null)
            return;

        diceDatas.Add(diceData);
    }

    public void AddDiceDatas(List<DiceData> newDiceDatas)
    {
        if (newDiceDatas == null)
            return;

        for (int i = 0; i < newDiceDatas.Count; i++)
        {
            AddDiceData(newDiceDatas[i]);
        }
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
