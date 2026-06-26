using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class InventoryItem : MonoBehaviour
{
    public DiceData dice;

    public MeshRenderer meshRenderer;
    public List<DecalProjector> decals = new();
    public List<DecalProjector> decals2 = new();

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
