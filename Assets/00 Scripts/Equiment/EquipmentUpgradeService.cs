using System.Collections.Generic;
using TigerForge;
using UnityEngine;

public class EquipmentUpgradeService : MonoBehaviour
{
    public static EquipmentUpgradeService Instance;

    void Awake()
    {


        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static EquipmentUpgradeService GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        GameObject serviceObject = new GameObject("EquipmentUpgradeService");
        return serviceObject.AddComponent<EquipmentUpgradeService>();
    }

    public bool TryUpgrade(BaseEquiment targetEquipment, out BaseEquiment upgradedEquipment)
    {
        upgradedEquipment = null;

        if (targetEquipment == null)
            return false;

        EquipmentInventoryManager inventory = EquipmentInventoryManager.GetOrCreate();
        List<BaseEquiment> candidates = inventory.GetEquipmentsByTypeAndRarity(
            targetEquipment.equipmentType,
            targetEquipment.rarity
        );

        List<BaseEquiment> materials = new List<BaseEquiment>();

        for (int i = 0; i < candidates.Count; i++)
        {
            BaseEquiment candidate = candidates[i];
            if (candidate == null)
                continue;

            materials.Add(candidate);
            if (materials.Count >= BaseEquiment.UpgradeRequireCount)
                break;
        }

        if (!targetEquipment.TryUpgradeRarity(materials, out upgradedEquipment))
            return false;

        for (int i = 0; i < BaseEquiment.UpgradeRequireCount; i++)
        {
            inventory.RemoveEquipment(materials[i]);
        }

        inventory.AddEquipment(upgradedEquipment);

        EquipmentSession session = EquipmentSession.GetOrCreate();
        if (session.GetEquipped(targetEquipment.equipmentType) == targetEquipment)
        {
            session.Equip(upgradedEquipment);
        }

        EventManager.EmitEventData(Constant.ON_EQUIPMENT_UPGRADED, upgradedEquipment);
        return true;
    }
}
