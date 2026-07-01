using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

public class DiceQueueItem : PoolingObject
{
    public MeshRenderer meshRenderer;
    public Image previewImage;

    public List<DecalProjector> decals = new();
    public List<DecalProjector> decals2 = new();
    public DiceData data;

    Texture2D ownedPreviewTexture;
    Sprite ownedPreviewSprite;

    public void SetDice(DiceData data)
    {
        SetDice(data, null);
    }

    public void SetDice(DiceData data, Texture2D previewTexture)
    {
        if (data == null)
            return;

        this.data = data;
        ApplyPreview(previewTexture);
        ApplyMeshVisual(data, previewTexture == null);
    }

    void ApplyMeshVisual(DiceData data, bool visible)
    {
        if (meshRenderer != null)
        {
            meshRenderer.enabled = visible;
            if (visible)
                meshRenderer.material = data.diceMaterial;
        }

        for (int i = 0; i < decals.Count; i++)
        {
            DecalProjector decal = decals[i];
            if (decal == null)
                continue;

            decal.enabled = visible;
            if (visible && data.decalMaterial != null && data.decalMaterial.Count > 0)
                decal.material = data.decalMaterial[0];
        }

        bool hasSecondDecal = visible && data.decalMaterial != null && data.decalMaterial.Count > 1;
        for (int i = 0; i < decals2.Count; i++)
        {
            DecalProjector decal = decals2[i];
            if (decal == null)
                continue;

            decal.gameObject.SetActive(hasSecondDecal);
            if (hasSecondDecal)
                decal.material = data.decalMaterial[1];
        }
    }

    void ApplyPreview(Texture2D previewTexture)
    {
        ReleaseOwnedPreview();

        if (previewImage == null)
            return;

        if (previewTexture == null)
        {
            previewImage.enabled = false;
            previewImage.sprite = null;
            return;
        }

        ownedPreviewTexture = previewTexture;
        ownedPreviewSprite = Sprite.Create(
            ownedPreviewTexture,
            new Rect(0f, 0f, ownedPreviewTexture.width, ownedPreviewTexture.height),
            new Vector2(0.5f, 0.5f)
        );

        previewImage.sprite = ownedPreviewSprite;
        previewImage.preserveAspect = true;
        previewImage.enabled = true;
    }

    void ReleaseOwnedPreview()
    {
        if (ownedPreviewSprite != null)
            Destroy(ownedPreviewSprite);

        if (ownedPreviewTexture != null)
            Destroy(ownedPreviewTexture);

        ownedPreviewSprite = null;
        ownedPreviewTexture = null;
    }

    void OnDisable()
    {
        ReleaseOwnedPreview();

        if (previewImage != null)
        {
            previewImage.sprite = null;
            previewImage.enabled = false;
        }
    }
}
