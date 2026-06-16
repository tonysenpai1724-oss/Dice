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
    public Button button;
    public Image BG;
    public Image border;

    BaseEquiment currentEquipment;
    Action<BaseEquiment> onSelected;

    public void OnPointerClick(PointerEventData eventData)
    {
        Select();
    }


    public void Setup(BaseEquiment equipment, Action<BaseEquiment> onSelectedCallback)
    {
        currentEquipment = equipment;
        onSelected = onSelectedCallback;
        EUIResourceResolution resolution = EUIResourceResolution.x200;

        if (icon != null)
            icon.sprite = equipment != null ? equipment.icon : null;

        if (txtName != null)
            txtName.text = equipment != null ? equipment.equipmentName : string.Empty;

        if (txtRarity != null)
            txtRarity.text = equipment != null ? equipment.rarity.ToString() : string.Empty;

        if (equippedMarker != null && equipment != null)
            equippedMarker.gameObject.SetActive(EquipmentSession.GetOrCreate().GetEquipped(equipment.equipmentType) == equipment);
        if (BG != null && equipment != null)
            BG.sprite = DataSystem.Instance.dataSprites.dicItemBg[resolution][equipment.rarity];
        if (border != null && equipment != null)
            border.sprite = DataSystem.Instance.dataSprites.dicItemBorder[resolution][equipment.rarity];

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(Select);
        }
    }

    void Select()
    {
        onSelected?.Invoke(currentEquipment);
    }
}
