using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;

[CreateAssetMenu(menuName = "RuneDice/Dice")]
public class DiceData : ScriptableObject
{
    public string diceName;

    public int level;

    public int damage;

    //public Mesh mesh;

    public Material diceMaterial;

    public List<Material> decalMaterial;
    public Sprite diceSprite;
}