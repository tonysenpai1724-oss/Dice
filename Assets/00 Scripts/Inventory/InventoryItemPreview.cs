using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class InventoryItemPreview : MonoBehaviour
{
    public DiceData dice;

    public MeshRenderer meshRenderer;
    //public List<DecalProjector> decals = new();
    //  public List<DecalProjector> decals2 = new();
    public List<MeshRenderer> decalMeshes = new();
    public List<MeshRenderer> decalMeshes2 = new();
    public List<MeshRenderer> decalMeshes3 = new();
    //  public List<MeshRenderer> decalMeshes3 = new();
    public bool preferMeshDecals = true;
    public Image itemPreview;

    Texture2D previewTexture;
    Sprite previewSprite;

    public void Setup(DiceData diceData)
    {
        dice = diceData;

        if (dice == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        if (meshRenderer != null && dice.diceMaterial != null)
            meshRenderer.material = dice.diceMaterial;

        ApplyDecalMaterials();
    }

    public void PrepareForCapture()
    {
        if (itemPreview != null)
        {
            itemPreview.sprite = null;
            itemPreview.enabled = false;
        }

        if (preferMeshDecals)
            DisableDecalProjectors();
    }

    public void Setup(DiceData diceData, Texture2D texture)
    {
        Setup(diceData);
        SetPreview(texture);
    }

    public void SetPreview(Texture2D texture)
    {
        ReleasePreview();
        previewTexture = texture;

        if (itemPreview == null)
            return;

        if (previewTexture == null)
        {
            itemPreview.sprite = null;
            itemPreview.enabled = false;
            return;
        }

        previewSprite = Sprite.Create(
            previewTexture,
            new Rect(0f, 0f, previewTexture.width, previewTexture.height),
            new Vector2(0.5f, 0.5f)
        );
        itemPreview.sprite = previewSprite;
        itemPreview.preserveAspect = true;
        itemPreview.enabled = true;
    }

    void ReleasePreview()
    {
        if (previewSprite != null)
            Destroy(previewSprite);

        if (previewTexture != null)
            Destroy(previewTexture);

        previewSprite = null;
        previewTexture = null;
    }

    void ApplyMeshDecalGroup(List<MeshRenderer> renderers, Material material)
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            MeshRenderer decalMesh = renderers[i];
            if (decalMesh == null)
                continue;

            bool enabled = preferMeshDecals && renderers.Count > 0 && material != null;
            decalMesh.gameObject.SetActive(enabled);
            decalMesh.enabled = enabled;
            if (material != null)
                decalMesh.sharedMaterial = material;
        }
    }

    void DisableDecalProjectors()
    {
        DecalProjector[] projectors = GetComponentsInChildren<DecalProjector>(true);
        for (int i = 0; i < projectors.Length; i++)
        {
            DecalProjector projector = projectors[i];
            if (projector == null)
                continue;

            projector.enabled = false;
            if (projector.gameObject.activeSelf)
                projector.gameObject.SetActive(false);
        }
    }

    void OnDestroy()
    {
        ReleasePreview();
    }

    void ApplyDecalMaterials()
    {
        Material primaryDecalMaterial = dice.decalMaterial.Count > 0
         ? dice.decalMaterial[0]
         : null;
        Material secondaryDecalMaterial = dice.decalMaterial.Count > 1
            ? dice.decalMaterial[1]
            : null;
        int decalCount = dice.decalMaterial.Count;

        bool useProjectorPrimary = !preferMeshDecals || decalMeshes.Count == 0;
        // foreach (var d in decals)
        // {
        //     if (d == null)
        //         continue;

        //     bool enabled = useProjectorPrimary && primaryDecalMaterial != null;
        //     d.gameObject.SetActive(enabled);
        //     d.enabled = enabled;
        //     if (primaryDecalMaterial != null)
        //         d.material = primaryDecalMaterial;
        // }

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
        // foreach (var d in decals2)
        // {
        //     if (d == null)
        //         continue;

        //     bool enabled = useProjectorSecondary && secondaryDecalMaterial != null && decalCount == 2;
        //     d.gameObject.SetActive(enabled);
        //     d.enabled = enabled;
        //     if (secondaryDecalMaterial != null)
        //         d.material = secondaryDecalMaterial;
        // }

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
}

