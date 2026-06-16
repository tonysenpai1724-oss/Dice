using System;
using System.Collections.Generic;
using TigerForge;
using UnityEngine;

[Serializable]
public class EquipmentSessionEntry
{
    public EquipmentType equipmentType;
    public BaseEquiment equipment;
}

public class EquipmentSession : MonoBehaviour
{
    public static EquipmentSession Instance;

    [SerializeField] List<EquipmentSessionEntry> equippedItems = new();

    public IReadOnlyList<EquipmentSessionEntry> EquippedItems => equippedItems;

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeSlots();
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
                equipment = null
            });
        }
    }

    public void Equip(BaseEquiment equipment)
    {
        if (equipment == null)
            return;

        InitializeSlots();

        EquipmentSessionEntry entry = GetEntry(equipment.equipmentType);
        if (entry == null)
            return;

        entry.equipment = equipment;
        NotifyChanged();
    }

    public void Unequip(EquipmentType equipmentType)
    {
        EquipmentSessionEntry entry = GetEntry(equipmentType);
        if (entry == null)
            return;

        entry.equipment = null;
        NotifyChanged();
    }

    public BaseEquiment GetEquipped(EquipmentType equipmentType)
    {
        EquipmentSessionEntry entry = GetEntry(equipmentType);
        return entry != null ? entry.equipment : null;
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
        EventManager.EmitEventData(Constant.ON_EQUIPMENT_SESSION_CHANGED, this);
    }
}
