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
    public List<MeshRenderer> decalMeshes = new();
    public List<MeshRenderer> decalMeshes2 = new();
    public List<MeshRenderer> decalMeshes3 = new();
    public bool preferMeshDecals = true;
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

        Material primaryDecalMaterial = data.decalMaterial.Count > 0
         ? data.decalMaterial[0]
         : null;
        Material secondaryDecalMaterial = data.decalMaterial.Count > 1
            ? data.decalMaterial[1]
            : null;
        int decalCount = data.decalMaterial.Count;

        bool useProjectorPrimary = !preferMeshDecals || decalMeshes.Count == 0;
        foreach (var d in decals)
        {
            if (d == null)
                continue;

            bool enabled = useProjectorPrimary && primaryDecalMaterial != null;
            d.gameObject.SetActive(enabled);
            d.enabled = enabled;
            if (primaryDecalMaterial != null)
                d.material = primaryDecalMaterial;
        }

        foreach (var d in decalMeshes)
        {
            if (d == null)
                continue;

            bool enabled = !useProjectorPrimary && primaryDecalMaterial != null && decalCount == 1;
            d.gameObject.SetActive(enabled);
            d.enabled = enabled;
            if (primaryDecalMaterial != null)
                d.sharedMaterial = primaryDecalMaterial;
        }

        bool useProjectorSecondary = !preferMeshDecals || decalMeshes2.Count == 0;
        foreach (var d in decals2)
        {
            if (d == null)
                continue;

            bool enabled = useProjectorSecondary && secondaryDecalMaterial != null && decalCount == 2;
            d.gameObject.SetActive(enabled);
            d.enabled = enabled;
            if (secondaryDecalMaterial != null)
                d.material = secondaryDecalMaterial;
        }

        foreach (var d in decalMeshes2)
        {
            if (d == null)
                continue;

            bool enabled = !useProjectorSecondary && primaryDecalMaterial != null && decalCount == 2;
            d.gameObject.SetActive(enabled);
            d.enabled = enabled;
            if (primaryDecalMaterial != null)
                d.sharedMaterial = primaryDecalMaterial;
        }
        foreach (var d in decalMeshes3)
        {
            if (d == null)
                continue;

            bool enabled = !useProjectorSecondary && secondaryDecalMaterial != null && decalCount == 2;
            d.gameObject.SetActive(enabled);
            d.enabled = enabled;
            if (secondaryDecalMaterial != null)
                d.sharedMaterial = secondaryDecalMaterial;
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


