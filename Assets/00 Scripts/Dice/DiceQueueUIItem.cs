using UnityEngine;
using UnityEngine.UI;

public class DiceQueueUIItem : MonoBehaviour
{
    public DiceData data;
    public Button button;
    public RawImage previewRawImage;
    public Image previewImage;

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
