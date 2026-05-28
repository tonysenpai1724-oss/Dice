using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

public class DiceQueueItem : MonoBehaviour
{
    public MeshRenderer meshRenderer;


    public List<DecalProjector> decals = new();
    public List<DecalProjector> decals2 = new();
    public DiceData data;

    public void SetDice(DiceData data)
    {
        this.data = data;
        meshRenderer.material = data.diceMaterial;
        foreach (var d in decals)
        {
            d.material = data.decalMaterial[0];
        }
        if (data.decalMaterial.Count > 1)
        {
            foreach (var d in decals2)
            {
                d.gameObject.SetActive(true);
                d.material = data.decalMaterial[1];
            }
        }
        else
        {
            foreach (var d in decals2)
            {
                d.gameObject.SetActive(false);
            }
        }


    }

}