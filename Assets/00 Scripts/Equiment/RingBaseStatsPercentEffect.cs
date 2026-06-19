using UnityEngine;

[CreateAssetMenu(menuName = "RuneDice/Equipment/Effects/Ring Base Stats Percent Bonus")]
public class RingBaseStatsPercentEffect : BaseEquipmentEffect
{
    [Range(-100f, 500f)] public float bonusPercent = 20f;
}
