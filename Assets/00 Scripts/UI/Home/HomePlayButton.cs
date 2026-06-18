using System.Collections.Generic;
using UnityEngine;

public class HomePlayButton : HomeFeatureButton
{
    public override void OnClick()
    {
        if (ChapterManager.Instance == null)
            return;

        List<Level> levels = ChapterManager.Instance.GetCurrentLevels();
        if (levels == null || levels.Count == 0)
        {
            Debug.LogWarning("Current chapter/mode has no level configured.");
            return;
        }
        //   int currentLevelIndex = Mathf.Max(0, IPlayerInfoController.Instance.CurrentLevel() - 1);
        //         if (currentLevelIndex >= levels.Count)
        //             currentLevelIndex = levels.Count - 1;
        int currentLevelIndex = Mathf.Clamp(ChapterManager.Instance.CurrentLevelIndex, 0, levels.Count - 1);
        ChapterManager.Instance.SetCurrentLevelIndex(currentLevelIndex);
        GameManager.Instance.PlayGame(EGameType.Campaign);
    }

    protected override void CheckActive()
    {
    }

    protected override void CheckNoti()
    {
    }
}
