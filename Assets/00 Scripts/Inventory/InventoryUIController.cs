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
    public InventoryDiceSource diceSource = InventoryDiceSource.ChapterSession;
    public List<DiceData> diceItems = new();
    public List<ItemToggle> itemToggles;

    [Header("Layout")]
    public Vector3 startPosition = new Vector3(-10f, -3.15f, 0f);
    public Vector2 itemOffset = new Vector2(4.3f, 4.3f);
    [Min(1)] public int itemsPerRow = 5;

    readonly List<InventoryItem> spawnedItems = new();
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

        if (itemPrefab == null)
            return;

        Transform parent = itemParent != null ? itemParent : transform;

        for (int i = 0; i < diceItems.Count; i++)
        {
            DiceData diceData = diceItems[i];
            if (diceData == null)
                continue;
            if (itemToggles.Count >= i)
                itemToggles[i].data = diceData;

            InventoryItem item = Instantiate(itemPrefab);
            item.transform.position = GetItemPosition(i);
            item.transform.localScale = new Vector3(1, 1, 1);
            item.transform.localRotation = Quaternion.Euler(new Vector3(-30, 48, -30));
            item.Setup(diceData);
            spawnedItems.Add(item);
            item.transform.SetParent(parent);
        }
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
