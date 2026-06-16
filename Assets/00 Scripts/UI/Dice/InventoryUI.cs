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


    [Button]
    public void RefreshInventory()
    {
        ClearItems();

        if (contentRoot == null || itemPrefab == null)
            return;

        List<BaseEquiment> equipments = EquipmentInventoryManager.GetOrCreate().GetAllEquipments();

        for (int i = 0; i < equipments.Count; i++)
        {
            BaseEquiment equipment = equipments[i];
            if (equipment == null)
                continue;

            InventoryItemUI itemUI = Instantiate(itemPrefab, contentRoot);
            itemUI.Setup(equipment, OnSelectEquipment);
            spawnedItems.Add(itemUI);
        }
    }

    void OnSelectEquipment(BaseEquiment equipment)
    {
        if (detailPanel == null)
            return;

        detailPanel.Show(equipment);
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
