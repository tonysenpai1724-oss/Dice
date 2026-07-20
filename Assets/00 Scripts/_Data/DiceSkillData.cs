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
        DiceType executionType = sourceDiceData != null ? sourceDiceData.type : TargetType;

        switch (effectType)
        {
            case DiceSkillEffectType.MultiAttack:
                if (executionType == DiceType.MultiAttack)
                {
                    MultiAttackDiceSkillData.Execute(stackApply, valueApply);
                    return;
                }
                break;
        }

        switch (executionType)
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
            case DiceType.TripleAttack:
                TripleAttackDiceSkillData.Execute(sourceDiceData, stackApply, valueApply);
                break;
            case DiceType.Ambush:
                AmbushDiceSkillData.Execute(sourceDiceData, stackApply, valueApply);
                break;
            case DiceType.X2BonusAtk:
                X2BonusAtkDiceSkillData.Execute(sourceDiceData, stackApply, valueApply);
                break;
            case DiceType.MagicCoin:
                MagicCoinDiceSkillData.Execute(sourceDiceData, stackApply, valueApply);
                break;
            case DiceType.Cure:
                CureDiceSkillData.Execute(sourceDiceData, stackApply, valueApply);
                break;
            case DiceType.Armor:
                ArmorDiceSkillData.Execute(sourceDiceData, stackApply, valueApply);
                break;
            case DiceType.Vulnerable:
                VulnerableDiceSkillData.Execute(stackApply, valueApply);
                break;
            case DiceType.Exhaust:
                ExhaustDiceSkillData.Execute(stackApply, valueApply);
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
        PlayerController player = gameplay?.skillPlayer;
        if (gameplay == null || player == null)
            return;

        int percentPerDie = Mathf.Max(0, valueApply);
        int dieNumber = Mathf.Max(1, gameplay.skillDiceData != null ? gameplay.skillDiceData.level : 1);
        int healAmount = DiceSkillFormula.CalculatePercentOfValue(player.hp, percentPerDie, dieNumber);
        if (healAmount <= 0)
            return;

        gameplay.CancelAttack();
        player.effectManager?.AddEffect<HealEffect>()?.Apply(healAmount);
    }
}

public static class DiceSkillFormula
{
    public static int CalculatePercentOfValue(int currentValue, int percentPerDie, int dieNumber)
    {
        float totalPercent = (Mathf.Max(0, percentPerDie) * Mathf.Max(1, dieNumber)) / 100f;
        return Mathf.RoundToInt(Mathf.Max(0, currentValue) * totalPercent);
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
        Enemy target = gameplay?.skillEnemyManager != null
            ? gameplay.skillEnemyManager.GetRandomAliveEnemy()
            : gameplay?.GetTargetEnemy();
        EnemyTurnSkipEffect turnSkipEffect = target?.effectManager?.AddEffect<EnemyTurnSkipEffect>();
        if (turnSkipEffect != null)
        {
            gameplay?.CancelAttack();
            turnSkipEffect.AddTurns(stackApply);
        }
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

public static class TripleAttackDiceSkillData
{
    public static void Execute(DiceData sourceDiceData, int stackApply, int valueApply)
    {
        GameplayManager gameplay = GameplayManager.Instance;
        if (gameplay == null)
            return;

        int baseAttackCount = sourceDiceData != null ? sourceDiceData.attackCount : valueApply;
        int attackCount = Mathf.Max(3, baseAttackCount + 1);
        gameplay.SetAttackCount(attackCount);
    }
}

public static class AmbushDiceSkillData
{
    public static void Execute(DiceData sourceDiceData, int stackApply, int valueApply)
    {
        GameplayManager gameplay = GameplayManager.Instance;
        DodgeEffect dodgeEffect = gameplay?.skillPlayer?.effectManager?.AddEffect<DodgeEffect>();
        if (dodgeEffect == null)
            return;

        int stacks = Mathf.Max(1, valueApply);
        dodgeEffect.AddStacks(stacks);
    }
}

public static class X2BonusAtkDiceSkillData
{
    public static void Execute(DiceData sourceDiceData, int stackApply, int valueApply)
    {
        GameplayManager gameplay = GameplayManager.Instance;
        if (gameplay?.skillPlayer == null)
            return;

        int currentDamage = Mathf.Max(0, gameplay.skillDamage);
        int percentPerDie = Mathf.Max(0, valueApply > 0 ? valueApply : 10);
        int dieNumber = Mathf.Max(1, sourceDiceData != null ? sourceDiceData.level : 1);
        int baseBonusDamage = Mathf.RoundToInt(currentDamage * ((percentPerDie * dieNumber) / 100f));
        int doubledBonusDamage = baseBonusDamage * 2;
        if (doubledBonusDamage <= 0)
            return;

        PlayerStats.Shared.AddTemporaryLevelStat(
            HeroStatType.Damage,
            doubledBonusDamage,
            "X2BonusAtk",
            true
        );

        gameplay.AddDamage(doubledBonusDamage);
    }
}

public static class MagicCoinDiceSkillData
{
    public static void Execute(DiceData sourceDiceData, int stackApply, int valueApply)
    {
        GameplayManager gameplay = GameplayManager.Instance;
        if (gameplay == null)
            return;

        int minCoin = Mathf.Max(0, stackApply > 0 ? stackApply : 5);
        int maxCoin = Mathf.Max(minCoin, valueApply > 0 ? valueApply : 10);
        int perDieReward = UnityEngine.Random.Range(minCoin, maxCoin + 1);
        int coinReward = perDieReward * Mathf.Max(1, sourceDiceData != null ? sourceDiceData.level : 1);

        gameplay.CancelAttack();
        CoinRewardEffect.Apply(coinReward);
    }
}

public static class CureDiceSkillData
{
    public static void Execute(DiceData sourceDiceData, int stackApply, int valueApply)
    {
        GameplayManager gameplay = GameplayManager.Instance;
        GameUnit player = gameplay?.skillPlayer;
        if (gameplay == null || player == null)
            return;

        int percentPerDie = Mathf.Max(0, valueApply > 0 ? valueApply : 10);
        int dieNumber = Mathf.Max(1, sourceDiceData != null ? sourceDiceData.level : 1);
        int healAmount = DiceSkillFormula.CalculatePercentOfValue(player.hp, percentPerDie, dieNumber);
        if (healAmount <= 0)
            return;

        gameplay.CancelAttack();
        player.effectManager?.AddEffect<HealEffect>()?.Apply(healAmount);
        player.effectManager?.RemoveEffectsByType(EffectType.Debuff);
    }
}

public static class ArmorDiceSkillData
{
    public static void Execute(DiceData sourceDiceData, int stackApply, int valueApply)
    {
        GameplayManager gameplay = GameplayManager.Instance;
        PlayerController player = gameplay?.skillPlayer;
        if (gameplay == null || player == null)
            return;

        int percentPerDie = Mathf.Max(0, valueApply > 0 ? valueApply : 10);
        int dieNumber = Mathf.Max(1, sourceDiceData != null ? sourceDiceData.level : 1);
        int armorAmount = DiceSkillFormula.CalculatePercentOfValue(player.RuntimeDefense, percentPerDie, dieNumber);
        if (armorAmount <= 0)
            return;

        ShieldEffect shieldEffect = player.effectManager?.AddEffect<ShieldEffect>();
        if (shieldEffect != null)
            shieldEffect.AddStacks(armorAmount);
    }
}

public static class VulnerableDiceSkillData
{
    const int DefaultPercentPerStack = 10;

    public static void Execute(int stackApply, int valueApply)
    {
        GameplayManager gameplay = GameplayManager.Instance;
        Enemy target = gameplay?.skillEnemyManager != null
            ? gameplay.skillEnemyManager.GetRandomAliveEnemy()
            : gameplay?.GetTargetEnemy();

        VulnerableEffect vulnerableEffect = target?.effectManager?.AddEffect<VulnerableEffect>();
        if (vulnerableEffect != null)
        {
            gameplay?.CancelAttack();
            vulnerableEffect.AddPercentStacks(Mathf.Max(1, stackApply), GetPercent(valueApply));
        }
    }

    static int GetPercent(int valueApply)
    {
        return valueApply > 0 ? valueApply : DefaultPercentPerStack;
    }
}

public static class ExhaustDiceSkillData
{
    const int DefaultPercentPerStack = 10;

    public static void Execute(int stackApply, int valueApply)
    {
        GameplayManager gameplay = GameplayManager.Instance;
        Enemy target = gameplay?.skillEnemyManager != null
            ? gameplay.skillEnemyManager.GetRandomAliveEnemy()
            : gameplay?.GetTargetEnemy();
        ExhaustEffect exhaustEffect = target?.effectManager?.AddEffect<ExhaustEffect>();
        if (exhaustEffect != null)
        {
            gameplay?.CancelAttack();
            exhaustEffect.AddPercentStacks(Mathf.Max(1, stackApply), GetPercent(valueApply));
        }
    }

    static int GetPercent(int valueApply)
    {
        return valueApply > 0 ? valueApply : DefaultPercentPerStack;
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
        skill.TargetType = type;
        skill.hideFlags = HideFlags.HideAndDontSave;
        cachedSkills[type] = skill;
        return skill;
    }
}







