using System;
using System.Collections.Generic;
using TigerForge;
using UnityEngine;

[Serializable]
public class EquipmentSessionEntry
{
    public EquipmentType equipmentType;
    public string inventoryEntryId;
    public BaseEquiment equipment;
}

[Serializable]
public class EquipmentSessionSaveData
{
    public List<EquipmentSessionSaveEntry> equippedItems = new();
}

[Serializable]
public class EquipmentSessionSaveEntry
{
    public EquipmentType equipmentType;
    public string equipmentId;
    public string inventoryEntryId;
}

public class EquipmentSession : MonoBehaviour
{
    const string SaveKey = "equipment_session";

    public static EquipmentSession Instance;

    [SerializeField] EquipmentDatabaseSO equipmentDatabase;
    [SerializeField] List<EquipmentSessionEntry> equippedItems = new();

    public IReadOnlyList<EquipmentSessionEntry> EquippedItems => equippedItems;

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeSlots();
        LoadFromPrefs();
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

    public static EquipmentSession GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        GameObject sessionObject = new GameObject("EquipmentSession");
        return sessionObject.AddComponent<EquipmentSession>();
    }

    public void InitializeSlots()
    {
        Array equipmentTypes = Enum.GetValues(typeof(EquipmentType));

        for (int i = 0; i < equipmentTypes.Length; i++)
        {
            EquipmentType equipmentType = (EquipmentType)equipmentTypes.GetValue(i);
            if (GetEntry(equipmentType) != null)
                continue;

            equippedItems.Add(new EquipmentSessionEntry
            {
                equipmentType = equipmentType,
                inventoryEntryId = string.Empty,
                equipment = null
            });
        }
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

            EquipEntry(entry.entryId);
            return;
        }
    }

    public void EquipEntry(string inventoryEntryId)
    {
        EquipmentInventoryEntry inventoryEntry = EquipmentInventoryManager.GetOrCreate().GetEntryById(inventoryEntryId);
        if (inventoryEntry == null || inventoryEntry.equipment == null || inventoryEntry.quantity <= 0)
            return;

        InitializeSlots();

        EquipmentSessionEntry entry = GetEntry(inventoryEntry.equipment.equipmentType);
        if (entry == null)
            return;

        entry.inventoryEntryId = inventoryEntry.entryId;
        entry.equipment = inventoryEntry.equipment;
        NotifyChanged();
    }

    public void Unequip(EquipmentType equipmentType)
    {
        EquipmentSessionEntry entry = GetEntry(equipmentType);
        if (entry == null)
            return;

        entry.inventoryEntryId = string.Empty;
        entry.equipment = null;
        NotifyChanged();
    }

    public void SetDatabase(EquipmentDatabaseSO database)
    {
        equipmentDatabase = database;
        LoadFromPrefs();
    }

    public BaseEquiment GetEquipped(EquipmentType equipmentType)
    {
        EquipmentSessionEntry entry = GetEntry(equipmentType);
        return entry != null ? entry.equipment : null;
    }

    public bool IsEntryEquipped(string inventoryEntryId)
    {
        if (string.IsNullOrEmpty(inventoryEntryId))
            return false;

        for (int i = 0; i < equippedItems.Count; i++)
        {
            if (equippedItems[i].inventoryEntryId == inventoryEntryId)
                return true;
        }

        return false;
    }

    public int GetEquippedCount(BaseEquiment equipment)
    {
        if (equipment == null)
            return 0;

        int count = 0;

        for (int i = 0; i < equippedItems.Count; i++)
        {
            if (equippedItems[i].equipment == equipment)
                count++;
        }

        return count;
    }

    public List<BaseEquiment> GetAllEquipped()
    {
        List<BaseEquiment> result = new List<BaseEquiment>();

        for (int i = 0; i < equippedItems.Count; i++)
        {
            if (equippedItems[i].equipment != null)
                result.Add(equippedItems[i].equipment);
        }

        return result;
    }

    EquipmentSessionEntry GetEntry(EquipmentType equipmentType)
    {
        for (int i = 0; i < equippedItems.Count; i++)
        {
            if (equippedItems[i].equipmentType == equipmentType)
                return equippedItems[i];
        }

        return null;
    }

    void NotifyChanged()
    {
        SaveToPrefs();
        EventManager.EmitEventData(Constant.ON_EQUIPMENT_SESSION_CHANGED, this);
    }

    void SaveToPrefs()
    {
        EquipmentSessionSaveData saveData = new EquipmentSessionSaveData();

        for (int i = 0; i < equippedItems.Count; i++)
        {
            EquipmentSessionEntry entry = equippedItems[i];
            if (entry == null)
                continue;

            saveData.equippedItems.Add(new EquipmentSessionSaveEntry
            {
                equipmentType = entry.equipmentType,
                equipmentId = entry.equipment != null ? entry.equipment.equipmentId : string.Empty,
                inventoryEntryId = entry.inventoryEntryId
            });
        }

        CPlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(saveData));
    }

    void LoadFromPrefs()
    {
        InitializeSlots();

        string json = CPlayerPrefs.GetString(SaveKey);
        if (string.IsNullOrEmpty(json))
            return;

        EquipmentSessionSaveData saveData = JsonUtility.FromJson<EquipmentSessionSaveData>(json);
        if (saveData == null || saveData.equippedItems == null)
            return;

        for (int i = 0; i < saveData.equippedItems.Count; i++)
        {
            EquipmentSessionSaveEntry saveEntry = saveData.equippedItems[i];
            EquipmentSessionEntry entry = GetEntry(saveEntry.equipmentType);
            if (entry == null)
                continue;

            entry.inventoryEntryId = saveEntry.inventoryEntryId;
            entry.equipment = ResolveEquipment(saveEntry.equipmentId, saveEntry.inventoryEntryId);
        }
    }

    BaseEquiment ResolveEquipment(string equipmentId, string inventoryEntryId)
    {
        EquipmentInventoryEntry inventoryEntry = EquipmentInventoryManager.GetOrCreate().GetEntryById(inventoryEntryId);
        if (inventoryEntry != null && inventoryEntry.equipment != null)
            return inventoryEntry.equipment;

        if (string.IsNullOrEmpty(equipmentId))
            return null;

        if (equipmentDatabase == null)
            equipmentDatabase = Resources.Load<EquipmentDatabaseSO>("00 Scripts/SO/Equiment/Equipment Database SO");

        return equipmentDatabase != null ? equipmentDatabase.FindById(equipmentId) : null;
    }
}