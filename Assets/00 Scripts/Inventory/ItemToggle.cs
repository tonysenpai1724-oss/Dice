using System;
using UnityEngine;
using UnityEngine.UI;

public class ItemToggle : MonoBehaviour
{
    public DiceData data;
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
        data = diceData;
        onSelected = onSelectedCallback;
        SetPreview(texture);

        if (btn != null)
            btn.interactable = data != null;
    }

    public void Clear()
    {
        data = null;
        onSelected = null;
        SetPreview(null);

        if (btn != null)
            btn.interactable = false;
    }

    public void OnClick()
    {
        if (data == null)
            return;

        Debug.Log(data.diceName);
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
