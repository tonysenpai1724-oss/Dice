$path = "Assets/00 Scripts/Gameplay/ChapterRewardChoiceOption.cs"
$text = [System.IO.File]::ReadAllText($path)
$text = $text.Replace("    CloneDice,`r`n", "")
[System.IO.File]::WriteAllText($path, $text)

$path = "Assets/00 Scripts/Gameplay/ChapterRewardChoiceBuilder.cs"
$text = [System.IO.File]::ReadAllText($path)
$old = @"
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
$new = @"
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
$text = $text.Replace($old, $new)
$cloneMethod = @"
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
"@
$text = $text.Replace($cloneMethod, "")
$old = @"
            switch (candidate.type)
            {
                case ChapterRewardChoiceType.CloneDice:
                    if (current.sourceDice == candidate.sourceDice)
                        return true;
                    break;
                case ChapterRewardChoiceType.UpgradeDice:
"@
$new = @"
            switch (candidate.type)
            {
                case ChapterRewardChoiceType.UpgradeDice:
"@
$text = $text.Replace($old, $new)
[System.IO.File]::WriteAllText($path, $text)

$path = "Assets/00 Scripts/Gameplay/GameplayManager.cs"
$text = [System.IO.File]::ReadAllText($path)
$old = @"
        switch (option.type)
        {
            case ChapterRewardChoiceType.CloneDice:
                if (option.sourceDice != null)
                    diceSession.CloneDiceData(option.sourceDice);
                break;
            case ChapterRewardChoiceType.UpgradeDice:
"@
$new = @"
        switch (option.type)
        {
            case ChapterRewardChoiceType.UpgradeDice:
"@
$text = $text.Replace($old, $new)
[System.IO.File]::WriteAllText($path, $text)

$path = "Assets/00 Scripts/Chapter/ClonePanel.cs"
$text = @"
using UnityEngine;

public class ClonePanel : MonoBehaviour
{
    public static ClonePanel Instance;

    void Awake()
    {
        Instance = this;
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
"@
[System.IO.File]::WriteAllText($path, $text)

$path = "Assets/00 Scripts/Manager/UIManager.cs"
$text = [System.IO.File]::ReadAllText($path)
$insertAfter = @"
    public void ShowPopupChoice(List<ChapterRewardChoiceOption> options)
    {
        PopupChapterRewardChoice ui = GetUI("Popup Chapter Reward Choice") as PopupChapterRewardChoice;
        if (ui == null)
            return;

        ui.ShowChoices(options);
    }
"@
$insertNew = @"
    public void ShowPopupChoice(List<ChapterRewardChoiceOption> options)
    {
        PopupChapterRewardChoice ui = GetUI("Popup Chapter Reward Choice") as PopupChapterRewardChoice;
        if (ui == null)
            return;

        ui.ShowChoices(options);
    }

    public void ShowClonePanel()
    {
        ClonePanel ui = GetUI("Clone Panel") as ClonePanel;
        if (ui == null)
            return;

        ui.Show();
    }
"@
$text = $text.Replace($insertAfter, $insertNew)
[System.IO.File]::WriteAllText($path, $text)

$path = "Assets/00 Scripts/Gameplay/LevelManager.cs"
$text = [System.IO.File]::ReadAllText($path)
$old = @"
    public void LoadLevel(Level level)
    {
        if (level == null)
            return;

        currentLevel = level;

        if (EnemyManager.Instance != null)
            EnemyManager.Instance.StartLevel(level);
    }
"@
$new = @"
    public void LoadLevel(Level level)
    {
        if (level == null)
            return;

        currentLevel = level;

        if (level.leveltype == LevelType.MagicAltar)
        {
            UIManager.Instance?.ShowClonePanel();
            return;
        }

        if (EnemyManager.Instance != null)
            EnemyManager.Instance.StartLevel(level);
    }
"@
$text = $text.Replace($old, $new)
[System.IO.File]::WriteAllText($path, $text)