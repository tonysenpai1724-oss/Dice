using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour
{
    public DiceData dice;

    public MeshRenderer meshRenderer;
    public List<DecalProjector> decals = new();
    public List<DecalProjector> decals2 = new();
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

    void OnDestroy()
    {
        ReleasePreview();
    }

    void ApplyDecalMaterials()
    {
        Material firstDecal = dice.decalMaterial != null && dice.decalMaterial.Count > 0
            ? dice.decalMaterial[0]
            : null;

        for (int i = 0; i < decals.Count; i++)
        {
            DecalProjector decal = decals[i];
            if (decal == null)
                continue;

            decal.gameObject.SetActive(firstDecal != null);
            if (firstDecal != null)
                decal.material = firstDecal;
        }

        Material secondDecal = dice.decalMaterial != null && dice.decalMaterial.Count > 1
            ? dice.decalMaterial[1]
            : null;

        for (int i = 0; i < decals2.Count; i++)
        {
            DecalProjector decal = decals2[i];
            if (decal == null)
                continue;

            decal.gameObject.SetActive(secondDecal != null);
            if (secondDecal != null)
                decal.material = secondDecal;
        }
    }
}
