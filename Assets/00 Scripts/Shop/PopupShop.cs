using System.Collections.Generic;
using UnityEngine;

public class PopupShop : UIBase
{
    public ShopDiceItemUI diceItemPrefab;
    public ShopRuneItemUI runeItemPrefab;
    public Transform contentRoot;
    public ShopDatabaseSO shopDatabase;

    [Header("Dice Preview")]
    public InventoryUIController inventoryUIController;
    public InventoryItemPreview itemPrefab;
    public ItemPreviewGenerator previewGenerator;

    readonly List<GameObject> spawnedItems = new List<GameObject>();

    public void Start()
    {
        RefreshShop();
    }
    public override void AfterHideAction()
    {
        GameManager.Instance.CompleteCurrentSpecialLevel(LevelType.Shop);
    }



    public void RefreshShop()
    {
        CachePreviewRefs();
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
            SetupDiceItem(item, entry.diceData, entry.price);
            spawnedItems.Add(item.gameObject);
        }
    }

    void SetupDiceItem(ShopDiceItemUI item, DiceData diceData, int price)
    {
        if (item == null)
            return;

        Sprite existingIcon = GetExistingDiceIcon(diceData);
        if (existingIcon != null)
        {
            item.Setup(diceData, price, existingIcon);
            return;
        }

        Texture2D previewTexture = CaptureDiceIcon(diceData);
        if (previewTexture != null)
        {
            item.Setup(diceData, price, previewTexture);
            return;
        }

        item.Setup(diceData, price);
    }

    Sprite GetExistingDiceIcon(DiceData diceData)
    {
        if (inventoryUIController == null || diceData == null)
            return null;

        ItemToggle itemToggle = inventoryUIController.GetToggle(diceData);
        return itemToggle != null ? itemToggle.PreviewSprite : null;
    }

    Texture2D CaptureDiceIcon(DiceData diceData)
    {
        if (diceData == null)
            return null;

        CachePreviewRefs();

        if (previewGenerator == null || itemPrefab == null)
        {
            Debug.LogWarning($"ShopDiceRunePanel cannot capture dice icon for {diceData.diceName}: missing previewGenerator or itemPrefab.");
            return null;
        }

        Texture2D texture = previewGenerator.Capture(itemPrefab, diceData);
        if (texture == null)
            Debug.LogWarning($"ShopDiceRunePanel failed to capture dice icon for {diceData.diceName}. Check preview camera/render texture.");

        return texture;
    }

    void CachePreviewRefs()
    {
        if (inventoryUIController == null)
            inventoryUIController = FindFirstObjectByType<InventoryUIController>();

        if (inventoryUIController != null)
        {
            if (itemPrefab == null)
                itemPrefab = inventoryUIController.itemPrefab;

            if (previewGenerator == null)
                previewGenerator = inventoryUIController.previewGenerator;
        }

        if (previewGenerator == null)
            previewGenerator = FindFirstObjectByType<ItemPreviewGenerator>();
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
