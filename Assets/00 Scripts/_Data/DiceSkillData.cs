using Sirenix.OdinInspector;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


[CreateAssetMenu(menuName = "RuneDice/Skill/Skill Base")]
public class DiceSkillData : SerializedScriptableObject
{
    public DiceType TargetType;

    public int stackApply;

    public int valueApply = 1;

    public virtual void Execute(DiceSkillContext context)
    {
        switch (TargetType)
        {
            case DiceType.Normal:
                NormalDiceSkillData.Execute(context);
                break;
            case DiceType.Dodge:
                DodgeDiceSkillData.Execute(context, stackApply, valueApply);
                break;
            case DiceType.Poison:
                PoisonDiceSkillData.Execute(context, stackApply, valueApply);
                break;
            case DiceType.Heal:
                HealDiceSkillData.Execute(context, stackApply, valueApply);
                break;
            case DiceType.Shield:
                ShieldDiceSkillData.Execute(context, stackApply, valueApply);
                break;
            case DiceType.Backstab:
                BackstabDiceSkillData.Execute(context, stackApply, valueApply);
                break;
            case DiceType.Coin:
                CoinDiceSkillData.Execute(context, stackApply, valueApply);
                break;
            case DiceType.BlindStrike:
                BlindStrikeDiceSkillData.Execute(context, stackApply, valueApply);
                break;
            case DiceType.Stun:
                StunDiceSkillData.Execute(context, stackApply, valueApply);
                break;
            case DiceType.Bomb:
                BombDiceSkillData.Execute(context, stackApply, valueApply);
                break;
            case DiceType.Enemy:
                EnemyDiceSKillData.Execute(context, stackApply, valueApply);
                break;

        }
    }
}


public static class NormalDiceSkillData
{
    public static void Execute(DiceSkillContext context)
    {
    }
}

public static class DodgeDiceSkillData
{
    public static void Execute(DiceSkillContext context, int stackApply, int valueApply)
    {
        DodgeEffect dodgeEffect = context?.player.effectManager?.AddEffect<DodgeEffect>(context.player);
        if (dodgeEffect == null)
            return;

        dodgeEffect.AddStacks(stackApply);
    }
}

public static class PoisonDiceSkillData
{
    public static void Execute(DiceSkillContext context, int stackApply, int valueApply)
    {
        if (context == null)
            return;

        Enemy target = context.GetTargetEnemy();
        if (target == null)
            return;

        int poisonTurns = Mathf.Max(1, context.DiceDamage);
        int poisonDamage = poisonTurns;

        context.CancelAttack();
        target.effectManager?.AddEffect<PoisonEffect>(target)?.Apply(poisonTurns, poisonDamage);
    }
}

public static class HealDiceSkillData
{

    public static void Execute(DiceSkillContext context, int stackApply, int valueApply)
    {
        if (context?.player == null)
            return;

        int healAmount = Mathf.Max(0, context.DiceDamage);

        context.CancelAttack();
        context.player.effectManager?.AddEffect<HealEffect>(context.player)?.Apply(healAmount);
    }
}

public static class ShieldDiceSkillData
{

    public static void Execute(DiceSkillContext context, int stackApply, int valueApply)
    {
        ShieldEffect shieldEffect = context?.player.effectManager?.AddEffect<ShieldEffect>(context.player);
        if (shieldEffect == null)
            return;

        shieldEffect.AddStacks(stackApply);
    }
}

public static class BackstabDiceSkillData
{

    public static void Execute(DiceSkillContext context, int stackApply, int valueApply)
    {
        context?.AddDamage(context.DiceDamage);
    }
}

public static class CoinDiceSkillData
{

    public static void Execute(DiceSkillContext context, int stackApply, int valueApply)
    {
        if (context == null)
            return;

        int coinReward = Mathf.Max(0, context.DiceDamage);

        context.CancelAttack();
        CoinRewardEffect.Apply(coinReward);
    }
}

public static class BlindStrikeDiceSkillData
{

    public static void Execute(DiceSkillContext context, int stackApply, int valueApply)
    {
        DamageReductionEffect damageReductionEffect = context?.player.effectManager?.AddEffect<DamageReductionEffect>(context.player);
        if (damageReductionEffect != null)
            damageReductionEffect.AddReduction(context.DiceDamage);
    }
}

public static class StunDiceSkillData
{

    public static void Execute(DiceSkillContext context, int stackApply, int valueApply)
    {
        EnemyTurnSkipEffect turnSkipEffect = context?.enemyManager.effectManager?.AddEffect<EnemyTurnSkipEffect>(context.enemyManager);
        if (turnSkipEffect != null)
            turnSkipEffect.AddTurns(stackApply);
    }
}

public static class BombDiceSkillData
{

    public static void Execute(DiceSkillContext context, int stackApply, int valueApply)
    {
        if (context == null || context.enemyManager == null)
            return;

        context.AddAfterAttack(() =>
        {
            DamageAllEnemiesEffect damageAllEnemiesEffect =
                context.enemyManager.effectManager?.AddEffect<DamageAllEnemiesEffect>(context.enemyManager);

            if (damageAllEnemiesEffect != null)
                damageAllEnemiesEffect.Apply(valueApply);
        });
    }
}
public static class EnemyDiceSKillData
{

    public static void Execute(DiceSkillContext context, int stackApply, int valueApply)
    {

    }
}

public sealed class DiceSkillContext
{
    readonly List<System.Action> afterAttackActions =
        new();

    public DiceData diceData;
    public DiceQueue queue;
    public EnemyManager enemyManager;
    public PlayerController player;
    public Dice dice;
    public Enemy targetEnemy;

    public int damage;
    public bool skipAttack;

    public int DiceDamage => diceData != null ? Mathf.Max(0, diceData.damage) : 0;

    public DiceSkillContext(
        DiceData diceData,
        DiceQueue queue,
        Dice dice,
        EnemyManager enemyManager,
        PlayerController player,
        Enemy targetEnemy
    )
    {
        this.diceData = diceData;
        this.queue = queue;
        this.dice = dice;
        this.enemyManager = enemyManager;
        this.player = player;
        this.targetEnemy = targetEnemy;
        damage = diceData != null ? Mathf.Max(0, diceData.damage) : 0;
    }

    public void AddDamage(int amount)
    {
        damage = Mathf.Max(0, damage + amount);
    }

    public void SetDamage(int amount)
    {
        damage = Mathf.Max(0, amount);
    }

    public void CancelAttack()
    {
        SetDamage(0);
        skipAttack = true;
    }

    public Enemy GetTargetEnemy()
    {
        if (targetEnemy != null &&
            targetEnemy.gameObject.activeInHierarchy &&
            targetEnemy.IsAlive())
        {
            return targetEnemy;
        }

        return enemyManager != null ? enemyManager.GetNearestAliveEnemy() : null;
    }

    public void AddAfterAttack(System.Action action)
    {
        if (action != null)
            afterAttackActions.Add(action);
    }

    public void RunAfterAttackActions()
    {
        for (int i = 0; i < afterAttackActions.Count; i++)
        {
            afterAttackActions[i]?.Invoke();
        }

        afterAttackActions.Clear();
    }
}

public static class DiceSkillFactory
{
    static readonly System.Collections.Generic.Dictionary<DiceType, DiceSkillData> cachedSkills =
        new();

    public static DiceSkillData Create(DiceType type)
    {
        if (cachedSkills.TryGetValue(type, out DiceSkillData cachedSkill) &&
            cachedSkill != null)
        {
            return cachedSkill;
        }

        DiceSkillData skill = new DiceSkillData();

        // DiceSkillData skill;
        // switch (type)
        // {
        //     case DiceType.Dodge:
        //         skill = ScriptableObject.CreateInstance<DodgeDiceSkillData>();
        //         break;
        //     case DiceType.Poison:
        //         skill = ScriptableObject.CreateInstance<PoisonDiceSkillData>();
        //         break;
        //     case DiceType.Heal:
        //         skill = ScriptableObject.CreateInstance<HealDiceSkillData>();
        //         break;
        //     case DiceType.Shield:
        //         skill = ScriptableObject.CreateInstance<ShieldDiceSkillData>();
        //         break;
        //     case DiceType.Backstab:
        //         skill = ScriptableObject.CreateInstance<BackstabDiceSkillData>();
        //         break;
        //     case DiceType.Coin:
        //         skill = ScriptableObject.CreateInstance<CoinDiceSkillData>();
        //         break;
        //     case DiceType.BlindStrike:
        //         skill = ScriptableObject.CreateInstance<BlindStrikeDiceSkillData>();
        //         break;
        //     case DiceType.Stun:
        //         skill = ScriptableObject.CreateInstance<StunDiceSkillData>();
        //         break;
        //     case DiceType.Bomb:
        //         skill = ScriptableObject.CreateInstance<BombDiceSkillData>();
        //         break;
        //     case DiceType.Normal:
        //     default:
        //         skill = ScriptableObject.CreateInstance<NormalDiceSkillData>();
        //         break;
        // }

        skill.hideFlags = HideFlags.HideAndDontSave;
        cachedSkills[type] = skill;
        return skill;
    }
}
