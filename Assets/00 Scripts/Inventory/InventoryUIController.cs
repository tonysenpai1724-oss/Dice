using System;
using System.Collections.Generic;
using UnityEngine;

public enum InventoryDiceSource
{
    ChapterSession,
    PlayerController,
    ManualList
}

public class InventoryUIController : MonoBehaviour
{
    public InventoryItem itemPrefab;
    public Transform itemParent;
    public ItemToggle itemTogglePrefab;
    public ItemPreviewGenerator previewGenerator;
    public InventoryDiceSource diceSource = InventoryDiceSource.ChapterSession;
    public List<DiceData> diceItems = new();
    public List<ItemToggle> itemToggles;

    public event Action<ItemToggle> ItemSelected;

    [Header("Layout")]
    public Vector3 startPosition = new Vector3(-10f, -3.15f, 0f);
    public Vector2 itemOffset = new Vector2(4.3f, 4.3f);
    [Min(1)] public int itemsPerRow = 5;

    readonly List<InventoryItem> spawnedItems = new();
    readonly List<ItemToggle> spawnedToggles = new();
    void Start()
    {
        // itemToggles.AddRange(GetComponentsInChildren<ItemToggle>());
        RefreshItems();
        TigerForge.EventManager.StartListening(Constant.ON_EQUIMENT_DICE_CHANGED, RefreshItems);
    }

    public void SetDiceItems(List<DiceData> newDiceItems)
    {
        diceSource = InventoryDiceSource.ManualList;
        diceItems = newDiceItems != null
            ? new List<DiceData>(newDiceItems)
            : new List<DiceData>();

        RefreshItems();
    }
    void OnEnable()
    {
        RefreshItems();
    }

    public void RefreshItems()
    {
        ClearItems();
        LoadDiceItemsFromSource();

        if (itemPrefab == null || itemTogglePrefab == null)
            return;

        if (previewGenerator == null)
            previewGenerator = FindFirstObjectByType<ItemPreviewGenerator>();

        if (previewGenerator == null)
        {
            Debug.LogWarning("InventoryUIController needs ItemPreviewGenerator to render dice previews.");
            return;
        }

        Transform parent = itemParent != null ? itemParent : transform;
        itemToggles.Clear();

        for (int i = 0; i < diceItems.Count; i++)
        {

            DiceData diceData = diceItems[i];
            if (diceData == null)
                continue;

            Texture2D previewTexture = previewGenerator.Capture(itemPrefab, diceData);
            ItemToggle itemToggle = Instantiate(itemTogglePrefab, parent);
            itemToggle.Setup(diceData, previewTexture, OnItemToggleSelected);
            itemToggles.Add(itemToggle);
            spawnedToggles.Add(itemToggle);
        }
    }

    public ItemToggle GetToggle(DiceData diceData)
    {
        if (diceData == null)
            return null;

        for (int i = 0; i < itemToggles.Count; i++)
        {
            ItemToggle itemToggle = itemToggles[i];
            if (itemToggle != null && itemToggle.data == diceData)
                return itemToggle;
        }

        return null;
    }

    void OnItemToggleSelected(ItemToggle itemToggle)
    {
        ItemSelected?.Invoke(itemToggle);
    }

    void CacheItemToggles()
    {
        if (itemToggles == null)
            itemToggles = new List<ItemToggle>();

        if (itemToggles.Count == 0)
            itemToggles.AddRange(GetComponentsInChildren<ItemToggle>(true));
    }

    void LoadDiceItemsFromSource()
    {
        if (diceSource == InventoryDiceSource.ManualList)
            return;

        if (diceSource == InventoryDiceSource.PlayerController)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            diceItems = player != null && player.diceDatas != null
                ? new List<DiceData>(player.diceDatas)
                : new List<DiceData>();
            return;
        }

        ChapterDiceSession session = ChapterDiceSession.GetOrCreate();
        diceItems = session != null
            ? session.GetRuntimeDiceDatasCopy()
            : new List<DiceData>();
    }

    Vector3 GetItemPosition(int index)
    {
        int column = index % itemsPerRow;
        int row = index / itemsPerRow;

        return startPosition + new Vector3(
            column * itemOffset.x,
            -row * itemOffset.y,
            0f
        );
    }

    void ClearItems()
    {
        for (int i = spawnedToggles.Count - 1; i >= 0; i--)
        {
            if (spawnedToggles[i] != null)
                Destroy(spawnedToggles[i].gameObject);
        }

        spawnedToggles.Clear();
        itemToggles.Clear();

        for (int i = spawnedItems.Count - 1; i >= 0; i--)
        {
            InventoryItem item = spawnedItems[i];
            if (item == null)
                continue;

            Destroy(item.gameObject);
        }

        spawnedItems.Clear();
    }
}
