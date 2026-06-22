using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public abstract class ShopItemBase : MonoBehaviour
{
    public EnumItemType enumItemType;
    public TextMeshProUGUI txtName;
    public TextMeshProUGUI txtDesc;
    public TextMeshProUGUI txtPrice;
    public Image icon;
    public Button buyButton;

    protected int price;
    protected bool purchased;

    protected virtual void Awake()
    {
        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(Buy);
            buyButton.onClick.AddListener(Buy);
        }
    }

    public abstract void SetupItem();

    protected void SetupCommon(string itemName, string itemDesc, int itemPrice, Sprite itemIcon = null)
    {
        price = Mathf.Max(0, itemPrice);

        if (txtName != null)
            txtName.text = itemName;

        if (txtDesc != null)
            txtDesc.text = itemDesc;

        if (txtPrice != null)
            txtPrice.text = price.ToString();

        if (icon != null)
            icon.sprite = itemIcon;

        RefreshButtonState();
    }

    protected virtual void RefreshButtonState()
    {
        if (buyButton != null)
            buyButton.interactable = !purchased && CanAfford();
    }

    protected bool CanAfford()
    {
        return IPlayerResource.Instance != null &&
               IPlayerResource.Instance.CheckResource(new CommonResource(ECommonResource.Coin, -price));
    }

    protected bool TrySpendCoin()
    {
        if (price <= 0)
            return true;

        if (!CanAfford())
            return false;

        IPlayerResource.Instance.AddResource(
            new List<GameResource> { new CommonResource(ECommonResource.Coin, -price) },
            EResourceFrom.SpendIngame
        );
        return true;
    }

    protected void MarkPurchased()
    {
        purchased = true;
        RefreshButtonState();
    }

    public abstract void Buy();
}

public enum EnumItemType
{
    Dice,
    Rune
}
