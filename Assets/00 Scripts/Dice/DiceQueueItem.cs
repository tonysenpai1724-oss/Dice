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

        Material firstDecal = visible && data.decalMaterial != null && data.decalMaterial.Count > 0
            ? data.decalMaterial[0]
            : null;

        for (int i = 0; i < decals.Count; i++)
        {
            DecalProjector decal = decals[i];
            if (decal == null)
                continue;

            decal.gameObject.SetActive(firstDecal != null && decalMeshes.Count == 0);
            decal.enabled = firstDecal != null && decalMeshes.Count == 0;
            if (firstDecal != null)
                decal.material = firstDecal;
        }

        for (int i = 0; i < decalMeshes.Count; i++)
        {
            MeshRenderer decalMesh = decalMeshes[i];
            if (decalMesh == null)
                continue;

            decalMesh.gameObject.SetActive(firstDecal != null);
            decalMesh.enabled = firstDecal != null;
            if (firstDecal != null)
                decalMesh.sharedMaterial = firstDecal;
        }

        Material secondDecal = visible && data.decalMaterial != null && data.decalMaterial.Count > 1
            ? data.decalMaterial[1]
            : null;
        bool useProjectorSecond = !preferMeshDecals || decalMeshes2.Count == 0;

        for (int i = 0; i < decals2.Count; i++)
        {
            DecalProjector decal = decals2[i];
            if (decal == null)
                continue;

            decal.gameObject.SetActive(secondDecal != null && useProjectorSecond);
            decal.enabled = secondDecal != null && useProjectorSecond;
            if (secondDecal != null)
                decal.material = secondDecal;
        }

        for (int i = 0; i < decalMeshes2.Count; i++)
        {
            MeshRenderer decalMesh = decalMeshes2[i];
            if (decalMesh == null)
                continue;

            decalMesh.gameObject.SetActive(secondDecal != null);
            decalMesh.enabled = secondDecal != null;
            if (secondDecal != null)
                decalMesh.sharedMaterial = secondDecal;
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



