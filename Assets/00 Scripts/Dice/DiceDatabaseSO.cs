using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "RuneDice/Dice Database")]
public class DiceDatabaseSO : ScriptableObject
{
    public List<DiceData> diceDatas = new();

    public DiceData GetDiceData(int level, DiceType type)
    {
        for (int i = 0; i < diceDatas.Count; i++)
        {
            DiceData data = diceDatas[i];
            if (data == null)
                continue;

            if (data.level == level && data.type == type)
                return data;
        }

        return null;
    }

    public DiceData GetDiceDataByLevel(int level)
    {
        for (int i = 0; i < diceDatas.Count; i++)
        {
            DiceData data = diceDatas[i];
            if (data == null)
                continue;

            if (data.level == level)
                return data;
        }

        return null;
    }

    public List<DiceData> GetAllByLevel(int level)
    {
        List<DiceData> result = new List<DiceData>();

        for (int i = 0; i < diceDatas.Count; i++)
        {
            DiceData data = diceDatas[i];
            if (data == null)
                continue;

            if (data.level == level)
                result.Add(data);
        }

        return result;
    }

    public List<DiceData> GetAllByType(DiceType type)
    {
        List<DiceData> result = new List<DiceData>();

        for (int i = 0; i < diceDatas.Count; i++)
        {
            DiceData data = diceDatas[i];
            if (data == null)
                continue;

            if (data.type == type)
                result.Add(data);
        }

        return result;
    }
}
