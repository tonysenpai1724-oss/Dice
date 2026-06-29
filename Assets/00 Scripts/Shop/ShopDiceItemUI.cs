using UnityEngine;

public class ShopDiceItemUI : ShopItemBase
{
    public DiceData diceData;
    public int coinPrice = 10;

    Texture2D ownedPreviewTexture;
    Sprite ownedPreviewSprite;
    Sprite previewSprite;

    public override void SetupItem()
    {
        if (diceData == null)
            return;

        enumItemType = EnumItemType.Dice;
        SetupCommon(diceData.diceName, $"Level {diceData.level} {diceData.type}", coinPrice, previewSprite);
    }

    public void Setup(DiceData data, int itemPrice)
    {
        Setup(data, itemPrice, (Sprite)null);
    }

    public void Setup(DiceData data, int itemPrice, Sprite itemIcon)
    {
        ReleaseOwnedPreview();
        diceData = data;
        coinPrice = itemPrice;
        ownedPreviewTexture = CopySpriteTexture(itemIcon);
        ownedPreviewSprite = CreateSprite(ownedPreviewTexture);
        previewSprite = ownedPreviewSprite != null ? ownedPreviewSprite : itemIcon;
        SetupItem();
    }

    public void Setup(DiceData data, int itemPrice, Texture2D previewTexture)
    {
        ReleaseOwnedPreview();
        diceData = data;
        coinPrice = itemPrice;
        ownedPreviewTexture = previewTexture;
        ownedPreviewSprite = CreateSprite(previewTexture);
        previewSprite = ownedPreviewSprite;
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

    Sprite CreateSprite(Texture2D texture)
    {
        if (texture == null)
            return null;

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );
    }

    Texture2D CopySpriteTexture(Sprite sourceSprite)
    {
        if (sourceSprite == null || sourceSprite.texture == null)
            return null;

        Rect rect = sourceSprite.textureRect;
        int width = Mathf.RoundToInt(rect.width);
        int height = Mathf.RoundToInt(rect.height);
        if (width <= 0 || height <= 0)
            return null;

        RenderTexture renderTexture = RenderTexture.GetTemporary(
            sourceSprite.texture.width,
            sourceSprite.texture.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear
        );

        RenderTexture previous = RenderTexture.active;
        Graphics.Blit(sourceSprite.texture, renderTexture);
        RenderTexture.active = renderTexture;

        Texture2D copiedTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        copiedTexture.ReadPixels(rect, 0, 0);
        copiedTexture.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTexture);

        return copiedTexture;
    }

    void ReleaseOwnedPreview()
    {
        bool previewUsesOwnedSprite = previewSprite == ownedPreviewSprite;

        if (ownedPreviewSprite != null)
            Destroy(ownedPreviewSprite);

        if (ownedPreviewTexture != null)
            Destroy(ownedPreviewTexture);

        ownedPreviewSprite = null;
        ownedPreviewTexture = null;

        if (previewUsesOwnedSprite)
            previewSprite = null;
    }

    void OnDestroy()
    {
        ReleaseOwnedPreview();
    }
}