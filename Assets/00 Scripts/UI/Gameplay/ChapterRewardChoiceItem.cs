using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChapterRewardChoiceItem : MonoBehaviour
{
    public TextMeshProUGUI txtTitle;
    public TextMeshProUGUI txtDescription;
    public Button button;
    public Image icon;

    ChapterRewardChoiceOption currentOption;
    Action<ChapterRewardChoiceOption> onSelect;
    Texture2D ownedIconTexture;
    Sprite ownedIconSprite;

    public void Setup(ChapterRewardChoiceOption option, Action<ChapterRewardChoiceOption> onSelectCallback)
    {
        Setup(option, onSelectCallback, (Sprite)null);
    }

    public void Setup(ChapterRewardChoiceOption option, Action<ChapterRewardChoiceOption> onSelectCallback, Sprite itemIcon)
    {
        currentOption = option;
        onSelect = onSelectCallback;
        SetOwnedIcon(itemIcon);
        SetupTextAndButton(option);
    }

    public void Setup(ChapterRewardChoiceOption option, Action<ChapterRewardChoiceOption> onSelectCallback, Texture2D itemIconTexture)
    {
        currentOption = option;
        onSelect = onSelectCallback;
        SetOwnedIcon(itemIconTexture);
        SetupTextAndButton(option);
    }

    void SetupTextAndButton(ChapterRewardChoiceOption option)
    {
        if (txtTitle != null)
            txtTitle.text = option != null ? option.title : string.Empty;

        if (txtDescription != null)
            txtDescription.text = option != null ? option.description : string.Empty;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(Select);
        }
    }

    void SetOwnedIcon(Sprite sourceSprite)
    {
        ReleaseOwnedIcon();
        ownedIconTexture = CopySpriteTexture(sourceSprite);
        ownedIconSprite = CreateSprite(ownedIconTexture);
        SetIcon(ownedIconSprite != null ? ownedIconSprite : sourceSprite);
    }

    void SetOwnedIcon(Texture2D texture)
    {
        ReleaseOwnedIcon();
        ownedIconTexture = CopyTexture(texture);
        ownedIconSprite = CreateSprite(ownedIconTexture);
        SetIcon(ownedIconSprite);
    }

    void SetIcon(Sprite sprite)
    {
        if (icon == null)
            return;

        icon.sprite = sprite;
        icon.enabled = sprite != null;
        icon.preserveAspect = true;

        Color color = icon.color;
        color.a = sprite != null ? 1f : color.a;
        icon.color = color;
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

    Texture2D CopyTexture(Texture2D sourceTexture)
    {
        if (sourceTexture == null)
            return null;

        RenderTexture renderTexture = RenderTexture.GetTemporary(
            sourceTexture.width,
            sourceTexture.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear
        );

        RenderTexture previous = RenderTexture.active;
        Graphics.Blit(sourceTexture, renderTexture);
        RenderTexture.active = renderTexture;

        Texture2D copiedTexture = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32, false);
        copiedTexture.ReadPixels(new Rect(0f, 0f, sourceTexture.width, sourceTexture.height), 0, 0);
        copiedTexture.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTexture);

        return copiedTexture;
    }

    void ReleaseOwnedIcon()
    {
        if (ownedIconSprite != null)
            Destroy(ownedIconSprite);

        if (ownedIconTexture != null)
            Destroy(ownedIconTexture);

        ownedIconSprite = null;
        ownedIconTexture = null;
    }

    void Select()
    {
        onSelect?.Invoke(currentOption);
    }

    void OnDestroy()
    {
        ReleaseOwnedIcon();
    }
}
