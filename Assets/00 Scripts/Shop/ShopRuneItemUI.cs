using UnityEngine;

public class ShopRuneItemUI : ShopItemBase
{
    public RuneSkillData runeData;
    public int coinPrice = 15;

    public override void SetupItem()
    {
        if (runeData == null)
            return;

        enumItemType = EnumItemType.Rune;
        SetupCommon(runeData.name, runeData.TargetType.ToString(), coinPrice);
    }

    public void Setup(RuneSkillData data, int itemPrice)
    {
        runeData = data;
        coinPrice = itemPrice;
        SetupItem();
    }

    public override void Buy()
    {
        if (runeData == null)
            return;

        if (!TrySpendCoin())
            return;

        if (!RuneManager.Instance.TryAddRune(runeData))
        {
            UIManager.Instance.ShowDialog("No empty rune slot");
            return;
        }

        MarkPurchased();
    }
}
