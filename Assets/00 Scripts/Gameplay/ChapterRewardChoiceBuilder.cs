using System.Collections.Generic;
using UnityEngine;
public class ChapterRewardChoiceBuilder
{
    readonly DiceDatabaseSO diceDatabase;
    readonly ChapterDiceSession diceSession;
    readonly List<RuneSkillData> runes;
    public ChapterRewardChoiceBuilder(DiceDatabaseSO diceDatabase, ChapterDiceSession diceSession, List<RuneSkillData> runes)
    {
        this.diceDatabase = diceDatabase;
        this.diceSession = diceSession;
        this.runes = runes;
    }
    public List<ChapterRewardChoiceOption> BuildChoices(int maxOptions = 3, int maxAttempts = 30)
    {
        List<ChapterRewardChoiceOption> options = new List<ChapterRewardChoiceOption>();
        while (options.Count < maxOptions && maxAttempts-- > 0)
        {
            ChapterRewardChoiceOption option = GenerateRandomReward();
            if (option == null)
                continue;
            if (!ContainsSameRewardOption(options, option))
                options.Add(option);
        }
        return options;
    }
    ChapterRewardChoiceOption GenerateRandomReward()
    {
        int rand = Random.Range(0, 3);
        switch (rand)
        {
            case 0:
                return GenerateUpgradeReward();
            case 1:
                return GenerateAddDiceReward();
            case 2:
                return GenerateRuneReward();
        }
        return null;
    }
    ChapterRewardChoiceOption GenerateUpgradeReward()
    {
        if (diceDatabase == null || diceSession == null)
            return null;
        List<DiceData> upgradeable = diceSession.GetUpgradeableDiceOptions(diceDatabase);
        if (upgradeable.Count == 0)
            return null;
        DiceData source = upgradeable[Random.Range(0, upgradeable.Count)];
        DiceData target = diceDatabase.GetDiceData(source.level + 1, source.type);
        if (target == null)
            return null;
        return new ChapterRewardChoiceOption
        {
            type = ChapterRewardChoiceType.UpgradeDice,
            title = $"Upgrade {source.diceName}",
            description = $"Upgrade to Lv{target.level}",
            sourceDice = source,
            targetDice = target
        };
    }
    ChapterRewardChoiceOption GenerateAddDiceReward()
    {
        if (diceDatabase == null || diceSession == null)
            return null;
        List<DiceData> addable = diceSession.GetAddableDiceOptions(diceDatabase);
        if (addable.Count == 0)
            return null;
        DiceData dice = addable[Random.Range(0, addable.Count)];
        return new ChapterRewardChoiceOption
        {
            type = ChapterRewardChoiceType.AddDice,
            title = $"Add {dice.diceName}",
            description = $"Gain 1 new {dice.type} dice",
            targetDice = dice
        };
    }
    ChapterRewardChoiceOption GenerateRuneReward()
    {
        if (runes == null || runes.Count == 0)
            return null;
        RuneSkillData rune = runes[Random.Range(0, runes.Count)];
        if (rune == null)
            return null;
        return new ChapterRewardChoiceOption
        {
            type = ChapterRewardChoiceType.AddRune,
            title = $"Add Rune {rune.TargetType}",
            description = "Gain 1 rune for this run",
            runeSkill = rune
        };
    }
    bool ContainsSameRewardOption(List<ChapterRewardChoiceOption> currentOptions, ChapterRewardChoiceOption candidate)
    {
        if (currentOptions == null || candidate == null)
            return false;
        for (int i = 0; i < currentOptions.Count; i++)
        {
            ChapterRewardChoiceOption current = currentOptions[i];
            if (current == null)
                continue;
            if (current.type != candidate.type)
                continue;
            switch (candidate.type)
            {
                case ChapterRewardChoiceType.UpgradeDice:
                    if (current.sourceDice == candidate.sourceDice && current.targetDice == candidate.targetDice)
                        return true;
                    break;
                case ChapterRewardChoiceType.AddDice:
                    if (current.targetDice == candidate.targetDice)
                        return true;
                    break;
                case ChapterRewardChoiceType.AddRune:
                    if (current.runeSkill == candidate.runeSkill)
                        return true;
                    break;
            }
        }
        return false;
    }
}
