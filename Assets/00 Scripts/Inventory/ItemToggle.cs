using System;
using UnityEngine;
using UnityEngine.UI;

public enum ItemToggleType
{
    None,
    Dice,
    Rune
}

public class ItemToggle : MonoBehaviour
{
    public ItemToggleType itemType;
    public DiceData data;
    public RuneSkillData runeData;
    public Button btn;
    public RawImage previewRawImage;
    public Image previewImage;

    Texture2D previewTexture;
    Sprite previewSprite;
    Action<ItemToggle> onSelected;

    public Sprite PreviewSprite => previewSprite;
    public Texture2D PreviewTexture => previewTexture;

    void Awake()
    {
        CacheComponents();
    }

    public void Start()
    {
        CacheComponents();

        if (btn != null)
        {
            btn.onClick.RemoveListener(OnClick);
            btn.onClick.AddListener(OnClick);
        }
    }

    public void Setup(DiceData diceData, Texture2D texture, Action<ItemToggle> onSelectedCallback = null)
    {
        itemType = ItemToggleType.Dice;
        data = diceData;
        runeData = null;
        onSelected = onSelectedCallback;
        SetPreview(texture);

        if (btn != null)
            btn.interactable = data != null;
    }

    public void Setup(RuneSkillData runeSkillData, Sprite sprite, Action<ItemToggle> onSelectedCallback = null)
    {
        itemType = ItemToggleType.Rune;
        data = null;
        runeData = runeSkillData;
        onSelected = onSelectedCallback;
        SetPreview(sprite);

        if (btn != null)
            btn.interactable = runeData != null;
    }

    public void Clear()
    {
        itemType = ItemToggleType.None;
        data = null;
        runeData = null;
        onSelected = null;
        SetPreview((Texture2D)null);

        if (btn != null)
            btn.interactable = false;
    }

    public void OnClick()
    {
        if (itemType == ItemToggleType.None)
            return;

        switch (itemType)
        {
            case ItemToggleType.Dice:
                if (data == null)
                    return;
                Debug.Log(data.diceName);
                break;
            case ItemToggleType.Rune:
                if (runeData == null)
                    return;
                Debug.Log(runeData.name);
                break;
            default:
                return;
        }

        onSelected?.Invoke(this);
    }

    void CacheComponents()
    {
        if (btn == null)
            btn = GetComponent<Button>();

        if (previewRawImage == null)
            previewRawImage = GetComponentInChildren<RawImage>(true);

        if (previewImage == null)
            previewImage = GetComponent<Image>();
    }

    void SetPreview(Texture2D texture)
    {
        ReleasePreview();
        previewTexture = texture;

        if (previewRawImage != null)
        {
            previewRawImage.texture = previewTexture;
            previewRawImage.enabled = previewTexture != null;
            return;
        }

        if (previewImage == null)
            return;

        if (previewTexture == null)
        {
            previewImage.sprite = null;
            previewImage.enabled = false;
            return;
        }

        previewSprite = Sprite.Create(
            previewTexture,
            new Rect(0f, 0f, previewTexture.width, previewTexture.height),
            new Vector2(0.5f, 0.5f)
        );
        previewImage.sprite = previewSprite;
        previewImage.preserveAspect = true;
        previewImage.enabled = true;
    }

    void SetPreview(Sprite sprite)
    {
        ReleasePreview();

        if (previewRawImage != null)
        {
            previewRawImage.texture = null;
            previewRawImage.enabled = false;
        }

        if (previewImage == null)
            return;

        previewImage.sprite = sprite;
        previewImage.preserveAspect = true;
        previewImage.enabled = sprite != null;
    }

    void ReleasePreview()
    {
        if (previewRawImage != null)
            previewRawImage.texture = null;

        if (previewSprite != null)
            Destroy(previewSprite);

        if (previewTexture != null)
            Destroy(previewTexture);

        previewSprite = null;
        previewTexture = null;
    }

    void OnDestroy()
    {
        ReleasePreview();
    }
}
