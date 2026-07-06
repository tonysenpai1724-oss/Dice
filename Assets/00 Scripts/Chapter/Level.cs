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

    public List<EnemyEntryConfig> GetWaveEnemyEntries(int waveIndex)
    {
        if (waves != null && waveIndex >= 0 && waveIndex < waves.Count)
            return waves[waveIndex] != null ? waves[waveIndex].enemyEntries : null;

        return null;
    }

    public List<EnemyData> GetWaveEnemyDatas(int waveIndex)
    {
        List<EnemyEntryConfig> entries = GetWaveEnemyEntries(waveIndex);
        if (entries != null && entries.Count > 0)
        {
            List<EnemyData> datas = new();
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && entries[i].Data != null)
                    datas.Add(entries[i].Data);
            }

            if (datas.Count > 0)
                return datas;
        }

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
        List<EnemyEntryConfig> entries = GetWaveEnemyEntries(waveIndex);
        if (entries != null && entries.Count > 0)
        {
            int countEntries = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && entries[i].Data != null)
                    countEntries++;
            }

            if (countEntries > 0)
                return countEntries;
        }

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
    public List<EnemyEntryConfig> enemyEntries = new();
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
    Roll
}

