using Sirenix.OdinInspector;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "RuneDice/Rune/Rune Base")]
public class RuneSkillData : SerializedScriptableObject
{
    public RuneType TargetType;

    public int stackApply;

    public int valueApply = 1;
    public string runeName;
    public string description;
    public Sprite runeSprite;

    public virtual void Execute()
    {
        switch (TargetType)
        {
            case RuneType.Bomb:
                BombRuneSkillData.Execute();
                break;
            case RuneType.MinorLife:
                MinorLifeRuneSkillData.Execute();
                break;
            case RuneType.Shuffle:
                ShuffleRuneSkillData.Execute();
                break;
            case RuneType.Protection:
                ProtectionRuneSkillData.Execute(stackApply, valueApply);
                break;
            case RuneType.Gravity:
                GravityRuneSkillData.Execute(stackApply, valueApply);
                break;
        }
    }
}

public static class NormalRuneSkillData
{
    public static void Execute()
    {
    }
}

public static class MinorLifeRuneSkillData
{
    public static void Execute()
    {
    }
}

public static class ShuffleRuneSkillData
{
    public static void Execute()
    {
    }
}

public static class ProtectionRuneSkillData
{
    public static void Execute(int stackApply, int valueApply)
    {
        GameplayManager gameplay = GameplayManager.Instance;
        ShieldEffect shieldEffect = gameplay?.skillPlayer?.effectManager?.AddEffect<ShieldEffect>();
        if (shieldEffect == null)
            return;

        shieldEffect.AddStacks(valueApply);
    }
}

public static class BombRuneSkillData
{
    public static void Execute()
    {
        DiceManager diceManager = DiceManager.Instance;
        DiceQueueUI diceQueueUI = diceManager != null && diceManager.diceQueueUI != null
            ? diceManager.diceQueueUI
            : DiceQueueUI.Instance;
        DiceQueueManager diceQueue = diceManager != null ? diceManager.diceQueue : null;
        DiceData bombDiceData = diceManager != null ? diceManager.GetDiceData(5, DiceType.Bomb) : null;

        if (bombDiceData == null)
            return;

        if (diceQueueUI != null)
        {
            diceQueueUI.AddDice(bombDiceData);
            return;
        }

        diceQueue?.AddDice(bombDiceData);
    }
}

public static class GravityRuneSkillData
{
    static IEnumerator ForceMergeDelayed(DiceManager diceManager, Vector3 center, float radius)
    {
        yield return new WaitForSeconds(0.28f);

        if (diceManager == null)
            yield break;

        for (int i = 0; i < 6; i++)
        {
            float mergeRadius = Mathf.Max(0.4f, radius * 0.3f);
            if (!diceManager.ForceMergeNearCenter(center, radius, mergeRadius))
                yield break;

            yield return new WaitForSeconds(0.08f);
        }
    }

    public static void Execute(int stackApply, int valueApply)
    {
        DiceManager diceManager = DiceManager.Instance;
        if (diceManager == null)
            return;

        List<Dice> boardDices = diceManager.GetBoardDices();
        if (boardDices == null || boardDices.Count == 0)
            return;

        RuneManager runeManager = RuneManager.Instance;
        Vector3 center = runeManager != null ? runeManager.LastRuneDropWorldPosition : Vector3.zero;
        float radius = Mathf.Max(1.5f, valueApply);
        float pullStrength = Mathf.Max(2f, 4f + stackApply + valueApply);

        for (int i = 0; i < boardDices.Count; i++)
        {
            Dice dice = boardDices[i];
            if (dice == null || dice.rb == null)
                continue;

            Vector3 offset = center - dice.transform.position;
            offset.y = 0f;

            float distance = offset.magnitude;
            if (distance <= 0.001f || distance > radius)
                continue;

            Vector3 direction = offset / distance;
            float forceScale = 1f - Mathf.Clamp01(distance / radius);

            dice.canMerge = true;
            dice.rb.isKinematic = false;
            dice.ApplyBoardMoveConstraints();
            dice.rb.AddForce(direction * pullStrength * Mathf.Max(0.25f, forceScale), ForceMode.Impulse);
        }

        diceManager.StartCoroutine(ForceMergeDelayed(diceManager, center, radius));
    }
}

public enum RuneType
{
    Bomb,
    MinorLife,
    Shuffle,
    Protection,
    Gravity
}
