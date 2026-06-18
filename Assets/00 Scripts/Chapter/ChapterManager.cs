using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ChapterProgressSaveData
{
    public int currentChapterId = 1;
    public int currentMode = 0;
    public int currentLevelIndex = 0;
}

public class ChapterManager : Singleton<ChapterManager>
{
    const string SaveKey = "chapter_progress";

    [SerializeField] private ChapterDatabase chapterDatabase;
    [SerializeField] private int currentChapterId = 1;
    [SerializeField] private ChapterMode currentMode = ChapterMode.Easy;
    [SerializeField] private int currentLevelIndex;

    public ChapterDatabase ChapterDatabase => chapterDatabase;
    public int CurrentChapterId => currentChapterId;
    public ChapterMode CurrentMode => currentMode;
    public int CurrentLevelIndex => currentLevelIndex;
    public List<ChapterDatabase> chapterDatabases;

    public void Start()
    {
        LoadProgress();
        ValidateCurrentProgress();
        TigerForge.EventManager.StartListening(Constant.ON_END_GAME, AdvanceAfterWin);
    }

    public void SetCurrentChapter(int chapterId)
    {
        currentChapterId = chapterId;
        ValidateCurrentProgress();
        SaveProgress();
    }

    public void SetCurrentMode(ChapterMode mode)
    {
        currentMode = mode;
        ValidateCurrentProgress();
        SaveProgress();
    }

    public void SetCurrentLevelIndex(int levelIndex)
    {
        currentLevelIndex = Mathf.Max(0, levelIndex);
        ValidateCurrentProgress();
        SaveProgress();
    }

    public void AdvanceAfterWin()
    {
        List<Level> levels = GetCurrentLevels();
        if (levels == null || levels.Count == 0)
            return;

        if (currentLevelIndex + 1 < levels.Count)
        {
            currentLevelIndex++;
            SaveProgress();
            return;
        }

        if (TryMoveToNextChapter())
        {
            SaveProgress();
            return;
        }

        currentLevelIndex = Mathf.Max(0, levels.Count - 1);
        SaveProgress();
    }

    public bool IsAtFinalPlayableLevel()
    {
        List<Level> levels = GetCurrentLevels();
        if (levels == null || levels.Count == 0)
            return true;

        return currentLevelIndex >= levels.Count - 1 && !HasNextChapter();
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
        ChapterDatabase database = ResolveDatabaseForChapter(chapterId);
        return database == null ? null : database.GetChapter(chapterId);
    }

    public List<Level> GetLevels(int chapterId, ChapterMode mode)
    {
        ChapterDatabase database = ResolveDatabaseForChapter(chapterId);
        return database == null ? null : database.GetLevels(chapterId, mode);
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

    bool TryMoveToNextChapter()
    {
        int nextChapterId = currentChapterId + 1;
        ChapterData nextChapter = GetChapter(nextChapterId);
        if (nextChapter == null)
            return false;

        currentChapterId = nextChapterId;
        currentLevelIndex = 0;
        ValidateCurrentProgress();
        return true;
    }

    bool HasNextChapter()
    {
        return GetChapter(currentChapterId + 1) != null;
    }

    ChapterDatabase ResolveDatabaseForChapter(int chapterId)
    {
        if (chapterDatabases != null)
        {
            for (int i = 0; i < chapterDatabases.Count; i++)
            {
                ChapterDatabase database = chapterDatabases[i];
                if (database == null)
                    continue;

                if (database.GetChapter(chapterId) != null)
                    return database;
            }
        }

        return chapterDatabase;
    }

    void ValidateCurrentProgress()
    {
        ChapterData chapter = GetCurrentChapter();
        if (chapter == null)
        {
            currentChapterId = 1;
            chapter = GetCurrentChapter();
        }

        List<Level> levels = GetCurrentLevels();
        if (levels == null || levels.Count == 0)
        {
            currentLevelIndex = 0;
            return;
        }

        currentLevelIndex = Mathf.Clamp(currentLevelIndex, 0, levels.Count - 1);
    }

    void SaveProgress()
    {
        ChapterProgressSaveData saveData = new ChapterProgressSaveData
        {
            currentChapterId = currentChapterId,
            currentMode = (int)currentMode,
            currentLevelIndex = currentLevelIndex
        };

        CPlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(saveData));
    }

    void LoadProgress()
    {
        string json = CPlayerPrefs.GetString(SaveKey);
        if (string.IsNullOrEmpty(json))
            return;

        ChapterProgressSaveData saveData = JsonUtility.FromJson<ChapterProgressSaveData>(json);
        if (saveData == null)
            return;

        currentChapterId = Mathf.Max(1, saveData.currentChapterId);
        currentMode = (ChapterMode)Mathf.Clamp(saveData.currentMode, 0, Enum.GetValues(typeof(ChapterMode)).Length - 1);
        currentLevelIndex = Mathf.Max(0, saveData.currentLevelIndex);
    }
}