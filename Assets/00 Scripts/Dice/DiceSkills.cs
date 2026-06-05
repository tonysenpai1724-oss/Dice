using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "RuneDice/Skill/Normal")]
public class NormalDiceSkillData : DiceSkillData
{
    public override DiceType TargetType => DiceType.Normal;

    public override void Execute(DiceSkillContext context)
    {
    }
}

[CreateAssetMenu(menuName = "RuneDice/Skill/Dodge")]
public class DodgeDiceSkillData : DiceSkillData
{
    public override DiceType TargetType => DiceType.Dodge;

    public int dodgeStacks = 1;

    public override void Execute(DiceSkillContext context)
    {
        if (context?.player == null)
            return;

        context.player.AddDodgeStacks(dodgeStacks);
    }
}

[CreateAssetMenu(menuName = "RuneDice/Skill/Poison")]
public class PoisonDiceSkillData : DiceSkillData
{
    public override DiceType TargetType => DiceType.Poison;

    public int poisonDamagePerTurn = 1;

    public override void Execute(DiceSkillContext context)
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
            poisonDamagePerTurn
        );
    }
}

[CreateAssetMenu(menuName = "RuneDice/Skill/Heal")]
public class HealDiceSkillData : DiceSkillData
{
    public override DiceType TargetType => DiceType.Heal;

    public override void Execute(DiceSkillContext context)
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

[CreateAssetMenu(menuName = "RuneDice/Skill/Shield")]
public class ShieldDiceSkillData : DiceSkillData
{
    public override DiceType TargetType => DiceType.Shield;

    public int shieldStacks = 1;

    public override void Execute(DiceSkillContext context)
    {
        if (context?.player == null)
            return;

        context.player.AddShieldStacks(shieldStacks);
    }
}

[CreateAssetMenu(menuName = "RuneDice/Skill/Backstab")]
public class BackstabDiceSkillData : DiceSkillData
{
    public override DiceType TargetType => DiceType.Backstab;

    public int bonusDamage = 2;

    public override void Execute(DiceSkillContext context)
    {
        context?.AddDamage(bonusDamage);
    }
}

[CreateAssetMenu(menuName = "RuneDice/Skill/Coin")]
public class CoinDiceSkillData : DiceSkillData
{
    public override DiceType TargetType => DiceType.Coin;

    public override void Execute(DiceSkillContext context)
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

[CreateAssetMenu(menuName = "RuneDice/Skill/Blind Strike")]
public class BlindStrikeDiceSkillData : DiceSkillData
{
    public override DiceType TargetType => DiceType.BlindStrike;

    public int damageReduction = 1;

    public override void Execute(DiceSkillContext context)
    {
        context?.enemyManager?.ReduceNextPlayerDamage(damageReduction);
    }
}

[CreateAssetMenu(menuName = "RuneDice/Skill/Stun")]
public class StunDiceSkillData : DiceSkillData
{
    public override DiceType TargetType => DiceType.Stun;

    public int stunTurns = 1;

    public override void Execute(DiceSkillContext context)
    {
        context?.enemyManager?.SkipNextEnemyTurns(stunTurns);
    }
}

[CreateAssetMenu(menuName = "RuneDice/Skill/Bomb")]
public class BombDiceSkillData : DiceSkillData
{
    public override DiceType TargetType => DiceType.Bomb;

    public int splashDamage = 1;

    public override void Execute(DiceSkillContext context)
    {
        if (context == null || context.enemyManager == null)
            return;

        context.AddAfterAttack(() =>
        {
            if (context.enemyManager != null)
            {
                context.enemyManager.DamageAllEnemies(splashDamage);
            }
        });
    }
}
