using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopupGameplaySetting : UIBase
{
    public Button replayBtn;
    public Button homeBtn;

    public override void Show()
    {
        base.Show();

        if (replayBtn != null)
        {
            replayBtn.onClick.RemoveAllListeners();
            replayBtn.onClick.AddListener(Replay);
        }

        if (homeBtn != null)
        {
            homeBtn.onClick.RemoveAllListeners();
            homeBtn.onClick.AddListener(Home);
        }
    }

    public void Replay()
    {
        PlayerStats.Shared.ClearTemporaryStats();
        CloseImmediately();
        GameManager.Instance.ReplayGame();
    }

    public void Home()
    {
        PlayerStats.Shared.ClearTemporaryStats();
        CloseImmediately();
        GameManager.Instance.GoSceneHome();
    }

    void CloseImmediately()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.Close(this);

        gameObject.SetActive(false);
    }
}
