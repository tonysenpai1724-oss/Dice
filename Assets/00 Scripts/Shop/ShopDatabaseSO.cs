using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "RuneDice/Shop/Shop Database")]
public class ShopDatabaseSO : SerializedScriptableObject
{
    public DiceDatabaseSO diceDatabase;
    [Min(0)] public int maxDiceItemsToSpawn = 0;
    [Min(0)] public int maxRuneItemsToSpawn = 0;
    public List<ShopDiceEntry> diceItems = new();
    public List<ShopDiceLevelRangeEntry> diceLevelRangeItems = new();
    public List<ShopRuneEntry> runeItems = new();

    public List<ShopDiceResolvedEntry> GetAvailableDiceItems()
    {
        List<ShopDiceResolvedEntry> result = new List<ShopDiceResolvedEntry>();

        for (int i = 0; i < diceItems.Count; i++)
        {
            ShopDiceEntry entry = diceItems[i];
            if (entry == null || entry.diceData == null)
                continue;

            result.Add(new ShopDiceResolvedEntry(entry.diceData, entry.price));
            if (ReachedLimit(result.Count, maxDiceItemsToSpawn))
                return result;
        }

        if (diceDatabase == null)
            return result;

        for (int i = 0; i < diceLevelRangeItems.Count; i++)
        {
            ShopDiceLevelRangeEntry entry = diceLevelRangeItems[i];
            if (entry == null)
                continue;

            List<DiceData> pool = diceDatabase.GetAllByLevelRange(entry.minLevel, entry.maxLevel);
            int spawnedFromRange = 0;

            for (int poolIndex = 0; poolIndex < pool.Count; poolIndex++)
            {
                DiceData data = pool[poolIndex];
                if (data == null)
                    continue;

                if (entry.allowedTypes != null && entry.allowedTypes.Count > 0 && !entry.allowedTypes.Contains(data.type))
                    continue;

                result.Add(new ShopDiceResolvedEntry(data, entry.price));
                spawnedFromRange++;

                if (ReachedLimit(spawnedFromRange, entry.spawnCount) || ReachedLimit(result.Count, maxDiceItemsToSpawn))
                    break;
            }

            if (ReachedLimit(result.Count, maxDiceItemsToSpawn))
                break;
        }

        return result;
    }

    public List<ShopRuneEntry> GetAvailableRuneItems()
    {
        List<ShopRuneEntry> result = new List<ShopRuneEntry>();
        for (int i = 0; i < runeItems.Count; i++)
        {
            ShopRuneEntry entry = runeItems[i];
            if (entry == null || entry.runeData == null)
                continue;

            result.Add(entry);
            if (ReachedLimit(result.Count, maxRuneItemsToSpawn))
                break;
        }

        return result;
    }

    bool ReachedLimit(int count, int limit)
    {
        return limit > 0 && count >= limit;
    }
}

[System.Serializable]
public class ShopDiceResolvedEntry
{
    public DiceData diceData;
    public int price;

    public ShopDiceResolvedEntry(DiceData data, int itemPrice)
    {
        diceData = data;
        price = itemPrice;
    }
}

[System.Serializable]
public class ShopDiceEntry
{
    public DiceData diceData;
    [Min(0)] public int price = 10;
}

[System.Serializable]
public class ShopDiceLevelRangeEntry
{
    [Min(1)] public int minLevel = 1;
    [Min(1)] public int maxLevel = 1;
    [Min(0)] public int price = 10;
    [Min(0)] public int spawnCount = 0;
    public List<DiceType> allowedTypes = new();
}

[System.Serializable]
public class ShopRuneEntry
{
    public RuneSkillData runeData;
    [Min(0)] public int price = 15;
}
