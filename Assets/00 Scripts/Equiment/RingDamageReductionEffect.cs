using UnityEngine;

[CreateAssetMenu(menuName = "RuneDice/Equipment/Effects/Ring Damage Reduction")]
public class RingDamageReductionEffect : BaseEquipmentEffect
{
    [Range(0f, 100f)] public float damageTakenReductionPercent = 3f;

    public override void OnEquip(PlayerController player, BaseEquiment equipment)
    {
        RingDamageReductionRuntime runtime = GetOrAddRuntimeEffect<RingDamageReductionRuntime>(player, equipment);
        if (runtime != null)
            runtime.SetReductionPercent(damageTakenReductionPercent);
    }

    public override void OnUnequip(PlayerController player, BaseEquiment equipment)
    {
        if (player == null)
            return;

        RingDamageReductionRuntime runtime = player.GetComponent<RingDamageReductionRuntime>();
        if (runtime != null && runtime.SourceEffect == this)
            runtime.ClearReduction();
    }
}

public class RingDamageReductionRuntime : EquipmentRuntimeEffect
{
    GameUnit unit;
    float reductionPercent;

    public override void Initialize(EquipmentEffectManager effectManager, PlayerController player, BaseEquiment equipment, BaseEquipmentEffect sourceEffect)
    {
        base.Initialize(effectManager, player, equipment, sourceEffect);

        if (unit == null)
        {
            unit = GetComponent<GameUnit>();
            if (unit != null)
                unit.OnBeforeDamage += OnBeforeDamage;
        }
    }

    protected override void OnDestroy()
    {
        if (unit != null)
            unit.OnBeforeDamage -= OnBeforeDamage;

        base.OnDestroy();
    }

    public void SetReductionPercent(float percent)
    {
        reductionPercent = Mathf.Clamp(percent, 0f, 100f);
    }

    public void ClearReduction()
    {
        reductionPercent = 0f;
        RemoveSelf();
    }

    void OnBeforeDamage(GameUnitDamageEvent damageEvent)
    {
        if (unit == null || damageEvent == null || damageEvent.Target != unit || reductionPercent <= 0f)
            return;

        int reducedAmount = Mathf.FloorToInt(damageEvent.Amount * reductionPercent * 0.01f);
        if (reducedAmount <= 0)
            return;

        damageEvent.Amount = Mathf.Max(0, damageEvent.Amount - reducedAmount);
    }
}
