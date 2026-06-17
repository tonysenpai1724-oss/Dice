using System.Collections.Generic;
using Sirenix.OdinInspector;
using TigerForge;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("Refs")]
    public Transform contentRoot;
    public InventoryItemUI itemPrefab;
    public EquipmentDetailPanel detailPanel;

    readonly List<InventoryItemUI> spawnedItems = new();

    void Start()
    {
        EventManager.StartListening(Constant.ON_EQUIPMENT_INVENTORY_CHANGED, RefreshInventory);
        EventManager.StartListening(Constant.ON_EQUIPMENT_SESSION_CHANGED, RefreshInventory);
        RefreshInventory();
    }

    void OnEnable()
    {
        RefreshInventory();
    }

    [Button]
    public void RefreshInventory()
    {
        ClearItems();

        if (contentRoot == null || itemPrefab == null)
            return;

        List<EquipmentInventoryEntry> entries = EquipmentInventoryManager.GetOrCreate().GetAllEntries();

        for (int i = 0; i < entries.Count; i++)
        {
            EquipmentInventoryEntry entry = entries[i];
            if (entry == null || entry.equipment == null || entry.quantity <= 0)
                continue;

            InventoryItemUI itemUI = Instantiate(itemPrefab, contentRoot);
            itemUI.Setup(entry, OnSelectEquipment);
            spawnedItems.Add(itemUI);
        }
    }

    void OnSelectEquipment(EquipmentInventoryEntry entry)
    {
        if (detailPanel == null || entry == null)
            return;

        detailPanel.Show(entry);
    }

    void ClearItems()
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            if (spawnedItems[i] != null)
                Destroy(spawnedItems[i].gameObject);
        }

        spawnedItems.Clear();
    }
}