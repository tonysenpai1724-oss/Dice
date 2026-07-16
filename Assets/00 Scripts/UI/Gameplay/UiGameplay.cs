using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UiGameplay : MonoBehaviour
{
    public TextMeshProUGUI txtLevel, txtTimer, txtTut, txtScore, txtSpeed;
    public GameObject tutBox;
    [SerializeField] private Transform popupRoot;

    public Transform PopupRoot => popupRoot != null ? popupRoot : transform;

    private void Start()
    {
        UIManager.Instance.uIGameplay = this;
        TigerForge.EventManager.StartListening(Constant.On_Speed_Changed, OnSpeedChanged);
    }

    public void Initialize()
    {
        HideTextTut();
        TigerForge.EventManager.StartListening(Constant.EVENT_TIMER_TICK, OnTick);
        InitLevel();
        TigerForge.EventManager.StartListening(Constant.EVENT_LEVEL_INITED, InitLevel);
        OnTick();

    }
    public void OnSpeedChanged()
    {
        txtSpeed.text = $"{Time.timeScale}x";
    }
    void InitLevel()
    {
        // if (GameManager.Instance.GameType == EGameType.Campaign)
        // {
        //     txtLevel.text = $"Level {GameplayManager.Instance.CurrentLevel}";
        //     txtScore.gameObject.SetActive(false);
        // }
        // else
        // {
        //     txtScore.gameObject.SetActive(true);
        // }
    }
    void OnTick()
    {
        // txtTimer.text = Helper.TimeToString(System.TimeSpan.FromSeconds(GameplayManager.Instance.LevelTime));
        // txtScore.text = $"Score: {GameplayManager.Instance.Score}";
    }
    public void OnClickPauseGame()
    {
        if (GameplayManager.Instance.State == EGamePlayState.Running)
            UIManager.Instance.ShowPopupPauseGame();
    }

    public void ShowTextTut(string txt)
    {
        // txtTut.text = txt;
        // tutBox.SetActive(true);
    }
    public void HideTextTut()
    {
        // tutBox.SetActive(false);
    }
}