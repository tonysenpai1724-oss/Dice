using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;

public class UiHome : MonoBehaviour
{
    public Transform coinBar, heartBar;
    public List<HomeToggleButton> homeToggleButtons;
    public List<HomePanel> homePanels;
    public GameObject rollPlane;
    public static UiHome Instance;
    public Transform rollPlaneAnchor;
    void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        UIManager.Instance.uIHome = this;
        ShowRollPlane(false);
    }
    public void InitHome()
    {
        homeToggleButtons[1].OnClick();
        RefreshHomePanels();
    }

    public void RefreshHomePanels()
    {
        foreach (var item in homePanels)
        {
            item.InitFirstTime();
        }
    }
    public void OnClickHomeButton(HomeToggleButton button)
    {
        for (int i = 0; i < homeToggleButtons.Count; i++)
        {
            homeToggleButtons[i].SetActive(homeToggleButtons[i] == button);
            homePanels[i].SetActive(homeToggleButtons[i] == button);
        }
    }

    public void ShowRollPlane(bool show)
    {
        if (rollPlane != null)
            rollPlane.SetActive(show);
    }
}
