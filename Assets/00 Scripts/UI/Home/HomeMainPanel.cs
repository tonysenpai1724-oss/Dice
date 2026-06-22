using Sirenix.OdinInspector;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TigerForge;

public class HomeMainPanel : HomePanel
{
    public HomeLevelItem itemPrefab;
    public Transform itemParent;
    public TextMeshProUGUI txtChapter;
    List<HomeLevelItem> lstItems = new List<HomeLevelItem>();

    public int scrollSpacingBottom, itemHeight;
    public RectTransform scrollTransform;
    public Button selectChapter;
    public TMP_Dropdown chapterDropdown;
    public TMP_Dropdown levelDropdown;

    bool isRefreshingDropdowns;

    public override void InitFirstTime()
    {
        BindControls();
        InitLevel();
    }

    void OnEnable()
    {
        BindControls();
        EventManager.StartListening(Constant.EVENT_ON_PLAYER_INFO_CHANGE, RefreshView);
    }

    void OnDisable()
    {
        EventManager.StopListening(Constant.EVENT_ON_PLAYER_INFO_CHANGE, RefreshView);

        if (chapterDropdown != null)
            chapterDropdown.onValueChanged.RemoveListener(OnChapterDropdownChanged);

        if (levelDropdown != null)
            levelDropdown.onValueChanged.RemoveListener(OnLevelDropdownChanged);

        if (selectChapter != null)
            selectChapter.onClick.RemoveListener(SelectChapter);
    }

    void BindControls()
    {
        if (selectChapter != null)
        {
            selectChapter.onClick.RemoveListener(SelectChapter);
            selectChapter.onClick.AddListener(SelectChapter);
        }

        if (chapterDropdown != null)
        {
            chapterDropdown.onValueChanged.RemoveListener(OnChapterDropdownChanged);
            chapterDropdown.onValueChanged.AddListener(OnChapterDropdownChanged);
        }

        if (levelDropdown != null)
        {
            levelDropdown.onValueChanged.RemoveListener(OnLevelDropdownChanged);
            levelDropdown.onValueChanged.AddListener(OnLevelDropdownChanged);
        }
    }

    void RefreshView()
    {
        InitLevel();
    }

    void InitLevel()
    {
        int need = GetDisplayLevelCount();
        int has = lstItems.Count;
        for (int i = 0; i < need - has; i++)
        {
            lstItems.Add(Instantiate(itemPrefab, itemParent));
        }

        for (int i = 0; i < lstItems.Count; i++)
        {
            bool active = i < need;
            lstItems[i].gameObject.SetActive(active);
            if (active)
                lstItems[i].InitLevel(i + 1);
        }

        UpdateChapterLabel();
        RefreshDropdowns();

        if (scrollTransform == null)
            return;

        scrollTransform.gameObject.SetActive(false);
        Canvas.ForceUpdateCanvases();
        scrollTransform.gameObject.SetActive(true);
        SnapLevel((ChapterManager.Instance != null ? ChapterManager.Instance.CurrentLevelIndex : 0) + 1);
    }

    int GetDisplayLevelCount()
    {
        if (ChapterManager.Instance != null)
        {
            List<Level> levels = ChapterManager.Instance.GetCurrentLevels();
            if (levels != null && levels.Count > 0)
                return levels.Count;
        }

        return IPlayerInfoController.Instance.MaxLevel();
    }

    void UpdateChapterLabel()
    {
        if (txtChapter == null)
            return;

        if (ChapterManager.Instance == null)
        {
            txtChapter.text = "Chapter";
            return;
        }

        txtChapter.text = $"Chapter {ChapterManager.Instance.CurrentChapterId} - {ChapterManager.Instance.CurrentMode}";
    }

    void RefreshDropdowns()
    {
        if (ChapterManager.Instance == null)
            return;

        isRefreshingDropdowns = true;

        RefreshChapterDropdown();
        RefreshLevelDropdown();

        isRefreshingDropdowns = false;
    }

    void RefreshChapterDropdown()
    {
        if (chapterDropdown == null)
            return;

        chapterDropdown.ClearOptions();

        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        int selectedIndex = 0;
        int optionIndex = 0;

        for (int chapterId = 1; chapterId <= 999; chapterId++)
        {
            ChapterData chapter = ChapterManager.Instance.GetChapter(chapterId);
            if (chapter == null)
                break;

            List<Level> levels = ChapterManager.Instance.GetLevels(chapterId, ChapterManager.Instance.CurrentMode);
            if (levels == null || levels.Count == 0)
                continue;

            string chapterLabel = string.IsNullOrEmpty(chapter.chapterName)
                ? $"Chapter {chapterId}"
                : $"Chapter {chapterId} - {chapter.chapterName}";
            options.Add(new TMP_Dropdown.OptionData(chapterLabel));

            if (chapterId == ChapterManager.Instance.CurrentChapterId)
                selectedIndex = optionIndex;

            optionIndex++;
        }

        chapterDropdown.AddOptions(options);
        if (options.Count > 0)
            chapterDropdown.SetValueWithoutNotify(Mathf.Clamp(selectedIndex, 0, options.Count - 1));
    }

    void RefreshLevelDropdown()
    {
        if (levelDropdown == null)
            return;

        levelDropdown.ClearOptions();

        List<Level> levels = ChapterManager.Instance.GetCurrentLevels();
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();

        if (levels != null)
        {
            for (int i = 0; i < levels.Count; i++)
            {
                int levelNumber = i + 1;
                options.Add(new TMP_Dropdown.OptionData($"Level {levelNumber}"));
            }
        }

        levelDropdown.AddOptions(options);

        if (options.Count > 0)
        {
            int selectedIndex = Mathf.Clamp(ChapterManager.Instance.CurrentLevelIndex, 0, options.Count - 1);
            levelDropdown.SetValueWithoutNotify(selectedIndex);
        }
    }

    void OnChapterDropdownChanged(int index)
    {
        if (isRefreshingDropdowns || ChapterManager.Instance == null || chapterDropdown == null)
            return;

        int validIndex = 0;
        for (int chapterId = 1; chapterId <= 999; chapterId++)
        {
            ChapterData chapter = ChapterManager.Instance.GetChapter(chapterId);
            if (chapter == null)
                break;

            List<Level> levels = ChapterManager.Instance.GetLevels(chapterId, ChapterManager.Instance.CurrentMode);
            if (levels == null || levels.Count == 0)
                continue;

            if (validIndex == index)
            {
                ChapterManager.Instance.SetCurrentChapter(chapterId);
                ChapterManager.Instance.SetCurrentLevelIndex(0);
                InitLevel();
                EventManager.EmitEvent(Constant.EVENT_ON_PLAYER_INFO_CHANGE);
                return;
            }

            validIndex++;
        }
    }

    void OnLevelDropdownChanged(int index)
    {
        if (isRefreshingDropdowns || ChapterManager.Instance == null)
            return;

        List<Level> levels = ChapterManager.Instance.GetCurrentLevels();
        if (levels == null || levels.Count == 0)
            return;

        int levelIndex = Mathf.Clamp(index, 0, levels.Count - 1);
        ChapterManager.Instance.SetCurrentLevelIndex(levelIndex);
        InitLevel();
        EventManager.EmitEvent(Constant.EVENT_ON_PLAYER_INFO_CHANGE);
    }

    [Button]
    void SnapLevel(int level)
    {
        if (scrollTransform == null)
            return;

        float anchorY = -(scrollSpacingBottom + (Mathf.Max(1, level) - 1) * itemHeight);
        scrollTransform.anchoredPosition = new Vector2(0, anchorY);
    }

    public void SelectChapter()
    {
        if (ChapterManager.Instance == null)
            return;

        int startChapterId = ChapterManager.Instance.CurrentChapterId;
        int nextChapterId = startChapterId;

        do
        {
            nextChapterId++;
            if (ChapterManager.Instance.GetChapter(nextChapterId) == null)
                nextChapterId = 1;

            List<Level> levels = ChapterManager.Instance.GetLevels(nextChapterId, ChapterManager.Instance.CurrentMode);
            if (levels != null && levels.Count > 0)
            {
                ChapterManager.Instance.SetCurrentChapter(nextChapterId);
                ChapterManager.Instance.SetCurrentLevelIndex(0);
                InitLevel();
                EventManager.EmitEvent(Constant.EVENT_ON_PLAYER_INFO_CHANGE);
                return;
            }
        }
        while (nextChapterId != startChapterId);
    }
}
