$path = "Assets/00 Scripts/Gameplay/ChapterRewardChoiceOption.cs"
$text = [System.IO.File]::ReadAllText($path)
$text = $text.Replace("public enum ChapterRewardChoiceType`r`n{`r`n    UpgradeDice,", "public enum ChapterRewardChoiceType`r`n{`r`n    CloneDice,`r`n    UpgradeDice,")
[System.IO.File]::WriteAllText($path, $text)

$path = "Assets/00 Scripts/Gameplay/GameplayManager.cs"
$text = [System.IO.File]::ReadAllText($path)
$text = $text.Replace("        switch (option.type)`r`n        {`r`n            case ChapterRewardChoiceType.UpgradeDice:", "        switch (option.type)`r`n        {`r`n            case ChapterRewardChoiceType.CloneDice:`r`n                if (option.sourceDice != null)`r`n                    diceSession.CloneDiceData(option.sourceDice);`r`n                break;`r`n            case ChapterRewardChoiceType.UpgradeDice:")
[System.IO.File]::WriteAllText($path, $text)

$path = "Assets/00 Scripts/Gameplay/ChapterRewardChoiceBuilder.cs"
$text = [System.IO.File]::ReadAllText($path)
$old = @"
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
"@
$new = @"
        int rand = Random.Range(0, 4);
        switch (rand)
        {
            case 0:
                return GenerateCloneDiceReward();
            case 1:
                return GenerateUpgradeReward();
            case 2:
                return GenerateAddDiceReward();
            case 3:
                return GenerateRuneReward();
        }
"@
$text = $text.Replace($old, $new)
$old = @"
    ChapterRewardChoiceOption GenerateUpgradeReward()
"@
$new = @"
    ChapterRewardChoiceOption GenerateCloneDiceReward()
    {
        if (diceSession == null)
            return null;

        List<DiceData> cloneable = diceSession.GetCloneableDiceOptions();
        if (cloneable.Count == 0)
            return null;

        DiceData source = cloneable[Random.Range(0, cloneable.Count)];
        if (source == null)
            return null;

        return new ChapterRewardChoiceOption
        {
            type = ChapterRewardChoiceType.CloneDice,
            title = $"Clone {source.diceName}",
            description = $"Gain 1 extra Lv{source.level} {source.type} dice",
            sourceDice = source,
            targetDice = source
        };
    }
    ChapterRewardChoiceOption GenerateUpgradeReward()
"@
$text = $text.Replace($old, $new)
$old = @"
            switch (candidate.type)
            {
                case ChapterRewardChoiceType.UpgradeDice:
"@
$new = @"
            switch (candidate.type)
            {
                case ChapterRewardChoiceType.CloneDice:
                    if (current.sourceDice == candidate.sourceDice)
                        return true;
                    break;
                case ChapterRewardChoiceType.UpgradeDice:
"@
$text = $text.Replace($old, $new)
[System.IO.File]::WriteAllText($path, $text)

$path = "Assets/00 Scripts/Player/ChapterDiceSession.cs"
$text = [System.IO.File]::ReadAllText($path)
$old = @"
    public bool UpgradeDiceData(DiceData currentDiceData, DiceData upgradedDiceData)
    {
        if (currentDiceData == null || upgradedDiceData == null)
            return false;
"@
$new = @"
    public bool CanCloneAnyDice()
    {
        for (int i = 0; i < runtimeDiceDatas.Count; i++)
        {
            if (runtimeDiceDatas[i] != null)
                return true;
        }

        return false;
    }

    public List<DiceData> GetCloneableDiceOptions()
    {
        List<DiceData> result = new List<DiceData>();

        for (int i = 0; i < runtimeDiceDatas.Count; i++)
        {
            DiceData current = runtimeDiceDatas[i];
            if (current == null)
                continue;

            result.Add(current);
        }

        return result;
    }

    public bool CloneDiceData(DiceData sourceDiceData)
    {
        if (sourceDiceData == null)
            return false;

        for (int i = 0; i < runtimeDiceDatas.Count; i++)
        {
            if (runtimeDiceDatas[i] != sourceDiceData)
                continue;

            runtimeDiceDatas.Insert(i + 1, sourceDiceData);
            initializedFromHero = true;
            DebugLogRuntimeDice($"CloneDiceData before save cloned={sourceDiceData.diceName}");
            SaveSession();
            return true;
        }

        return false;
    }

    public bool UpgradeDiceData(DiceData currentDiceData, DiceData upgradedDiceData)
    {
        if (currentDiceData == null || upgradedDiceData == null)
            return false;
"@
$text = $text.Replace($old, $new)
[System.IO.File]::WriteAllText($path, $text)