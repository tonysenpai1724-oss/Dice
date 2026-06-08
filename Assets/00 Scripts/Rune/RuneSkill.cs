
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using System;
[CreateAssetMenu(menuName = "RuneSkill/Normal")]
public class NormalRuneSkillData : RuneSkillData
{
    public override RuneType RuneType => RuneType.Bomb;

    public override void Execute(RuneSkillContext context)
    {

    }
}

[CreateAssetMenu(menuName = "RuneSkill/BombRuneSkillData")]
public class BombRuneSkillData : RuneSkillData
{
    public override RuneType RuneType => RuneType.Bomb;

    public override void Execute(RuneSkillContext context)
    {

    }
}
[CreateAssetMenu(menuName = "RuneSkill/MinorLifeRuneSkillData")]
public class MinorLifeRuneSkillData : RuneSkillData
{
    public override RuneType RuneType => RuneType.MinorLife;

    public override void Execute(RuneSkillContext context)
    {

    }
}
[CreateAssetMenu(menuName = "RuneSkill/ShuffleRuneSkillData")]
public class ShuffleRuneSkillData : RuneSkillData
{
    public override RuneType RuneType => RuneType.Shuffle;

    public override void Execute(RuneSkillContext context)
    {

    }
}
[CreateAssetMenu(menuName = "RuneSkill/ProtectionRuneSkillData")]
public class ProtectionRuneSkillData : RuneSkillData
{
    public override RuneType RuneType => RuneType.Protection;

    public override void Execute(RuneSkillContext context)
    {
    }
}