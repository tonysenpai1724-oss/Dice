using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "RuneDice/Level")]
public class Level : SerializedScriptableObject
{
    public int levelNumber;
    public List<LevelWaveData> waves = new();
    public LevelType leveltype;

    public int WaveCount
    {
        get
        {
            return waves != null ? waves.Count : 0;
        }
    }

    public bool HasAnyWaveData()
    {
        return WaveCount > 0;
    }

    public List<EnemyData> GetWaveEnemyDatas(int waveIndex)
    {
        if (waves != null && waveIndex >= 0 && waveIndex < waves.Count)
            return waves[waveIndex] != null ? waves[waveIndex].enemyDatas : null;

        return null;
    }

    public List<EnemySpawnPlacement> GetWaveSpawnPlacements(int waveIndex)
    {
        if (waves != null && waveIndex >= 0 && waveIndex < waves.Count)
            return waves[waveIndex] != null ? waves[waveIndex].enemySpawnPlacements : null;

        return null;
    }

    public int GetWaveEnemyCount(int waveIndex)
    {
        List<EnemyData> datas = GetWaveEnemyDatas(waveIndex);
        if (datas == null)
            return 0;

        int count = 0;
        for (int i = 0; i < datas.Count; i++)
        {
            if (datas[i] != null)
                count++;
        }

        return count;
    }
}

[System.Serializable]
public class LevelWaveData
{
    public string waveName = "Wave";
    public List<EnemyData> enemyDatas = new();
    public List<EnemySpawnPlacement> enemySpawnPlacements = new();
}

public enum LevelType
{
    NormalBattle,
    ToughBattle,
    MiniBoss,
    Shop,
    MagicAltar,
    Chest,
    Jester,
    Upgrade,
    FinalBoss,
    ChestReward,
}
