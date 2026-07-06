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
        Execute(null);
    }

    public void Execute(DiceData sourceDiceData)
    {
        if (sourceDiceData != null && DiceEvoSkillRuntime.TryExecute(sourceDiceData))
            return;

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
            case DiceType.BonusAtk:
                BonusAtkDiceSkillData.Execute(stackApply, valueApply);
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

public static class BonusAtkDiceSkillData
{
    public static void Execute(int stackApply, int valueApply)
    {
        GameplayManager gameplay = GameplayManager.Instance;
        if (gameplay == null)
            return;

        int percentPerDie = Mathf.Max(0, valueApply);

        int dieNumber = Mathf.Max(1, gameplay.skillDiceData != null ? gameplay.skillDiceData.level : 1);
        int bonusPercent = percentPerDie * dieNumber;
        int currentDamage = Mathf.Max(0, gameplay.skillDamage);
        int bonusDamage = Mathf.RoundToInt(currentDamage * (bonusPercent / 100f));
        if (bonusDamage <= 0)
            return;

        PlayerStats.Shared.AddTemporaryLevelStat(
            HeroStatType.Damage,
            bonusDamage,
            "BonusAtk",
            true
        );

        gameplay.AddDamage(bonusDamage);
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

public static class DiceEvoSkillRuntime
{
    public static bool TryExecute(DiceData sourceDiceData)
    {
        if (sourceDiceData == null)
            return false;

        switch (sourceDiceData.evol)
        {
            case DiceEvoType.TripleAttack:
                return ExecuteTripleAttack(sourceDiceData);
            case DiceEvoType.Ambush:
                return ExecuteAmbush(sourceDiceData);
            case DiceEvoType.X2BonusAtk:
                return ExecuteX2BonusAtk(sourceDiceData);
            case DiceEvoType.MagicCoin:
                return ExecuteMagicCoin(sourceDiceData);
            case DiceEvoType.Cure:
                return ExecuteCure(sourceDiceData);
            case DiceEvoType.Armor:
                return ExecuteArmor(sourceDiceData);
            default:
                return false;
        }
    }

    static bool ExecuteTripleAttack(DiceData sourceDiceData)
    {
        GameplayManager gameplay = GameplayManager.Instance;
        if (gameplay == null)
            return false;

        int attackCount = Mathf.Max(3, sourceDiceData.attackCount + 1);
        gameplay.SetAttackCount(attackCount);
        return true;
    }

    static bool ExecuteAmbush(DiceData sourceDiceData)
    {
        GameplayManager gameplay = GameplayManager.Instance;
        DodgeEffect dodgeEffect = gameplay?.skillPlayer?.effectManager?.AddEffect<DodgeEffect>();
        if (dodgeEffect == null)
            return false;

        int stacks = Mathf.Max(1, sourceDiceData.skillData != null ? sourceDiceData.skillData.valueApply : 1);
        dodgeEffect.AddStacks(stacks);
        return true;
    }

    static bool ExecuteX2BonusAtk(DiceData sourceDiceData)
    {
        GameplayManager gameplay = GameplayManager.Instance;
        if (gameplay?.skillPlayer == null)
            return false;

        int currentDamage = Mathf.Max(0, gameplay.skillDamage);
        int percentPerDie = Mathf.Max(0, sourceDiceData.skillData != null ? sourceDiceData.skillData.valueApply : 10);
        int dieNumber = Mathf.Max(1, sourceDiceData.level);
        int baseBonusDamage = Mathf.RoundToInt(currentDamage * ((percentPerDie * dieNumber) / 100f));
        int doubledBonusDamage = baseBonusDamage * 2;
        if (doubledBonusDamage <= 0)
            return false;

        PlayerStats.Shared.AddTemporaryLevelStat(
            HeroStatType.Damage,
            doubledBonusDamage,
            "X2BonusAtk",
            true
        );

        gameplay.AddDamage(doubledBonusDamage);
        return true;
    }

    static bool ExecuteMagicCoin(DiceData sourceDiceData)
    {
        GameplayManager gameplay = GameplayManager.Instance;
        if (gameplay == null)
            return false;

        int minCoin = Mathf.Max(0, sourceDiceData.skillData != null ? sourceDiceData.skillData.stackApply : 5);
        int maxCoin = Mathf.Max(minCoin, sourceDiceData.skillData != null ? sourceDiceData.skillData.valueApply : 10);
        int perDieReward = UnityEngine.Random.Range(minCoin, maxCoin + 1);
        int coinReward = perDieReward * Mathf.Max(1, sourceDiceData.level);

        gameplay.CancelAttack();
        CoinRewardEffect.Apply(coinReward);
        return true;
    }

    static bool ExecuteCure(DiceData sourceDiceData)
    {
        GameplayManager gameplay = GameplayManager.Instance;
        GameUnit player = gameplay?.skillPlayer;
        if (gameplay == null || player == null)
            return false;

        int percentPerDie = Mathf.Max(0, sourceDiceData.skillData != null ? sourceDiceData.skillData.valueApply : 10);
        float totalPercent = (percentPerDie / 100f) * Mathf.Max(1, sourceDiceData.level);
        int healAmount = Mathf.RoundToInt(player.hp * totalPercent);
        if (healAmount <= 0)
            return false;

        gameplay.CancelAttack();
        player.effectManager?.AddEffect<HealEffect>()?.Apply(healAmount);
        player.effectManager?.RemoveEffectsByType(EffectType.Debuff);
        return true;
    }

    static bool ExecuteArmor(DiceData sourceDiceData)
    {
        GameplayManager gameplay = GameplayManager.Instance;
        PlayerController player = gameplay?.skillPlayer;
        if (gameplay == null || player == null)
            return false;

        int percentPerDie = Mathf.Max(0, sourceDiceData.skillData != null ? sourceDiceData.skillData.valueApply : 10);
        float totalPercent = (percentPerDie / 100f) * Mathf.Max(1, sourceDiceData.level);
        int armorAmount = Mathf.RoundToInt(Mathf.Max(0, player.RuntimeDefense) * totalPercent);
        if (armorAmount <= 0)
            return false;

        ShieldEffect shieldEffect = player.effectManager?.AddEffect<ShieldEffect>();
        if (shieldEffect == null)
            return false;

        shieldEffect.AddStacks(armorAmount);
        return true;
    }
}








