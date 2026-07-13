using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EquipmentSessionEntry
{
    public EquipmentType equipmentType;
    public string equipmentId;
    public string inventoryEntryId;
    public BaseEquiment equipment;
}

[Serializable]
public class EquipmentSessionCachedData : IControllerCachedData
{
    public List<EquipmentSessionSaveEntry> equippedItems = new();

    public void InitFirsTime()
    {
        if (equippedItems == null)
            equippedItems = new List<EquipmentSessionSaveEntry>();

        Array equipmentTypes = Enum.GetValues(typeof(EquipmentType));
        for (int i = 0; i < equipmentTypes.Length; i++)
        {
            EquipmentType equipmentType = (EquipmentType)equipmentTypes.GetValue(i);
            if (GetEntry(equipmentType) != null)
                continue;

            equippedItems.Add(new EquipmentSessionSaveEntry
            {
                equipmentType = equipmentType,
                equipmentId = string.Empty,
                inventoryEntryId = string.Empty
            });
        }
    }

    public void OnNewData()
    {
    }

    public EquipmentSessionSaveEntry GetEntry(EquipmentType equipmentType)
    {
        for (int i = 0; i < equippedItems.Count; i++)
        {
            if (equippedItems[i].equipmentType == equipmentType)
                return equippedItems[i];
        }

        return null;
    }
}

[Serializable]
public class EquipmentSessionSaveEntry
{
    public EquipmentType equipmentType;
    public string equipmentId;
    public string inventoryEntryId;
}

public interface IEquipmentSessionController : IController<EquipmentSession>
{
}

public class EquipmentSession : BaseLocalController<EquipmentSessionCachedData>, IEquipmentSessionController
{
    const string SaveKey = "equipment_session";

    EquipmentDatabaseSO equipmentDatabase;
    readonly List<EquipmentSessionEntry> runtimeEntries = new();

    public static EquipmentSession Instance => IEquipmentSessionController.Instance;
    public IReadOnlyList<EquipmentSessionEntry> EquippedItems => GetRuntimeEntries();

    public static EquipmentSession GetOrCreate()
    {
        return Instance;
    }

    public override string KeyData()
    {
        return SaveKey;
    }

    public override string KeyEvent()
    {
        return Constant.ON_EQUIPMENT_SESSION_CHANGED;
    }

    public void Equip(BaseEquiment equipment)
    {
        if (equipment == null)
            return;

        EquipmentInventoryManager inventory = EquipmentInventoryManager.GetOrCreate();
        List<EquipmentInventoryEntry> entries = inventory.GetAllEntries();
        for (int i = 0; i < entries.Count; i++)
        {
            EquipmentInventoryEntry entry = entries[i];
            if (entry == null || entry.equipment != equipment || entry.quantity <= 0)
                continue;

            EquipEntry(entry);
            return;
        }
    }

    public void EquipEntry(string inventoryEntryId)
    {
        EquipmentInventoryEntry inventoryEntry = EquipmentInventoryManager.GetOrCreate().GetEntryById(inventoryEntryId);
        EquipEntry(inventoryEntry);
    }

    public void EquipEntry(EquipmentInventoryEntry inventoryEntry)
    {
        if (inventoryEntry == null || inventoryEntry.equipment == null || inventoryEntry.quantity <= 0)
            return;

        EnsureCachedData();

        EquipmentSessionSaveEntry entry = GetCachedEntry(inventoryEntry.equipment.equipmentType);
        if (entry == null)
            return;

        entry.inventoryEntryId = inventoryEntry.entryId;
        entry.equipmentId = inventoryEntry.equipment.equipmentId;
        RefreshRuntimeEntries();
        OnValueChange();
    }

    public void Unequip(EquipmentType equipmentType)
    {
        EnsureCachedData();

        EquipmentSessionSaveEntry entry = GetCachedEntry(equipmentType);
        if (entry == null)
            return;

        entry.inventoryEntryId = string.Empty;
        entry.equipmentId = string.Empty;
        RefreshRuntimeEntries();
        OnValueChange();
    }

    public void SetDatabase(EquipmentDatabaseSO database)
    {
        equipmentDatabase = database;
        RefreshRuntimeEntries();
    }

    public BaseEquiment GetEquipped(EquipmentType equipmentType)
    {
        EquipmentSessionEntry entry = GetRuntimeEntry(equipmentType);
        return entry != null ? entry.equipment : null;
    }

    public bool IsEntryEquipped(string inventoryEntryId)
    {
        if (string.IsNullOrEmpty(inventoryEntryId))
            return false;

        EnsureCachedData();
        for (int i = 0; i < cachedData.equippedItems.Count; i++)
        {
            if (cachedData.equippedItems[i].inventoryEntryId == inventoryEntryId)
                return true;
        }

        return false;
    }

    public int GetEquippedCount(BaseEquiment equipment)
    {
        if (equipment == null)
            return 0;

        List<EquipmentSessionEntry> entries = GetRuntimeEntries();
        int count = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].equipment == equipment)
                count++;
        }

        return count;
    }

    public List<BaseEquiment> GetAllEquipped()
    {
        List<BaseEquiment> result = new List<BaseEquiment>();
        List<EquipmentSessionEntry> entries = GetRuntimeEntries();

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].equipment != null)
                result.Add(entries[i].equipment);
        }

        return result;
    }

    EquipmentSessionSaveEntry GetCachedEntry(EquipmentType equipmentType)
    {
        EnsureCachedData();
        return cachedData.GetEntry(equipmentType);
    }

    EquipmentSessionEntry GetRuntimeEntry(EquipmentType equipmentType)
    {
        List<EquipmentSessionEntry> entries = GetRuntimeEntries();
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].equipmentType == equipmentType)
                return entries[i];
        }

        return null;
    }

    List<EquipmentSessionEntry> GetRuntimeEntries()
    {
        RefreshRuntimeEntries();
        return runtimeEntries;
    }

    void RefreshRuntimeEntries()
    {
        EnsureCachedData();
        runtimeEntries.Clear();

        for (int i = 0; i < cachedData.equippedItems.Count; i++)
        {
            EquipmentSessionSaveEntry cachedEntry = cachedData.equippedItems[i];
            if (cachedEntry == null)
                continue;

            runtimeEntries.Add(new EquipmentSessionEntry
            {
                equipmentType = cachedEntry.equipmentType,
                equipmentId = cachedEntry.equipmentId,
                inventoryEntryId = cachedEntry.inventoryEntryId,
                equipment = ResolveEquipment(cachedEntry.equipmentId, cachedEntry.inventoryEntryId)
            });
        }
    }

    void EnsureCachedData()
    {
        if (cachedData == null)
            cachedData = LoadLocalCachedData<EquipmentSessionCachedData>(KeyData());

        cachedData.InitFirsTime();
    }

    BaseEquiment ResolveEquipment(string equipmentId, string inventoryEntryId)
    {
        EquipmentInventoryEntry inventoryEntry = EquipmentInventoryManager.Instance != null ? EquipmentInventoryManager.Instance.GetEntryById(inventoryEntryId) : null;
        if (inventoryEntry != null && inventoryEntry.equipment != null)
            return inventoryEntry.equipment;

        if (string.IsNullOrEmpty(equipmentId))
            return null;

        if (equipmentDatabase == null)
        {
            if (EquipmentInventoryManager.Instance != null)
                equipmentDatabase = EquipmentInventoryManager.Instance.EquipmentDatabase;

            if (equipmentDatabase == null)
                equipmentDatabase = Resources.Load<EquipmentDatabaseSO>("00 Scripts/SO/Equiment/Equipment Database SO");
        }

        return equipmentDatabase != null ? equipmentDatabase.FindById(equipmentId) : null;
    }
}
