using System.Collections.Generic;
using UnityEngine;

public class ShopDiceRunePanel : MonoBehaviour
{
    public ShopDiceItemUI diceItemPrefab;
    public ShopRuneItemUI runeItemPrefab;
    public Transform contentRoot;
    public ShopDatabaseSO shopDatabase;


    readonly List<GameObject> spawnedItems = new List<GameObject>();

    public void Start()
    {
        RefreshShop();
    }

    public void RefreshShop()
    {
        ClearSpawnedItems();
        SpawnDiceItems();
        SpawnRuneItems();
    }

    void SpawnDiceItems()
    {
        if (diceItemPrefab == null || contentRoot == null || shopDatabase == null)
            return;

        List<ShopDiceResolvedEntry> source = shopDatabase.GetAvailableDiceItems();
        for (int i = 0; i < source.Count; i++)
        {
            ShopDiceResolvedEntry entry = source[i];
            if (entry == null || entry.diceData == null)
                continue;

            ShopDiceItemUI item = Instantiate(diceItemPrefab, contentRoot);
            item.Setup(entry.diceData, entry.price);
            spawnedItems.Add(item.gameObject);
        }
    }

    void SpawnRuneItems()
    {
        if (runeItemPrefab == null || contentRoot == null || shopDatabase == null)
            return;

        List<ShopRuneEntry> source = shopDatabase.GetAvailableRuneItems();
        for (int i = 0; i < source.Count; i++)
        {
            ShopRuneEntry entry = source[i];
            if (entry == null || entry.runeData == null)
                continue;

            ShopRuneItemUI item = Instantiate(runeItemPrefab, contentRoot);
            item.Setup(entry.runeData, entry.price);
            spawnedItems.Add(item.gameObject);
        }
    }

    void ClearSpawnedItems()
    {
        for (int i = spawnedItems.Count - 1; i >= 0; i--)
        {
            if (spawnedItems[i] != null)
                Destroy(spawnedItems[i]);
        }

        spawnedItems.Clear();
    }
}
