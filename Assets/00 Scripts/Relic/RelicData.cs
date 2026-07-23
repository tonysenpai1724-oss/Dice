using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "RuneDice/Relic/Relic Base")]
public class RelicData : SerializedScriptableObject
{
    public RelicType TargetType;
    public ERarity rarity = ERarity.Rare;
    public int stackApply = 1;
    public int valueApply = 1;
    public List<int> valueApplyByLevel = new();
    public Sprite relicSprite;
    [TextArea] public string description;

    public int GetValueApply(int level)
    {
        if (valueApplyByLevel != null && valueApplyByLevel.Count > 0)
        {
            int valueIndex = Mathf.Clamp(level, 1, valueApplyByLevel.Count) - 1;
            return valueApplyByLevel[valueIndex];
        }

        return valueApply * Mathf.Max(1, level);
    }

    public virtual void Execute(int level = 1)
    {
        int levelValueApply = GetValueApply(level);
        switch (TargetType)
        {
            case RelicType.RelicArmorTurn:
                RelicArmorTurnData.Execute(stackApply, levelValueApply);
                break;
        }
    }

    public DiceData ResolveDiceDataBeforeSkill(DiceData diceData, int level = 1)
    {
        int levelValueApply = GetValueApply(level);
        switch (TargetType)
        {
            case RelicType.RelicBomb6:
                return RelicBomb6Data.ResolveDiceData(diceData, levelValueApply);
            default:
                return diceData;
        }
    }

    public void ApplyBeforeDiceSkill(DiceData diceData, GameplayManager gameplay, int level = 1)
    {
        int levelValueApply = GetValueApply(level);
        switch (TargetType)
        {
            case RelicType.Relic2by2:
                Relic2by2Data.Execute(diceData, gameplay, levelValueApply);
                break;
            case RelicType.RelicLucky7:
                RelicLucky7Data.Execute(diceData, gameplay, levelValueApply);
                break;
        }
    }

    public void ModifyPlayerAttackDamage(PlayerAttackDamageContext context, int level = 1)
    {
        int levelValueApply = GetValueApply(level);
        switch (TargetType)
        {
            case RelicType.RelicDmgBurn:
                RelicDmgBurnData.Apply(context, levelValueApply);
                break;
            case RelicType.RelicDmgStun:
                RelicDmgStunData.Apply(context, levelValueApply);
                break;
            case RelicType.RelicDmgVulnerable:
                RelicDmgVulnerableData.Apply(context, levelValueApply);
                break;
            case RelicType.RelicDmgExhausted:
                RelicDmgExhaustedData.Apply(context, levelValueApply);
                break;
            case RelicType.RelicDmgCrit:
                RelicDmgCritData.Apply(context, levelValueApply);
                break;
            case RelicType.RelicDmgCounter:
                RelicDmgCounterData.Apply(context, levelValueApply);
                break;
            case RelicType.RelicDmgBoss:
                RelicDmgBossData.Apply(context, levelValueApply);
                break;
            case RelicType.RelicDmgDice1:
                RelicDmgDice1Data.Apply(context, levelValueApply);
                break;
            case RelicType.RelicDmgDiceS:
                RelicDmgDiceSData.Apply(context, levelValueApply);
                break;
            case RelicType.RelicDmgDiceM:
                RelicDmgDiceMData.Apply(context, levelValueApply);
                break;
            case RelicType.RelicDmgDiceL:
                RelicDmgDiceLData.Apply(context, levelValueApply);
                break;
            case RelicType.RelicBerserker:
                RelicBerserkerData.Apply(context, levelValueApply);
                break;
        }
    }

    public void NotifyPlayerDealtDamage(PlayerController player, int damage, int level = 1)
    {
        int levelValueApply = GetValueApply(level);
        switch (TargetType)
        {
            case RelicType.RelicDmgHeal:
                RelicDmgHealData.Execute(player, damage, levelValueApply);
                break;
        }
    }

    public bool ShouldCloneMergedDice(int level = 1)
    {
        if (TargetType != RelicType.RelicCloneMerge)
            return false;

        return RelicDataHelper.RollChance(GetValueApply(level));
    }
}

public enum RelicType
{
    RelicDmgBurn,
    RelicDmgStun,
    RelicDmgVulnerable,
    RelicDmgExhausted,
    RelicDmgCrit,
    RelicDmgCounter,
    RelicDmgBoss,
    RelicArmorTurn,
    RelicDmgHeal,
    RelicDmgDiceS,
    RelicDmgDiceM,
    RelicDmgDiceL,
    Relic2by2,
    RelicLucky7,
    RelicBomb6,
    RelicBerserker,
    RelicDmgDice1,
    RelicCloneMerge
}

public sealed class PlayerAttackDamageContext
{
    public PlayerController Player { get; }
    public Enemy Target { get; }
    public DiceData DiceData { get; }
    public bool IsCritical { get; }
    public bool IsCounter { get; }
    public int BaseDamage { get; }
    public int Damage { get; set; }
    public int DiceValue => DiceData != null ? Mathf.Max(1, DiceData.level) : 0;

    public PlayerAttackDamageContext(
        PlayerController player,
        Enemy target,
        DiceData diceData,
        int damage,
        bool isCritical,
        bool isCounter)
    {
        Player = player;
        Target = target;
        DiceData = diceData;
        BaseDamage = Mathf.Max(0, damage);
        Damage = BaseDamage;
        IsCritical = isCritical;
        IsCounter = isCounter;
    }

    public void AddPercentDamage(int percent)
    {
        if (percent <= 0 || BaseDamage <= 0)
            return;

        Damage += Mathf.RoundToInt(BaseDamage * (percent / 100f));
    }
}

public static class RelicDataHelper
{
    public static bool RollChance(int percent)
    {
        return percent > 0 && Random.value <= Mathf.Clamp01(percent / 100f);
    }

    public static bool HasEffect<T>(Enemy enemy) where T : GameEffect
    {
        T effect = enemy != null ? enemy.effectManager?.GetEffect<T>() : null;
        return effect != null && effect.IsActiveEffect;
    }
}

public static class RelicDmgBurnData
{
    public static void Apply(PlayerAttackDamageContext context, int valueApply)
    {
        if (context != null && RelicDataHelper.HasEffect<PoisonEffect>(context.Target))
            context.AddPercentDamage(valueApply);
    }
}

public static class RelicDmgStunData
{
    public static void Apply(PlayerAttackDamageContext context, int valueApply)
    {
        if (context != null && RelicDataHelper.HasEffect<EnemyTurnSkipEffect>(context.Target))
            context.AddPercentDamage(valueApply);
    }
}

public static class RelicDmgVulnerableData
{
    public static void Apply(PlayerAttackDamageContext context, int valueApply)
    {
        if (context != null && RelicDataHelper.HasEffect<VulnerableEffect>(context.Target))
            context.AddPercentDamage(valueApply);
    }
}

public static class RelicDmgExhaustedData
{
    public static void Apply(PlayerAttackDamageContext context, int valueApply)
    {
        if (context != null && RelicDataHelper.HasEffect<ExhaustEffect>(context.Target))
            context.AddPercentDamage(valueApply);
    }
}

public static class RelicDmgCritData
{
    public static void Apply(PlayerAttackDamageContext context, int valueApply)
    {
        if (context != null && context.IsCritical)
            context.AddPercentDamage(valueApply);
    }
}

public static class RelicDmgCounterData
{
    public static void Apply(PlayerAttackDamageContext context, int valueApply)
    {
        if (context != null && (context.IsCounter || context.DiceData != null && context.DiceData.type == DiceType.Counter))
            context.AddPercentDamage(valueApply);
    }
}

public static class RelicDmgBossData
{
    public static void Apply(PlayerAttackDamageContext context, int valueApply)
    {
        if (context?.Target == null)
            return;

        if (context.Target.enemyLevel == EnemyLevel.MiniBoss || context.Target.enemyLevel == EnemyLevel.Boss)
            context.AddPercentDamage(valueApply);
    }
}

public static class RelicArmorTurnData
{
    public static void Execute(int stackApply, int valueApply)
    {
        PlayerController player = EnemyManager.Instance != null ? EnemyManager.Instance.player : null;
        if (player == null)
            return;

        int armorAmount = Mathf.RoundToInt(player.RuntimeDefense * (Mathf.Max(0, valueApply) / 100f));
        if (armorAmount <= 0 && valueApply > 0)
            armorAmount = 1;

        ShieldEffect shieldEffect = player.effectManager?.AddEffect<ShieldEffect>();
        if (shieldEffect != null)
            shieldEffect.SetStacks(armorAmount * Mathf.Max(1, stackApply));
    }
}

public static class RelicDmgHealData
{
    public static void Execute(PlayerController player, int damage, int valueApply)
    {
        if (player == null || damage <= 0 || valueApply <= 0)
            return;

        int healAmount = Mathf.RoundToInt(damage * (valueApply / 100f));
        if (healAmount > 0)
            player.OnHeal(healAmount);
    }
}

public static class RelicDmgDiceSData
{
    public static void Apply(PlayerAttackDamageContext context, int valueApply)
    {
        if (context != null && (context.DiceValue == 2 || context.DiceValue == 3))
            context.AddPercentDamage(valueApply);
    }
}

public static class RelicDmgDice1Data
{
    public static void Apply(PlayerAttackDamageContext context, int valueApply)
    {
        if (context != null && context.DiceValue == 1)
            context.AddPercentDamage(valueApply);
    }
}

public static class RelicDmgDiceMData
{
    public static void Apply(PlayerAttackDamageContext context, int valueApply)
    {
        if (context != null && (context.DiceValue == 4 || context.DiceValue == 5))
            context.AddPercentDamage(valueApply);
    }
}

public static class RelicDmgDiceLData
{
    public static void Apply(PlayerAttackDamageContext context, int valueApply)
    {
        if (context != null && context.DiceValue > 5)
            context.AddPercentDamage(valueApply);
    }
}

public static class Relic2by2Data
{
    public static void Execute(DiceData diceData, GameplayManager gameplay, int valueApply)
    {
        if (diceData == null || gameplay == null || diceData.level != 2 || !RelicDataHelper.RollChance(valueApply))
            return;

        gameplay.AddAttackCount(gameplay.GetAttackCount());
    }
}

public static class RelicLucky7Data
{
    public static void Execute(DiceData diceData, GameplayManager gameplay, int valueApply)
    {
        if (diceData == null || gameplay == null || diceData.level != 7 || !RelicDataHelper.RollChance(valueApply))
            return;

        gameplay.SetAttackCount(Mathf.Max(gameplay.GetAttackCount(), 3));
    }
}

public static class RelicBomb6Data
{
    public static DiceData ResolveDiceData(DiceData diceData, int valueApply)
    {
        if (diceData == null || diceData.level != 6 || !RelicDataHelper.RollChance(valueApply))
            return diceData;

        DiceData bombDice = DiceManager.Instance != null
            ? DiceManager.Instance.GetDiceData(diceData.level, DiceType.Bomb)
            : null;
        return bombDice != null ? bombDice : diceData;
    }
}

public static class RelicBerserkerData
{
    public static void Apply(PlayerAttackDamageContext context, int valueApply)
    {
        if (context?.Player == null || valueApply <= 0)
            return;

        int lostHp = Mathf.Max(0, context.Player.hp - context.Player.currentHp);
        if (lostHp <= 0)
            return;

        context.Damage += Mathf.RoundToInt(lostHp * (valueApply / 100f));
    }
}
