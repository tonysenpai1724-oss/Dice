using UnityEngine;

public class ShopDiceItemUI : ShopItemBase
{
    public DiceData diceData;
    public int coinPrice = 10;

    public override void SetupItem()
    {
        if (diceData == null)
            return;

        enumItemType = EnumItemType.Dice;
        SetupCommon(diceData.diceName, $"Level {diceData.level} {diceData.type}", coinPrice);
    }

    public void Setup(DiceData data, int itemPrice)
    {
        diceData = data;
        coinPrice = itemPrice;
        SetupItem();
    }

    public override void Buy()
    {
        if (diceData == null)
            return;

        if (!TrySpendCoin())
            return;

        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
            player.AddDiceData(diceData);
        else
            ChapterDiceSession.GetOrCreate().AddDiceData(diceData);

        MarkPurchased();
    }
}
