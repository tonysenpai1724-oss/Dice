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
    }

}




public enum RuneType
{
    Bomb,
    MinorLife,
    Shuffle,
    Protection
}
