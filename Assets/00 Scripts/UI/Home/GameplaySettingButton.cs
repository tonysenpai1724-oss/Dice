using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameplaySettingButton : HomeFeatureButton
{
    public override void OnClick()
    {
        UIManager.Instance.ShowPopupGameplaySetting();
    }

    protected override void CheckActive()
    {
    }

    protected override void CheckNoti()
    {
    }
}
