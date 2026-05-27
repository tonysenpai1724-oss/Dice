using UnityEngine;

[CreateAssetMenu(menuName = "RuneDice/Dice")]
public class DiceData : ScriptableObject
{
    public string diceName;

    public int level;

    public int damage;

    //public Mesh mesh;

    public Material diceMaterial;

    public Material decalMaterial;
}