using System.Collections;
using UnityEngine;
public class LevelManager : Singleton<LevelManager>
{
    public Level currentLevel;

    void Start()
    {
        LoadCurrentLevel();
    }

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

        if (EnemyManager.Instance != null)
            EnemyManager.Instance.SpawnEnemies(level.enemyDatas);
    }
}
