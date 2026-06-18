using TigerForge;
using UnityEngine;

public class HomeChapterModeButton : HomeFeatureButton
{
    public ChapterMode chapterMode = ChapterMode.Easy;
    public GameObject activeMarker;

    public override void OnClick()
    {
        if (ChapterManager.Instance == null)
            return;

        ChapterManager.Instance.SetCurrentMode(chapterMode);
        ChapterManager.Instance.SetCurrentLevelIndex(0);

        if (UIManager.Instance != null && UIManager.Instance.uIHome != null)
            UIManager.Instance.uIHome.InitHome();

        EventManager.EmitEvent(Constant.EVENT_ON_PLAYER_INFO_CHANGE);
    }

    protected override void CheckActive()
    {
        bool isActive = ChapterManager.Instance != null && ChapterManager.Instance.CurrentMode == chapterMode;

        if (activeMarker != null)
            activeMarker.SetActive(isActive);

        if (button != null)
            button.interactable = !isActive;
    }

    protected override void CheckNoti()
    {
    }
}
