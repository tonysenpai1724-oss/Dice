using TigerForge;
using UnityEngine;
using TMPro;

public class WaveChapterUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI waveCountText;

    void OnEnable()
    {
        RefreshWave();
        EventManager.StartListening(Constant.EVENT_LEVEL_INITED, RefreshWave);
        EventManager.StartListening(Constant.ON_WIN_LEVEL, RefreshWave);
        EventManager.StartListening(Constant.ON_LOSE_LEVEL, RefreshWave);
    }

    void OnDisable()
    {
        EventManager.StopListening(Constant.EVENT_LEVEL_INITED, RefreshWave);
        EventManager.StopListening(Constant.ON_WIN_LEVEL, RefreshWave);
        EventManager.StopListening(Constant.ON_LOSE_LEVEL, RefreshWave);
    }

    public void SetWave(int wave, int totalWaves)
    {

        if (waveCountText != null)
            waveCountText.text = $"WAVE {Mathf.Max(1, wave)}/{Mathf.Max(1, totalWaves)}";
    }

    void RefreshWave()
    {
        if (ChapterManager.Instance == null)
            return;

        int currentWave = ChapterManager.Instance.CurrentLevelIndex + 1;
        int totalWaves = ChapterManager.Instance.GetCurrentLevels()?.Count ?? currentWave;
        SetWave(currentWave, totalWaves);
    }
}
