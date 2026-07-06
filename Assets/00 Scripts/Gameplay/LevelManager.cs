using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

public class LevelManager : Singleton<LevelManager>
{
    public Level currentLevel;

    void Start()
    {
        currentLevel = ChapterManager.Instance.GetCurrentLevel();
        LoadCurrentLevel();
    }

    [Button]
    public void LoadCurrentLevel()
    {
        if (currentLevel == null)
            return;

        LoadLevel(currentLevel);
    }

    public void LoadLevel(Level level)
    {
        if (level == null)
            return;

        currentLevel = level;

        if (level.leveltype == LevelType.MagicAltar)
        {
            UIManager.Instance?.ShowPopupClonePanel();
            return;
        }

        if (level.leveltype == LevelType.Roll)
        {
            UIManager.Instance?.ShowPopupRoll();
            return;
        }

        if (EnemyManager.Instance != null)
            EnemyManager.Instance.StartLevel(level);
    }
}
