using System.Collections.Generic;
using TigerForge;
using UnityEngine;

public class EquipmentUpgradeService : MonoBehaviour
{
    public static EquipmentUpgradeService Instance;

    [SerializeField] EquipmentDatabaseSO equipmentDatabase;

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

    public bool TryUpgrade(string targetEntryId, out BaseEquiment upgradedEquipment)
    {
        upgradedEquipment = null;
        Debug.Log($"[EquipmentUpgrade] TryUpgrade start | entryId={targetEntryId}");

        EquipmentInventoryManager inventory = EquipmentInventoryManager.GetOrCreate();
        EquipmentInventoryEntry targetEntry = inventory.GetEntryById(targetEntryId);
        if (targetEntry == null)
        {
            Debug.LogWarning($"[EquipmentUpgrade] FAIL | targetEntry not found | entryId={targetEntryId}");
            return false;
        }

        Debug.Log($"[EquipmentUpgrade] Entry found | entryId={targetEntry.entryId} | equipmentId={targetEntry.equipmentId} | qty={targetEntry.quantity} | equipment={(targetEntry.equipment != null ? targetEntry.equipment.equipmentName : "NULL")}");

        if (targetEntry.equipment == null)
        {
            Debug.LogWarning($"[EquipmentUpgrade] FAIL | targetEntry.equipment is null | entryId={targetEntryId} | equipmentId={targetEntry.equipmentId}");
            return false;
        }

        BaseEquiment targetEquipment = targetEntry.equipment;
        List<EquipmentInventoryEntry> allEntries = inventory.GetAllEntries();
        List<EquipmentInventoryEntry> matchedEntries = new List<EquipmentInventoryEntry>();
        int totalQuantity = 0;

        for (int i = 0; i < allEntries.Count; i++)
        {
            EquipmentInventoryEntry entry = allEntries[i];
            if (!IsSameUpgradeGroup(targetEquipment, entry))
                continue;

            matchedEntries.Add(entry);
            totalQuantity += entry.quantity;
            Debug.Log($"[EquipmentUpgrade] Matched entry | entryId={entry.entryId} | qty={entry.quantity} | runningTotal={totalQuantity}");
        }

        if (totalQuantity < BaseEquiment.UpgradeRequireCount)
        {
            Debug.LogWarning($"[EquipmentUpgrade] FAIL | total quantity not enough | need={BaseEquiment.UpgradeRequireCount} | total={totalQuantity} | item={targetEquipment.equipmentName}");
            return false;
        }

        upgradedEquipment = ResolveUpgradeResult(targetEquipment);
        if (upgradedEquipment == null)
        {
            Debug.LogWarning($"[EquipmentUpgrade] FAIL | upgrade result null | item={targetEquipment.equipmentName} | equipmentId={targetEquipment.equipmentId} | type={targetEquipment.equipmentType} | rarity={targetEquipment.rarity}");
            return false;
        }

        Debug.Log($"[EquipmentUpgrade] Upgrade result | from={targetEquipment.equipmentName} | to={upgradedEquipment.equipmentName} | toId={upgradedEquipment.equipmentId}");

        EquipmentSession session = EquipmentSession.GetOrCreate();
        bool wasEquipped = session.IsEntryEquipped(targetEntryId);
        Debug.Log($"[EquipmentUpgrade] Equipped state | entryId={targetEntryId} | wasEquipped={wasEquipped}");

        int remainingToRemove = BaseEquiment.UpgradeRequireCount;
        for (int i = 0; i < matchedEntries.Count && remainingToRemove > 0; i++)
        {
            EquipmentInventoryEntry entry = matchedEntries[i];
            if (entry == null || entry.quantity <= 0)
                continue;

            int removeAmount = Mathf.Min(entry.quantity, remainingToRemove);
            bool removed = inventory.RemoveFromEntry(entry.entryId, removeAmount);
            Debug.Log($"[EquipmentUpgrade] RemoveFromEntry | entryId={entry.entryId} | removeCount={removeAmount} | result={removed}");

            if (!removed)
            {
                Debug.LogWarning($"[EquipmentUpgrade] FAIL | RemoveFromEntry returned false | entryId={entry.entryId}");
                return false;
            }

            remainingToRemove -= removeAmount;
        }

        if (remainingToRemove > 0)
        {
            Debug.LogWarning($"[EquipmentUpgrade] FAIL | remainingToRemove > 0 after removal | remaining={remainingToRemove}");
            return false;
        }

        inventory.AddEquipment(upgradedEquipment);
        Debug.Log($"[EquipmentUpgrade] Added upgraded equipment to inventory | item={upgradedEquipment.equipmentName} | equipmentId={upgradedEquipment.equipmentId}");

        if (wasEquipped)
        {
            session.Unequip(targetEquipment.equipmentType);
            Debug.Log($"[EquipmentUpgrade] Unequipped old slot after upgrade | type={targetEquipment.equipmentType}");

            EquipmentInventoryEntry upgradedEntry = FindBestEntryForEquipment(inventory, upgradedEquipment);
            if (upgradedEntry != null)
            {
                session.EquipEntry(upgradedEntry.entryId);
                Debug.Log($"[EquipmentUpgrade] Equipped upgraded item | entryId={upgradedEntry.entryId} | item={upgradedEquipment.equipmentName}");
            }
            else
            {
                Debug.LogWarning($"[EquipmentUpgrade] Could not find upgraded entry to re-equip | item={upgradedEquipment.equipmentName}");
            }

            if (EquipmentManager.Instance != null)
            {
                EquipmentManager.Instance.ApplySessionLoadout();
                Debug.Log("[EquipmentUpgrade] Forced EquipmentManager.ApplySessionLoadout after upgrade");
            }
        }

        EventManager.EmitEventData(Constant.ON_EQUIPMENT_UPGRADED, upgradedEquipment);
        Debug.Log($"[EquipmentUpgrade] SUCCESS | entryId={targetEntryId} | upgraded={upgradedEquipment.equipmentName}");
        return true;
    }

    public bool TryUpgrade(BaseEquiment targetEquipment, out BaseEquiment upgradedEquipment)
    {
        upgradedEquipment = null;

        if (targetEquipment == null)
        {
            Debug.LogWarning("[EquipmentUpgrade] FAIL | targetEquipment null");
            return false;
        }

        Debug.Log($"[EquipmentUpgrade] TryUpgrade by equipment | item={targetEquipment.equipmentName} | equipmentId={targetEquipment.equipmentId}");

        EquipmentInventoryManager inventory = EquipmentInventoryManager.GetOrCreate();
        List<EquipmentInventoryEntry> entries = inventory.GetAllEntries();
        for (int i = 0; i < entries.Count; i++)
        {
            EquipmentInventoryEntry entry = entries[i];
            if (entry == null)
                continue;

            Debug.Log($"[EquipmentUpgrade] Scan entry | entryId={entry.entryId} | equipmentId={entry.equipmentId} | qty={entry.quantity} | equipment={(entry.equipment != null ? entry.equipment.equipmentName : "NULL")}");

            if (!IsSameUpgradeGroup(targetEquipment, entry))
                continue;

            return TryUpgrade(entry.entryId, out upgradedEquipment);
        }

        Debug.LogWarning($"[EquipmentUpgrade] FAIL | no inventory entry matched target equipment | item={targetEquipment.equipmentName}");
        return false;
    }

    bool IsSameUpgradeGroup(BaseEquiment targetEquipment, EquipmentInventoryEntry entry)
    {
        if (targetEquipment == null || entry == null || entry.equipment == null)
            return false;

        string targetId = !string.IsNullOrEmpty(targetEquipment.equipmentId) ? targetEquipment.equipmentId : targetEquipment.name;
        string entryId = !string.IsNullOrEmpty(entry.equipmentId) ? entry.equipmentId : entry.equipment.name;

        return targetEquipment.equipmentType == entry.equipment.equipmentType &&
               targetEquipment.rarity == entry.equipment.rarity &&
               targetId == entryId;
    }

    EquipmentInventoryEntry FindBestEntryForEquipment(EquipmentInventoryManager inventory, BaseEquiment equipment)
    {
        if (inventory == null || equipment == null)
            return null;

        List<EquipmentInventoryEntry> entries = inventory.GetAllEntries();
        for (int i = 0; i < entries.Count; i++)
        {
            EquipmentInventoryEntry entry = entries[i];
            if (entry == null || entry.equipment != equipment || entry.quantity <= 0)
                continue;

            return entry;
        }

        return null;
    }

    BaseEquiment ResolveUpgradeResult(BaseEquiment targetEquipment)
    {
        if (targetEquipment == null)
        {
            Debug.LogWarning("[EquipmentUpgrade] ResolveUpgradeResult FAIL | targetEquipment null");
            return null;
        }

        if (targetEquipment.upgradeResult != null)
        {
            Debug.Log($"[EquipmentUpgrade] ResolveUpgradeResult direct | from={targetEquipment.equipmentName} | to={targetEquipment.upgradeResult.equipmentName}");
            return targetEquipment.upgradeResult;
        }

        if (equipmentDatabase == null)
            equipmentDatabase = Resources.Load<EquipmentDatabaseSO>("00 Scripts/SO/Equiment/Equipment Database SO");

        if (equipmentDatabase == null || equipmentDatabase.equipments == null)
        {
            Debug.LogWarning($"[EquipmentUpgrade] ResolveUpgradeResult FAIL | database missing | item={targetEquipment.equipmentName}");
            return null;
        }

        ERarity nextRarity = BaseEquiment.GetNextRarity(targetEquipment.rarity);
        Debug.Log($"[EquipmentUpgrade] ResolveUpgradeResult fallback | item={targetEquipment.equipmentName} | currentRarity={targetEquipment.rarity} | nextRarity={nextRarity}");

        for (int i = 0; i < equipmentDatabase.equipments.Count; i++)
        {
            BaseEquiment candidate = equipmentDatabase.equipments[i];
            if (candidate == null)
                continue;

            Debug.Log($"[EquipmentUpgrade] Check candidate | candidate={candidate.equipmentName} | type={candidate.equipmentType} | rarity={candidate.rarity}");

            if (candidate.equipmentType != targetEquipment.equipmentType)
                continue;

            if (candidate.rarity != nextRarity)
                continue;

            Debug.Log($"[EquipmentUpgrade] ResolveUpgradeResult fallback matched | to={candidate.equipmentName}");
            return candidate;
        }

        Debug.LogWarning($"[EquipmentUpgrade] ResolveUpgradeResult FAIL | no fallback match | item={targetEquipment.equipmentName}");
        return null;
    }
}