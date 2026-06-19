using UnityEngine;

[CreateAssetMenu(menuName = "RuneDice/Equipment/Effects/Ring Crit Damage Bonus")]
public class RingCritDamageBonusEffect : BaseEquipmentEffect
{
    [Range(-100f, 500f)] public float critDamageBonusPercent = 20f;
}
