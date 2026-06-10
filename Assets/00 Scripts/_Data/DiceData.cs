using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "RuneDice/Dice")]
public class DiceData : SerializedScriptableObject
{
    public string diceName;

    public int level;

    public int damage;

    //public Mesh mesh;

    public Material diceMaterial;

    public List<Material> decalMaterial;
    public Sprite diceSprite;
    public Color baseOutlineColor;
    public Color targetColor;
    public GameObject hitEffectPrefab;
    public DiceType type;
    [Header("Skill")]
    public DiceSkillData skillData;

    public void ExecuteSkill()
    {
        DiceSkillData resolvedSkill =
            skillData != null
                ? skillData
                : DiceSkillFactory.Create(type);

        if (resolvedSkill == null)
            return;

        resolvedSkill.Execute();
    }

    void OnValidate()
    {
        if (skillData != null &&
            skillData.TargetType != type)
        {
            Debug.LogWarning(
                $"{name}: skill {skillData.name} targets {skillData.TargetType} but dice type is {type}",
                this
            );
        }
    }
}
