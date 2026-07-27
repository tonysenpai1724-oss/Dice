using TigerForge;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

public class WaveChapterUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI waveCountText;
    [Header("Progress")]
    [SerializeField] private Image progressFill;
    [SerializeField] private Image startMilestoneImage;
    [SerializeField] private Image middleMilestoneImage;
    [SerializeField] private Image endMilestoneImage;
    [SerializeField] private RectTransform arrowRect;
    [SerializeField] private Sprite activeMilestoneSprite;
    [SerializeField] private Sprite inactiveMilestoneSprite;
    [SerializeField] private float progressTweenDuration = 0.35f;
    [SerializeField] private Vector2 arrowLocalOffset = new Vector2(-15f, -5f);

    private Tween progressTween;
    private int lastDisplayedWave = -1;
    private int lastDisplayedTotalWaves = -1;
    private bool hasInitialized;

    void OnEnable()
    {
        RefreshWave(false);
        EventManager.StartListening(Constant.EVENT_LEVEL_INITED, OnLevelInited);
        EventManager.StartListening(Constant.ON_WIN_LEVEL, OnWinLevel);
        EventManager.StartListening(Constant.ON_LOSE_LEVEL, OnLoseLevel);
    }

    void OnDisable()
    {
        progressTween?.Kill();
        EventManager.StopListening(Constant.EVENT_LEVEL_INITED, OnLevelInited);
        EventManager.StopListening(Constant.ON_WIN_LEVEL, OnWinLevel);
        EventManager.StopListening(Constant.ON_LOSE_LEVEL, OnLoseLevel);
    }

    public void SetWave(int wave, int totalWaves, bool animateProgress = false)
    {
        int safeTotalWaves = Mathf.Max(1, totalWaves);
        int safeWave = Mathf.Clamp(wave, 1, safeTotalWaves);

        if (waveCountText != null)
            waveCountText.text = $"WAVE {safeWave}/{safeTotalWaves}";

        UpdateProgress(safeWave, safeTotalWaves, animateProgress && hasInitialized);
        UpdateArrowPosition(safeWave, safeTotalWaves);
        lastDisplayedWave = safeWave;
        lastDisplayedTotalWaves = safeTotalWaves;
        hasInitialized = true;
    }

    void OnLevelInited()
    {
        RefreshWave(false);
    }

    void OnLoseLevel()
    {
        RefreshWave(false);
    }

    void OnWinLevel()
    {
        if (ShouldAnimateChapterCompleteTransition())
        {
            AnimateChapterCompleteTransition();
            return;
        }

        RefreshWave(true);
    }

    void RefreshWave(bool animateProgress)
    {
        if (ChapterManager.Instance == null)
            return;

        int currentWave = ChapterManager.Instance.CurrentLevelIndex + 1;
        int totalWaves = ChapterManager.Instance.GetCurrentLevels()?.Count ?? currentWave;
        SetWave(currentWave, totalWaves, animateProgress);
    }

    void UpdateProgress(int wave, int totalWaves, bool animateProgress)
    {
        UpdateMilestoneSprites(wave, totalWaves);

        if (progressFill == null)
            return;

        float targetFill = GetProgressFillAmount(wave, totalWaves);
        progressTween?.Kill();

        if (animateProgress && progressTweenDuration > 0f && progressFill.gameObject.activeInHierarchy)
            progressTween = progressFill.DOFillAmount(targetFill, progressTweenDuration).SetEase(Ease.OutCubic);
        else
            progressFill.fillAmount = targetFill;
    }

    void UpdateMilestoneSprites(int wave, int totalWaves)
    {
        SetMilestoneSprite(startMilestoneImage, wave >= 1);
        SetMilestoneSprite(middleMilestoneImage, wave >= GetMiddleWave(totalWaves));
        SetMilestoneSprite(endMilestoneImage, wave >= totalWaves);
    }

    void UpdateArrowPosition(int wave, int totalWaves)
    {
        if (arrowRect == null)
            return;

        RectTransform targetRect = GetArrowTargetRect(wave, totalWaves);
        if (targetRect == null)
            return;

        if (arrowRect.parent != targetRect)
            arrowRect.SetParent(targetRect, false);

        arrowRect.anchorMin = new Vector2(0.5f, 0.5f);
        arrowRect.anchorMax = new Vector2(0.5f, 0.5f);
        arrowRect.pivot = new Vector2(0.5f, 0.5f);
        arrowRect.localScale = Vector3.one;
        arrowRect.anchoredPosition = arrowLocalOffset;
    }

    RectTransform GetArrowTargetRect(int wave, int totalWaves)
    {
        if (wave >= totalWaves)
            return endMilestoneImage != null ? endMilestoneImage.rectTransform : null;

        if (wave >= GetMiddleWave(totalWaves))
            return middleMilestoneImage != null ? middleMilestoneImage.rectTransform : null;

        return startMilestoneImage != null ? startMilestoneImage.rectTransform : null;
    }

    void SetMilestoneSprite(Image targetImage, bool isReached)
    {
        if (targetImage == null)
            return;

        if (activeMilestoneSprite == null || inactiveMilestoneSprite == null)
            return;

        targetImage.sprite = isReached ? activeMilestoneSprite : inactiveMilestoneSprite;
    }

    float GetProgressFillAmount(int wave, int totalWaves)
    {
        if (totalWaves <= 1)
            return 1f;

        return Mathf.Clamp01((float)(wave - 1) / (totalWaves - 1));
    }

    int GetMiddleWave(int totalWaves)
    {
        return Mathf.Clamp(Mathf.CeilToInt(totalWaves * 0.5f), 1, totalWaves);
    }

    bool ShouldAnimateChapterCompleteTransition()
    {
        if (!hasInitialized || ChapterManager.Instance == null)
            return false;

        int currentWave = ChapterManager.Instance.CurrentLevelIndex + 1;
        int totalWaves = ChapterManager.Instance.GetCurrentLevels()?.Count ?? currentWave;

        return lastDisplayedTotalWaves > 0
            && lastDisplayedWave >= lastDisplayedTotalWaves
            && currentWave == 1
            && (currentWave != lastDisplayedWave || totalWaves != lastDisplayedTotalWaves);
    }

    void AnimateChapterCompleteTransition()
    {
        if (ChapterManager.Instance == null)
            return;

        int nextWave = ChapterManager.Instance.CurrentLevelIndex + 1;
        int nextTotalWaves = ChapterManager.Instance.GetCurrentLevels()?.Count ?? nextWave;

        if (waveCountText != null)
            waveCountText.text = $"WAVE {Mathf.Max(1, lastDisplayedWave)}/{Mathf.Max(1, lastDisplayedTotalWaves)}";

        UpdateMilestoneSprites(lastDisplayedTotalWaves, lastDisplayedTotalWaves);

        if (progressFill == null || progressTweenDuration <= 0f || !progressFill.gameObject.activeInHierarchy)
        {
            SetWave(nextWave, nextTotalWaves, false);
            return;
        }

        progressTween?.Kill();
        progressTween = progressFill
            .DOFillAmount(1f, progressTweenDuration)
            .SetEase(Ease.OutCubic)
            .OnComplete(() => SetWave(nextWave, nextTotalWaves, false));
    }
}
