using UnityEngine;
using UnityEngine.UI;

public class DiceQueueUIItem : MonoBehaviour
{
    public DiceData data;
    public Button button;
    public RawImage previewRawImage;
    public Image previewImage;
    public Vector2 defaultPreviewSize = new Vector2(100f, 100f);

    Texture2D previewTexture;
    Sprite previewSprite;

    public void Setup(DiceData diceData, Texture2D texture)
    {
        data = diceData;
        SetPreview(texture);

        if (button != null)
            button.interactable = false;
    }

    public void Clear()
    {
        data = null;
        SetPreview(null);
    }

    void SetPreview(Texture2D texture)
    {
        ReleasePreview();
        previewTexture = texture;

        if (previewImage == null && previewRawImage == null)
            return;

        if (previewTexture == null)
        {
            if (previewImage != null)
            {
                previewImage.sprite = null;
                previewImage.enabled = false;
            }

            if (previewRawImage != null)
            {
                previewRawImage.texture = null;
                previewRawImage.enabled = false;
            }

            return;
        }

        if (previewImage != null)
        {
            previewSprite = Sprite.Create(
                previewTexture,
                new Rect(0f, 0f, previewTexture.width, previewTexture.height),
                new Vector2(0.5f, 0.5f)
            );
            previewImage.sprite = previewSprite;
            previewImage.preserveAspect = true;
            previewImage.enabled = true;
            previewImage.gameObject.SetActive(true);

            Color color = previewImage.color;
            if (color.a <= 0f)
                color.a = 1f;
            previewImage.color = color;

            EnsureVisibleRect(previewImage.rectTransform);
        }

        if (previewRawImage != null)
        {
            previewRawImage.texture = previewTexture;
            previewRawImage.enabled = true;
            previewRawImage.gameObject.SetActive(true);
            EnsureVisibleRect(previewRawImage.rectTransform);
        }
    }

    void EnsureVisibleRect(RectTransform rect)
    {
        if (rect == null)
            return;

        Vector2 size = rect.rect.size;
        if (size.x <= 0.01f || size.y <= 0.01f)
        {
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, defaultPreviewSize.x);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, defaultPreviewSize.y);
        }

        rect.localScale = Vector3.one;
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
