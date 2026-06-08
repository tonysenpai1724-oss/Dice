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
        if (context?.player == null) return;

        context.player.AddDodgeStacks(stackApply);
    }
}

public static class PoisonDiceSkillData
{
    public static void Execute(DiceSkillContext context, int stackApply, int valueApply)
    {
        if (context == null || context.enemyManager == null)
            return;

        Enemy target =
            context.targetEnemy != null &&
            context.targetEnemy.gameObject.activeInHierarchy
                ? context.targetEnemy
                : context.enemyManager.GetNearestAliveEnemy();

        if (target == null)
            return;

        int poisonTurns =
            Mathf.Max(
                1,
                context.diceData != null
                    ? context.diceData.damage
                    : 1
            );

        context.SetDamage(0);
        context.skipAttack = true;
        context.enemyManager.ApplyPoison(
            target,
            poisonTurns,
            valueApply
        );
    }
}

public static class HealDiceSkillData
{

    public static void Execute(DiceSkillContext context, int stackApply, int valueApply)
    {
        if (context?.player == null)
            return;

        int healAmount =
            Mathf.Max(
                0,
                context.diceData != null
                    ? context.diceData.damage
                    : 0
            );

        context.SetDamage(0);
        context.skipAttack = true;
        context.player.Heal(healAmount);
    }
}

public static class ShieldDiceSkillData
{

    public static void Execute(DiceSkillContext context, int stackApply, int valueApply)
    {
        if (context?.player == null)
            return;

        context.player.AddShieldStacks(stackApply);
    }
}

public static class BackstabDiceSkillData
{

    public static void Execute(DiceSkillContext context, int stackApply, int valueApply)
    {
        context?.AddDamage(valueApply);
    }
}

public static class CoinDiceSkillData
{

    public static void Execute(DiceSkillContext context, int stackApply, int valueApply)
    {
        if (context == null)
            return;

        int coinReward =
            Mathf.Max(
                0,
                context.diceData != null
                    ? context.diceData.damage
                    : 0
            );

        context.SetDamage(0);
        context.skipAttack = true;

        if (coinReward <= 0 || IPlayerResource.Instance == null)
            return;

        IPlayerResource.Instance.AddResource(
            new List<GameResource>
            {
                new CommonResource(ECommonResource.Coin, coinReward)
            },
            EResourceFrom.TimeReward
        );
    }
}

public static class BlindStrikeDiceSkillData
{

    public static void Execute(DiceSkillContext context, int stackApply, int valueApply)
    {
        context?.enemyManager?.ReduceNextPlayerDamage(valueApply);
    }
}

public static class StunDiceSkillData
{

    public static void Execute(DiceSkillContext context, int stackApply, int valueApply)
    {
        context?.enemyManager?.SkipNextEnemyTurns(stackApply);
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
            if (context.enemyManager != null)
            {
                context.enemyManager.DamageAllEnemies(valueApply);
            }
        });
    }
}

public sealed class DiceSkillContext
{
    readonly System.Collections.Generic.List<System.Action> afterAttackActions =
        new();

    public DiceData diceData;
    public DiceQueue queue;
    public EnemyManager enemyManager;
    public PlayerController player;
    public Dice dice;
    public Enemy targetEnemy;

    public int damage;
    public bool skipAttack;

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
