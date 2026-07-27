using System;
using System.Collections.Generic;
using UnityEngine;

public enum ChapterRewardChoiceType
{
    UpgradeDice,
    AddDice,
    AddRune,
    Relic
}

[Serializable]
public class ChapterRewardChoiceOption
{
    public ChapterRewardChoiceType type;
    public string title;
    public string description;
    public DiceData sourceDice;
    public DiceData targetDice;
    public RuneSkillData runeSkill;
    public RelicData relicData;
}
