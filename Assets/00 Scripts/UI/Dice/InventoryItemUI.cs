using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventoryItemUI : MonoBehaviour, IPointerClickHandler
{
    public Image icon;
    public Image equippedMarker;
    public TextMeshProUGUI txtName;
    public TextMeshProUGUI txtRarity;
    public TextMeshProUGUI txtQuantity;
    public Button button;
    public Image BG;
    public Image border;

    EquipmentInventoryEntry currentEntry;
    Action<EquipmentInventoryEntry> onSelected;

    public void OnPointerClick(PointerEventData eventData)
    {
        Select();
    }

    public void Setup(EquipmentInventoryEntry entry, Action<EquipmentInventoryEntry> onSelectedCallback)
    {
        currentEntry = entry;
        onSelected = onSelectedCallback;
        BaseEquiment currentEquipment = currentEntry != null ? currentEntry.equipment : null;
        EUIResourceResolution resolution = EUIResourceResolution.x200;

        if (icon != null)
            icon.sprite = currentEquipment != null ? currentEquipment.icon : null;

        if (txtName != null)
            txtName.text = currentEquipment != null ? currentEquipment.equipmentName : string.Empty;

        if (txtRarity != null)
            txtRarity.text = currentEquipment != null ? currentEquipment.rarity.ToString() : string.Empty;

        if (txtQuantity != null)
            txtQuantity.text = entry != null && entry.quantity > 1 ? $"x{entry.quantity}" : string.Empty;

        if (equippedMarker != null && currentEntry != null)
            equippedMarker.gameObject.SetActive(EquipmentSession.GetOrCreate().IsEntryEquipped(currentEntry.entryId));

        if (BG != null && currentEquipment != null)
            BG.sprite = DataSystem.Instance.dataSprites.dicItemBg[resolution][currentEquipment.rarity];

        if (border != null && currentEquipment != null)
            border.sprite = DataSystem.Instance.dataSprites.dicItemBorder[resolution][currentEquipment.rarity];

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(Select);
        }
    }

    void Select()
    {
        onSelected?.Invoke(currentEntry);
    }
}