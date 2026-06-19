using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TigerForge;
using UnityEngine;

[Serializable]
public class EquippedItemSlot
{
    public EquipmentType equipmentType;
    public BaseEquiment equippedItem;
}

public class EquipmentChangedEventData
{
    public EquipmentManager Manager { get; }
    public PlayerController Player { get; }
    public BaseEquiment Equipment { get; }
    public EquipmentType EquipmentType { get; }
    public HeroStatSnapshot FinalStats { get; }

    public EquipmentChangedEventData(
        EquipmentManager manager,
        PlayerController player,
        BaseEquiment equipment,
        EquipmentType equipmentType,
        HeroStatSnapshot finalStats)
    {
        Manager = manager;
        Player = player;
        Equipment = equipment;
        EquipmentType = equipmentType;
        FinalStats = finalStats;
    }
}

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance;

    [Header("Refs")]
    public PlayerController player;
    public HeroData heroDataOverride;
    public EquipmentDatabaseSO equipmentDatabase;

    [Header("Equipped Items")]
    public List<EquippedItemSlot> equippedSlots = new();

    public event Action<BaseEquiment> OnEquipmentEquipped;
    public event Action<BaseEquiment> OnEquipmentUnequipped;
    public event Action<HeroStatSnapshot> OnEquipmentStatsChanged;
    public event Action OnEquipmentChanged;

    void Awake()
    {
        Instance = this;
        InitializeSlots();
        EquipmentSession.GetOrCreate().SetDatabase(equipmentDatabase);
        ApplySessionLoadout();
    }

    void OnEnable()
    {
        EventManager.StartListening(Constant.ON_EQUIPMENT_SESSION_CHANGED, RefreshFromSessionEvent);
    }

    void OnDisable()
    {
        EventManager.StopListening(Constant.ON_EQUIPMENT_SESSION_CHANGED, RefreshFromSessionEvent);
    }

    void Reset()
    {
        InitializeSlots();
    }

    [Button]
    public void InitializeSlots()
    {
        if (equippedSlots == null)
        {
            equippedSlots = new List<EquippedItemSlot>();
        }

        Array equipmentTypes = Enum.GetValues(typeof(EquipmentType));

        for (int i = 0; i < equipmentTypes.Length; i++)
        {
            EquipmentType equipmentType = (EquipmentType)equipmentTypes.GetValue(i);

            if (GetSlot(equipmentType) != null)
                continue;

            equippedSlots.Add(new EquippedItemSlot
            {
                equipmentType = equipmentType,
                equippedItem = null
            });
        }
    }

    public HeroData GetHeroData()
    {
        if (heroDataOverride != null)
            return heroDataOverride;

        if (player != null)
            return player.data;

        return null;
    }

    public BaseEquiment GetEquippedItem(EquipmentType equipmentType)
    {
        EquippedItemSlot slot = GetSlot(equipmentType);
        return slot != null ? slot.equippedItem : null;
    }

    public List<BaseEquiment> GetAllEquippedItems()
    {
        List<BaseEquiment> result = new List<BaseEquiment>();

        if (equippedSlots == null)
            return result;

        for (int i = 0; i < equippedSlots.Count; i++)
        {
            BaseEquiment equippedItem = equippedSlots[i].equippedItem;
            if (equippedItem == null)
                continue;

            result.Add(equippedItem);
        }

        return result;
    }

    public HeroStatSnapshot GetCurrentTotalStats()
    {
        if (player != null && player.playerStats != null)
            return player.playerStats.ToHeroStatSnapshot(GetHeroData());

        return new HeroStatSnapshot(GetHeroData());
    }

    public bool IsEquipped(BaseEquiment equipment)
    {
        if (equipment == null || equippedSlots == null)
            return false;

        for (int i = 0; i < equippedSlots.Count; i++)
        {
            if (equippedSlots[i].equippedItem == equipment)
                return true;
        }

        return false;
    }

    public bool Equip(BaseEquiment equipment)
    {
        return EquipInternal(equipment, true);
    }

    public bool EquipById(string equipmentId)
    {
        BaseEquiment equipment = FindEquipmentById(equipmentId);
        return equipment != null && Equip(equipment);
    }

    public bool Unequip(EquipmentType equipmentType)
    {
        return UnequipInternal(equipmentType, true);
    }

    public bool Unequip(BaseEquiment equipment)
    {
        if (equipment == null)
            return false;

        return Unequip(equipment.equipmentType);
    }

    public bool TryUpgradeEquippedItem(EquipmentType equipmentType, IList<BaseEquiment> materials, out BaseEquiment upgradedEquipment)
    {
        upgradedEquipment = null;

        BaseEquiment equippedItem = GetEquippedItem(equipmentType);
        if (equippedItem == null)
            return false;

        if (!equippedItem.TryUpgradeRarity(materials, out upgradedEquipment))
            return false;

        return Equip(upgradedEquipment);
    }

    public void ApplySessionLoadout()
    {
        EquipmentSession session = EquipmentSession.Instance;
        if (session == null)
            return;

        List<BaseEquiment> equippedItems = session.GetAllEquipped();
        for (int i = 0; i < equippedItems.Count; i++)
        {
            EquipInternal(equippedItems[i], false);
        }

        NotifyEquipmentChanged();
    }

    bool EquipInternal(BaseEquiment equipment, bool syncSession)
    {
        if (equipment == null)
            return false;

        InitializeSlots();

        EquippedItemSlot slot = GetSlot(equipment.equipmentType);
        if (slot == null)
            return false;

        BaseEquiment previousEquipment = slot.equippedItem;
        if (previousEquipment == equipment)
            return true;

        if (previousEquipment != null)
        {
            previousEquipment.ApplyUnequipEffects(player);
            OnEquipmentUnequipped?.Invoke(previousEquipment);
            EmitEquipmentEvent(Constant.ON_EQUIPMENT_UNEQUIPPED, previousEquipment, previousEquipment.equipmentType);
        }

        slot.equippedItem = equipment;
        equipment.ApplyEquipEffects(player);

        if (syncSession)
            EquipmentSession.GetOrCreate().Equip(equipment);

        OnEquipmentEquipped?.Invoke(equipment);
        EmitEquipmentEvent(Constant.ON_EQUIPMENT_EQUIPPED, equipment, equipment.equipmentType);
        NotifyEquipmentChanged();
        return true;
    }

    bool UnequipInternal(EquipmentType equipmentType, bool syncSession)
    {
        EquippedItemSlot slot = GetSlot(equipmentType);
        if (slot == null || slot.equippedItem == null)
            return false;

        BaseEquiment removedEquipment = slot.equippedItem;
        slot.equippedItem = null;

        removedEquipment.ApplyUnequipEffects(player);

        if (syncSession)
            EquipmentSession.GetOrCreate().Unequip(equipmentType);

        OnEquipmentUnequipped?.Invoke(removedEquipment);
        EmitEquipmentEvent(Constant.ON_EQUIPMENT_UNEQUIPPED, removedEquipment, removedEquipment.equipmentType);
        NotifyEquipmentChanged();
        return true;
    }

    BaseEquiment FindEquipmentById(string equipmentId)
    {
        return equipmentDatabase != null ? equipmentDatabase.FindById(equipmentId) : null;
    }

    EquippedItemSlot GetSlot(EquipmentType equipmentType)
    {
        if (equippedSlots == null)
            return null;

        for (int i = 0; i < equippedSlots.Count; i++)
        {
            if (equippedSlots[i].equipmentType == equipmentType)
                return equippedSlots[i];
        }

        return null;
    }

    void RefreshFromSessionEvent()
    {
        ApplySessionLoadout();
    }

    void NotifyEquipmentChanged()
    {
        if (player != null)
            player.RefreshStatsFromEquipment();

        HeroStatSnapshot finalStats = GetCurrentTotalStats();
        OnEquipmentChanged?.Invoke();
        OnEquipmentStatsChanged?.Invoke(finalStats);
        EventManager.EmitEventData(Constant.ON_EQUIPMENT_CHANGED,
            new EquipmentChangedEventData(this, player, null, EquipmentType.Weapon, finalStats));
        EventManager.EmitEventData(Constant.ON_PLAYER_EQUIPMENT_STATS_CHANGED,
            new EquipmentChangedEventData(this, player, null, EquipmentType.Weapon, finalStats));
    }

    void EmitEquipmentEvent(string eventName, BaseEquiment equipment, EquipmentType equipmentType)
    {
        HeroStatSnapshot finalStats = GetCurrentTotalStats();
        EventManager.EmitEventData(eventName,
            new EquipmentChangedEventData(this, player, equipment, equipmentType, finalStats));
    }
}
