using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentDetailPanel : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI txtName;
    public TextMeshProUGUI txtRarity;
    public TextMeshProUGUI txtDescription;
    public TextMeshProUGUI txtStats;
    public Button btnEquip;
    public Button btnUnequip;
    public Button btnUpgrade;

    EquipmentInventoryEntry currentEntry;
    BaseEquiment CurrentEquipment => currentEntry != null ? currentEntry.equipment : null;

    void Awake()
    {
        gameObject.SetActive(false);
    }

    public void Show(EquipmentInventoryEntry entry)
    {
        currentEntry = entry;
        gameObject.SetActive(true);
        RefreshView();
    }

    public void Hide()
    {
        currentEntry = null;
        gameObject.SetActive(false);
    }

    public void OnClickEquip()
    {
        if (currentEntry == null)
            return;

        EquipmentSession.GetOrCreate().EquipEntry(currentEntry.entryId);
        RefreshView();
        Hide();
    }

    public void OnClickUnequip()
    {
        if (CurrentEquipment == null)
            return;

        EquipmentSession.GetOrCreate().Unequip(CurrentEquipment.equipmentType);
        RefreshView();
        Hide();
    }

    public void OnClickUpgrade()
    {
        if (CurrentEquipment == null)
            return;

        if (currentEntry != null && EquipmentUpgradeService.GetOrCreate().TryUpgrade(currentEntry.entryId, out BaseEquiment upgradedEquipment))
        {
            currentEntry = null;
        }

        RefreshView();
        Hide();
    }

    void RefreshView()
    {
        if (CurrentEquipment == null)
        {
            Hide();
            return;
        }

        if (icon != null)
            icon.sprite = CurrentEquipment.icon;

        if (txtName != null)
            txtName.text = CurrentEquipment.equipmentName;

        if (txtRarity != null)
            txtRarity.text = CurrentEquipment.rarity.ToString();

        if (txtDescription != null)
            txtDescription.text = CurrentEquipment.description;

        if (txtStats != null)
            txtStats.text = CurrentEquipment.StatsPreview;

        bool isEquipped = currentEntry != null && EquipmentSession.GetOrCreate().IsEntryEquipped(currentEntry.entryId);

        if (btnEquip != null)
        {
            btnEquip.gameObject.SetActive(!isEquipped);
            btnEquip.onClick.RemoveAllListeners();
            btnEquip.onClick.AddListener(OnClickEquip);
        }

        if (btnUnequip != null)
        {
            btnUnequip.gameObject.SetActive(isEquipped);
            btnUnequip.onClick.RemoveAllListeners();
            btnUnequip.onClick.AddListener(OnClickUnequip);
        }

        if (btnUpgrade != null)
        {
            btnUpgrade.onClick.RemoveAllListeners();
            btnUpgrade.onClick.AddListener(OnClickUpgrade);
        }
    }
}
