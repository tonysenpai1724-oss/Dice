using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "RuneDice/Equipment/Effects/Ring Crit Damage Bonus")]
public class RingCritDamageBonusEffect : BaseEquipmentEffect
{
    [Range(-100f, 500f)] public float critDamageBonusPercent = 20f;

    public override void CollectModifiers(PlayerController player, BaseEquiment equipment, List<HeroStatModifier> modifiers)
    {
        if (equipment == null || modifiers == null || equipment.equipmentType != EquipmentType.Ring)
            return;

        modifiers.Add(new HeroStatModifier
        {
            sourceId = $"ring-crit-dmg:{equipment.name}:{name}",
            statType = HeroStatType.CritDamage,
            mode = HeroStatModifierMode.Flat,
            amount = critDamageBonusPercent * 0.01f
        });
    }
}
