using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "RuneDice/Chapter Database")]
public class ChapterDatabase : SerializedScriptableObject
{
    public List<ChapterData> chapters = new();

    public ChapterData GetChapter(int chapterId)
    {
        return chapters.Find(item => item.chapterId == chapterId);
    }

    public List<Level> GetLevels(int chapterId, ChapterMode mode)
    {
        ChapterData chapter = GetChapter(chapterId);
        return chapter == null ? null : chapter.GetLevels(mode);
    }
}

[System.Serializable]
public class ChapterData
{
    public int chapterId;
    public string chapterName;
    public ChapterModeLevels easy = new();
    public ChapterModeLevels hard = new();
    public ChapterModeLevels devil = new();

    public List<Level> GetLevels(ChapterMode mode)
    {
        return mode switch
        {
            ChapterMode.Easy => easy.levels,
            ChapterMode.Hard => hard.levels,
            ChapterMode.Devil => devil.levels,
            _ => easy.levels
        };
    }
}

[System.Serializable]
public class ChapterModeLevels
{
    public List<Level> levels = new();
}

public enum ChapterMode
{
    Easy,
    Hard,
    Devil
}
