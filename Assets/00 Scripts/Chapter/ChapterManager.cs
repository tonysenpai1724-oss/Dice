using System.Collections.Generic;
using UnityEngine;

public class ChapterManager : Singleton<ChapterManager>
{
    [SerializeField] private ChapterDatabase chapterDatabase;
    [SerializeField] private int currentChapterId = 1;
    [SerializeField] private ChapterMode currentMode = ChapterMode.Easy;
    [SerializeField] private int currentLevelIndex;

    public ChapterDatabase ChapterDatabase => chapterDatabase;
    public int CurrentChapterId => currentChapterId;
    public ChapterMode CurrentMode => currentMode;
    public int CurrentLevelIndex => currentLevelIndex;
    public void Start()
    {
        currentLevelIndex = Mathf.Max(0, IPlayerInfoController.Instance.CurrentLevel() - 1);

    }

    public void SetCurrentChapter(int chapterId)
    {
        currentChapterId = chapterId;
    }

    public void SetCurrentMode(ChapterMode mode)
    {
        currentMode = mode;
    }

    public void SetCurrentLevelIndex(int levelIndex)
    {
        currentLevelIndex = Mathf.Max(0, levelIndex);
    }

    public ChapterData GetCurrentChapter()
    {
        return GetChapter(currentChapterId);
    }

    public List<Level> GetCurrentLevels()
    {
        return GetLevels(currentChapterId, currentMode);
    }

    public Level GetCurrentLevel()
    {
        return GetLevel(currentChapterId, currentMode, currentLevelIndex);
    }

    public ChapterData GetChapter(int chapterId)
    {
        return chapterDatabase == null ? null : chapterDatabase.GetChapter(chapterId);
    }

    public List<Level> GetLevels(int chapterId, ChapterMode mode)
    {
        return chapterDatabase == null ? null : chapterDatabase.GetLevels(chapterId, mode);
    }

    public Level GetLevel(int chapterId, ChapterMode mode, int levelIndex)
    {
        List<Level> levels = GetLevels(chapterId, mode);
        if (levels == null || levels.Count == 0)
            return null;

        if (levelIndex < 0 || levelIndex >= levels.Count)
            return null;

        return levels[levelIndex];
    }
}
