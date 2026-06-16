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

    BaseEquiment currentEquipment;

    void Awake()
    {
        gameObject.SetActive(false);
    }

    public void Show(BaseEquiment equipment)
    {
        currentEquipment = equipment;
        gameObject.SetActive(true);
        RefreshView();
    }

    public void Hide()
    {
        currentEquipment = null;
        gameObject.SetActive(false);
    }

    public void OnClickEquip()
    {
        if (currentEquipment == null)
            return;

        EquipmentSession.GetOrCreate().Equip(currentEquipment);
        RefreshView();
        Hide();
    }

    public void OnClickUnequip()
    {
        if (currentEquipment == null)
            return;

        EquipmentSession.GetOrCreate().Unequip(currentEquipment.equipmentType);
        RefreshView();
        Hide();
    }

    public void OnClickUpgrade()
    {
        if (currentEquipment == null)
            return;

        if (EquipmentUpgradeService.GetOrCreate().TryUpgrade(currentEquipment, out BaseEquiment upgradedEquipment))
        {
            currentEquipment = upgradedEquipment;
        }

        RefreshView();
        Hide();
    }

    void RefreshView()
    {
        if (currentEquipment == null)
        {
            Hide();
            return;
        }

        if (icon != null)
            icon.sprite = currentEquipment.icon;

        if (txtName != null)
            txtName.text = currentEquipment.equipmentName;

        if (txtRarity != null)
            txtRarity.text = currentEquipment.rarity.ToString();

        if (txtDescription != null)
            txtDescription.text = currentEquipment.description;

        if (txtStats != null)
            txtStats.text = currentEquipment.StatsPreview;

        bool isEquipped = EquipmentSession.GetOrCreate().GetEquipped(currentEquipment.equipmentType) == currentEquipment;

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
