using System;
using System.Collections.Generic;
using TigerForge;
using UnityEngine;

using Sirenix.OdinInspector;

[Serializable]
public class EquipmentInventoryEntry
{
    public string entryId;
    public string equipmentId;
    public int quantity = 1;

    [NonSerialized] public BaseEquiment equipment;

    public int MaxStack => equipment != null ? Mathf.Max(1, equipment.maxStack) : 1;
    public bool CanStack => equipment != null && equipment.canStack;
}

[Serializable]
public class EquipmentInventoryCachedEntry
{
    public string entryId;
    public string equipmentId;
    public int quantity = 1;
}

[Serializable]
public class EquipmentInventoryCachedData : IControllerCachedData
{
    public List<EquipmentInventoryCachedEntry> entries = new();

    public void InitFirsTime()
    {
        if (entries == null)
            entries = new List<EquipmentInventoryCachedEntry>();
    }

    public void OnNewData()
    {
    }
}

public class EquipmentInventoryManager : MonoBehaviour
{
    const string SaveKey = "equipment_inventory";

    public static EquipmentInventoryManager Instance;

    [SerializeField] EquipmentDatabaseSO equipmentDatabase;
    [SerializeField] List<EquipmentInventoryEntry> ownedEquipments = new();

    public IReadOnlyList<EquipmentInventoryEntry> OwnedEquipments => ownedEquipments;
    public EquipmentDatabaseSO EquipmentDatabase
    {
        get
        {
            EnsureDatabase();
            return equipmentDatabase;
        }
    }

    [SerializeField] List<BaseEquiment> baseEquiments;

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureDatabase();
        LoadFromPrefs();
        EnsureEntryIds();
        ResolveAllEquipmentRefs();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            SaveToPrefs();
    }

    void OnApplicationQuit()
    {
        SaveToPrefs();
    }

    public static EquipmentInventoryManager GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        GameObject inventoryObject = new GameObject("EquipmentInventoryManager");
        return inventoryObject.AddComponent<EquipmentInventoryManager>();
    }

    public void SetDatabase(EquipmentDatabaseSO database)
    {
        equipmentDatabase = database;
        EnsureDatabase();
        ResolveAllEquipmentRefs();
        SaveToPrefs();
    }
    [Button]
    void TestAdd()
    {
        foreach (var e in baseEquiments)
        {
            AddEquipment(e, 2);
        }
    }

    public void AddEquipment(BaseEquiment equipment, int quantity = 1)
    {
        if (equipment == null || quantity <= 0)
            return;

        if (equipment.canStack)
        {
            while (quantity > 0)
            {
                EquipmentInventoryEntry stack = FindStackWithSpace(equipment);
                if (stack == null)
                {
                    stack = new EquipmentInventoryEntry
                    {
                        entryId = Guid.NewGuid().ToString("N"),
                        equipmentId = equipment.equipmentId,
                        equipment = equipment,
                        quantity = 0
                    };
                    ownedEquipments.Add(stack);
                }

                int availableSpace = Mathf.Max(0, stack.MaxStack - stack.quantity);
                int addAmount = Mathf.Min(quantity, availableSpace);
                stack.quantity += addAmount;
                quantity -= addAmount;
            }
        }
        else
        {
            for (int i = 0; i < quantity; i++)
            {
                ownedEquipments.Add(new EquipmentInventoryEntry
                {
                    entryId = Guid.NewGuid().ToString("N"),
                    equipmentId = equipment.equipmentId,
                    equipment = equipment,
                    quantity = 1
                });
            }
        }

        NotifyChanged();
    }

    public bool RemoveEquipment(BaseEquiment equipment, int quantity = 1)
    {
        if (equipment == null || quantity <= 0)
            return false;

        int remaining = quantity;

        for (int i = ownedEquipments.Count - 1; i >= 0 && remaining > 0; i--)
        {
            EquipmentInventoryEntry entry = ownedEquipments[i];
            if (entry == null || entry.equipment != equipment)
                continue;

            int removeAmount = Mathf.Min(entry.quantity, remaining);
            entry.quantity -= removeAmount;
            remaining -= removeAmount;

            if (entry.quantity <= 0)
                ownedEquipments.RemoveAt(i);
        }

        bool removed = remaining == 0;
        if (removed)
            NotifyChanged();

        return removed;
    }

    public bool RemoveFromEntry(string entryId, int quantity = 1)
    {
        if (string.IsNullOrEmpty(entryId) || quantity <= 0)
            return false;

        EquipmentInventoryEntry entry = GetEntryById(entryId);
        if (entry == null || entry.quantity < quantity)
            return false;

        entry.quantity -= quantity;
        if (entry.quantity <= 0)
            ownedEquipments.Remove(entry);

        NotifyChanged();
        return true;
    }

    public EquipmentInventoryEntry GetEntryById(string entryId)
    {
        if (string.IsNullOrEmpty(entryId))
            return null;

        for (int i = 0; i < ownedEquipments.Count; i++)
        {
            EquipmentInventoryEntry entry = ownedEquipments[i];
            if (entry != null && entry.entryId == entryId)
                return entry;
        }

        return null;
    }

    public List<EquipmentInventoryEntry> GetAllEntries()
    {
        EnsureEntryIds();
        ResolveAllEquipmentRefs();
        return new List<EquipmentInventoryEntry>(ownedEquipments);
    }

    EquipmentInventoryEntry FindStackWithSpace(BaseEquiment equipment)
    {
        for (int i = 0; i < ownedEquipments.Count; i++)
        {
            EquipmentInventoryEntry entry = ownedEquipments[i];
            if (entry == null || entry.equipment != equipment || !entry.CanStack)
                continue;

            if (entry.quantity < entry.MaxStack)
                return entry;
        }

        return null;
    }

    void EnsureDatabase()
    {
        if (equipmentDatabase == null)
            equipmentDatabase = Resources.Load<EquipmentDatabaseSO>("00 Scripts/SO/Equiment/Equipment Database SO");
    }

    void EnsureEntryIds()
    {
        for (int i = 0; i < ownedEquipments.Count; i++)
        {
            if (ownedEquipments[i] == null)
                continue;

            if (string.IsNullOrEmpty(ownedEquipments[i].entryId))
                ownedEquipments[i].entryId = Guid.NewGuid().ToString("N");
        }
    }

    void ResolveAllEquipmentRefs()
    {
        EnsureDatabase();

        for (int i = 0; i < ownedEquipments.Count; i++)
        {
            EquipmentInventoryEntry entry = ownedEquipments[i];
            if (entry == null)
                continue;

            if (entry.equipment == null && !string.IsNullOrEmpty(entry.equipmentId) && equipmentDatabase != null)
                entry.equipment = equipmentDatabase.FindById(entry.equipmentId);

            if (entry.equipment != null && string.IsNullOrEmpty(entry.equipmentId))
                entry.equipmentId = entry.equipment.equipmentId;
        }
    }

    void SaveToPrefs()
    {
        ResolveAllEquipmentRefs();
        EquipmentInventoryCachedData cachedData = new EquipmentInventoryCachedData
        {
            entries = new List<EquipmentInventoryCachedEntry>()
        };

        for (int i = 0; i < ownedEquipments.Count; i++)
        {
            EquipmentInventoryEntry entry = ownedEquipments[i];
            if (entry == null || entry.quantity <= 0)
                continue;

            cachedData.entries.Add(new EquipmentInventoryCachedEntry
            {
                entryId = entry.entryId,
                equipmentId = entry.equipmentId,
                quantity = entry.quantity
            });
        }
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(cachedData);
        GameManager.Instance.SaveLocalData(SaveKey, json);

        //BaseDataController.SaveLocalCachedData(SaveKey, cachedData);
    }

    void LoadFromPrefs()
    {
        ownedEquipments.Clear();

        EquipmentInventoryCachedData cachedData = BaseDataController.LoadLocalCachedData<EquipmentInventoryCachedData>(SaveKey);
        for (int i = 0; i < cachedData.entries.Count; i++)
        {
            EquipmentInventoryCachedEntry cachedEntry = cachedData.entries[i];
            if (cachedEntry == null)
                continue;

            ownedEquipments.Add(new EquipmentInventoryEntry
            {
                entryId = cachedEntry.entryId,
                equipmentId = cachedEntry.equipmentId,
                quantity = cachedEntry.quantity
            });
        }
    }

    void NotifyChanged()
    {
        SaveToPrefs();
        EventManager.EmitEventData(Constant.ON_EQUIPMENT_INVENTORY_CHANGED, this);
    }
}
