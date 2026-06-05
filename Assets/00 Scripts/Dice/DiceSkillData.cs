using UnityEngine;

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

public abstract class DiceSkillData : ScriptableObject
{
    public abstract DiceType TargetType { get; }

    public abstract void Execute(DiceSkillContext context);
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

        DiceSkillData skill;

        switch (type)
        {
            case DiceType.Dodge:
                skill = ScriptableObject.CreateInstance<DodgeDiceSkillData>();
                break;
            case DiceType.Poison:
                skill = ScriptableObject.CreateInstance<PoisonDiceSkillData>();
                break;
            case DiceType.Heal:
                skill = ScriptableObject.CreateInstance<HealDiceSkillData>();
                break;
            case DiceType.Shield:
                skill = ScriptableObject.CreateInstance<ShieldDiceSkillData>();
                break;
            case DiceType.Backstab:
                skill = ScriptableObject.CreateInstance<BackstabDiceSkillData>();
                break;
            case DiceType.Coin:
                skill = ScriptableObject.CreateInstance<CoinDiceSkillData>();
                break;
            case DiceType.BlindStrike:
                skill = ScriptableObject.CreateInstance<BlindStrikeDiceSkillData>();
                break;
            case DiceType.Stun:
                skill = ScriptableObject.CreateInstance<StunDiceSkillData>();
                break;
            case DiceType.Bomb:
                skill = ScriptableObject.CreateInstance<BombDiceSkillData>();
                break;
            case DiceType.Normal:
            default:
                skill = ScriptableObject.CreateInstance<NormalDiceSkillData>();
                break;
        }

        skill.hideFlags = HideFlags.HideAndDontSave;
        cachedSkills[type] = skill;
        return skill;
    }
}
