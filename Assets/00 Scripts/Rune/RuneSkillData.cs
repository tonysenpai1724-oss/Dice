
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using System;
public sealed class RuneSkillContext
{
    public DiceData diceData;
    public DiceQueue queue;
    public EnemyManager enemyManager;
    public PlayerController player;
    public Dice dice;
    public Enemy targetEnemy;

    public int damage;
    public bool skipAttack;

    public RuneSkillContext(
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
}
public abstract class RuneSkillData : SerializedScriptableObject
{
    public abstract RuneType RuneType { get; }

    public abstract void Execute(RuneSkillContext context);
}
public static class RuneSkillFactory
{
    static readonly System.Collections.Generic.Dictionary<RuneType, RuneSkillData> cachedSkills =
        new();

    public static RuneSkillData Create(RuneType type)
    {
        if (cachedSkills.TryGetValue(type, out RuneSkillData cachedSkill) &&
               cachedSkill != null)
        {
            return cachedSkill;
        }
        RuneSkillData skill;

        switch (type)
        {
            case RuneType.Bomb:
                skill = ScriptableObject.CreateInstance<BombRuneSkillData>();
                break;
            case RuneType.MinorLife:
                skill = ScriptableObject.CreateInstance<MinorLifeRuneSkillData>();
                break;
            case RuneType.Shuffle:
                skill = ScriptableObject.CreateInstance<ShuffleRuneSkillData>();
                break;
            case RuneType.Protection:
                skill = ScriptableObject.CreateInstance<ProtectionRuneSkillData>();
                break;
            default:
                skill = ScriptableObject.CreateInstance<NormalRuneSkillData>();
                break;

        }
        skill.hideFlags = HideFlags.HideAndDontSave;
        cachedSkills[type] = skill;
        return skill;
    }
}

public enum RuneType
{
    Bomb,
    MinorLife,
    Shuffle,
    Protection
}
