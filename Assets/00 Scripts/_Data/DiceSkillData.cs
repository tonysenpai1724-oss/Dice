using Sirenix.OdinInspector;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public enum DiceSkillEffectType
{
    DefaultByDiceType,
    MultiAttack
}

[CreateAssetMenu(menuName = "RuneDice/Skill/Skill Base")]
public class DiceSkillData : SerializedScriptableObject
{
    public DiceType TargetType;
    public DiceSkillEffectType effectType = DiceSkillEffectType.DefaultByDiceType;

    public int stackApply;

    public int valueApply = 1;

    public virtual void Execute()
    {
        switch (effectType)
        {
            case DiceSkillEffectType.MultiAttack:
                MultiAttackDiceSkillData.Execute(stackApply, valueApply);
                return;
        }

        switch (TargetType)
        {
            case DiceType.Normal:
                NormalDiceSkillData.Execute();
                break;
            case DiceType.Dodge:
                DodgeDiceSkillData.Execute(stackApply, valueApply);
                break;
            case DiceType.Poison:
                PoisonDiceSkillData.Execute(stackApply, valueApply);
                break;
            case DiceType.Heal:
                HealDiceSkillData.Execute(stackApply, valueApply);
                break;
            case DiceType.Shield:
                ShieldDiceSkillData.Execute(stackApply, valueApply);
                break;
            case DiceType.Backstab:
                BackstabDiceSkillData.Execute(stackApply, valueApply);
                break;
            case DiceType.Coin:
                CoinDiceSkillData.Execute(stackApply, valueApply);
                break;
            case DiceType.BlindStrike:
                BlindStrikeDiceSkillData.Execute(stackApply, valueApply);
                break;
            case DiceType.Stun:
                StunDiceSkillData.Execute(stackApply, valueApply);
                break;
            case DiceType.Bomb:
                BombDiceSkillData.Execute(stackApply, valueApply);
                break;
            case DiceType.Enemy:
                EnemyDiceSKillData.Execute(stackApply, valueApply);
                break;
            case DiceType.MultiAttack:
                MultiAttackDiceSkillData.Execute(stackApply, valueApply);
                break;
        }
    }
}

public static class NormalDiceSkillData
{
    public static void Execute()
    {
    }
}

public static class DodgeDiceSkillData
{
    public static void Execute(int stackApply, int valueApply)
    {
        GameplayManager gameplay = GameplayManager.Instance;
        DodgeEffect dodgeEffect = gameplay?.skillPlayer?.effectManager?.AddEffect<DodgeEffect>();
        if (dodgeEffect == null)
            return;

        dodgeEffect.AddStacks(stackApply);
    }
}

public static class PoisonDiceSkillData
{
    public static void Execute(int stackApply, int valueApply)
    {
        GameplayManager gameplay = GameplayManager.Instance;
        if (gameplay == null)
            return;

        Enemy target = gameplay.skillEnemyManager != null
            ? gameplay.skillEnemyManager.GetRightmostAliveEnemy()
            : gameplay.GetTargetEnemy();
        if (target == null)
            return;

        int poisonTurns = Mathf.Max(1, gameplay.DiceDamage);
        int poisonDamage = poisonTurns;

        gameplay.CancelAttack();
        target.effectManager?.AddEffect<PoisonEffect>()?.Apply(poisonTurns, poisonDamage);
    }
}

public static class HealDiceSkillData
{
    public static void Execute(int stackApply, int valueApply)
    {
        GameplayManager gameplay = GameplayManager.Instance;
        if (gameplay?.skillPlayer == null)
            return;

        int healAmount = Mathf.Max(0, gameplay.DiceDamage);

        gameplay.CancelAttack();
        gameplay.skillPlayer.effectManager?.AddEffect<HealEffect>()?.Apply(healAmount);
    }
}

public static class ShieldDiceSkillData
{
    public static void Execute(int stackApply, int valueApply)
    {
        Debug.Log("ShieldDiceSkillData");
        GameplayManager gameplay = GameplayManager.Instance;
        ShieldEffect shieldEffect = gameplay?.skillPlayer?.effectManager?.AddEffect<ShieldEffect>();
        if (shieldEffect == null)
            return;

        shieldEffect.AddStacks(stackApply);
    }
}

public static class BackstabDiceSkillData
{
    public static void Execute(int stackApply, int valueApply)
    {
        GameplayManager gameplay = GameplayManager.Instance;
        gameplay?.AddDamage(gameplay.DiceDamage);
    }
}

public static class CoinDiceSkillData
{
    public static void Execute(int stackApply, int valueApply)
    {
        GameplayManager gameplay = GameplayManager.Instance;
        if (gameplay == null)
            return;

        int coinReward = Mathf.Max(0, gameplay.DiceDamage);

        gameplay.CancelAttack();
        CoinRewardEffect.Apply(coinReward);
    }
}

public static class BlindStrikeDiceSkillData
{
    public static void Execute(int stackApply, int valueApply)
    {
        GameplayManager gameplay = GameplayManager.Instance;
        DamageReductionEffect damageReductionEffect = gameplay?.skillPlayer?.effectManager?.AddEffect<DamageReductionEffect>();
        if (damageReductionEffect != null)
            damageReductionEffect.AddReduction(gameplay.DiceDamage);
    }
}

public static class StunDiceSkillData
{
    public static void Execute(int stackApply, int valueApply)
    {
        GameplayManager gameplay = GameplayManager.Instance;
        EnemyTurnSkipEffect turnSkipEffect = gameplay?.skillEnemyManager?.effectManager?.AddEffect<EnemyTurnSkipEffect>();
        if (turnSkipEffect != null)
            turnSkipEffect.AddTurns(stackApply);
    }
}

public static class BombDiceSkillData
{
    public static void Execute(int stackApply, int valueApply)
    {
        GameplayManager gameplay = GameplayManager.Instance;
        if (gameplay == null || gameplay.skillEnemyManager == null)
            return;

        gameplay.CancelAttack();
        gameplay.AddDamageAllEnemiesAfterAttack(valueApply);
    }
}

public static class MultiAttackDiceSkillData
{
    public static void Execute(int stackApply, int valueApply)
    {
        GameplayManager gameplay = GameplayManager.Instance;
        if (gameplay == null)
            return;

        int attackCount = Mathf.Max(1, valueApply);
        gameplay.SetAttackCount(attackCount);
    }
}

public static class EnemyDiceSKillData
{
    public static void Execute(int stackApply, int valueApply)
    {
    }
}

public static class DiceSkillFactory
{
    static readonly Dictionary<DiceType, DiceSkillData> cachedSkills =
        new();

    public static DiceSkillData Create(DiceType type)
    {
        if (cachedSkills.TryGetValue(type, out DiceSkillData cachedSkill) &&
            cachedSkill != null)
        {
            return cachedSkill;
        }

        DiceSkillData skill = new DiceSkillData();
        skill.hideFlags = HideFlags.HideAndDontSave;
        cachedSkills[type] = skill;
        return skill;
    }
}
