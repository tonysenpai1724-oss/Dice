using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "RuneDice/Equipment/Effects/Ring Base Stats Percent Bonus")]
public class RingBaseStatsPercentEffect : BaseEquipmentEffect
{
    [Range(-100f, 500f)] public float bonusPercent = 20f;

    public override void CollectModifiers(PlayerController player, BaseEquiment equipment, List<HeroStatModifier> modifiers)
    {
        if (equipment == null || modifiers == null || equipment.equipmentType != EquipmentType.Ring)
            return;

        if (equipment.statBonuses == null || equipment.statBonuses.Count == 0)
            return;

        string sourceId = $"ring-base-bonus:{equipment.name}:{name}";

        for (int i = 0; i < equipment.statBonuses.Count; i++)
        {
            EquipmentStatBonus statBonus = equipment.statBonuses[i];
            if (statBonus == null)
                continue;

            modifiers.Add(new HeroStatModifier
            {
                sourceId = sourceId,
                statType = statBonus.statType,
                mode = HeroStatModifierMode.Flat,
                amount = Mathf.Abs(statBonus.amount) * bonusPercent * 0.01f
            });
        }
    }
}
