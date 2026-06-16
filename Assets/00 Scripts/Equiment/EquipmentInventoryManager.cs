using System.Collections.Generic;
using TigerForge;
using UnityEngine;

public class EquipmentInventoryManager : MonoBehaviour
{
    public static EquipmentInventoryManager Instance;

    [SerializeField] List<BaseEquiment> ownedEquipments = new();

    public IReadOnlyList<BaseEquiment> OwnedEquipments => ownedEquipments;

    void Awake()
    {


        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static EquipmentInventoryManager GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        GameObject inventoryObject = new GameObject("EquipmentInventoryManager");
        return inventoryObject.AddComponent<EquipmentInventoryManager>();
    }

    public void AddEquipment(BaseEquiment equipment)
    {
        if (equipment == null)
            return;

        ownedEquipments.Add(equipment);
        NotifyChanged();
    }

    public bool RemoveEquipment(BaseEquiment equipment)
    {
        if (equipment == null)
            return false;

        bool removed = ownedEquipments.Remove(equipment);
        if (removed)
            NotifyChanged();

        return removed;
    }

    public List<BaseEquiment> GetAllEquipments()
    {
        return new List<BaseEquiment>(ownedEquipments);
    }

    public List<BaseEquiment> GetEquipmentsByTypeAndRarity(EquipmentType equipmentType, ERarity rarity)
    {
        List<BaseEquiment> result = new List<BaseEquiment>();

        for (int i = 0; i < ownedEquipments.Count; i++)
        {
            BaseEquiment equipment = ownedEquipments[i];
            if (equipment == null)
                continue;

            if (equipment.equipmentType != equipmentType || equipment.rarity != rarity)
                continue;

            result.Add(equipment);
        }

        return result;
    }

    void NotifyChanged()
    {
        EventManager.EmitEventData(Constant.ON_EQUIPMENT_INVENTORY_CHANGED, this);
    }
}
