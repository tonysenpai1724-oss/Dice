using UnityEngine;

public class DamageReductionEffect : GameEffect
{
    public int reductionAmount;

    GameUnit unit;

    public override void Initialize(EffectManager effectManager, object owner)
    {
        base.Initialize(effectManager, owner);
        unit = owner as GameUnit;

        if (unit != null)
            unit.BeforeDamage += OnBeforeDamage;
    }

    public override void Dispose()
    {
        if (unit != null)
            unit.BeforeDamage -= OnBeforeDamage;

        unit = null;
        base.Dispose();
    }

    public void AddReduction(int amount)
    {
        if (amount <= 0)
            return;

        reductionAmount += amount;
    }

    void OnBeforeDamage(GameUnitDamageEvent damageEvent)
    {
        if (damageEvent == null || damageEvent.Target != unit || reductionAmount <= 0)
            return;

        int usedReduction = Mathf.Min(reductionAmount, damageEvent.Amount);
        damageEvent.Amount = Mathf.Max(0, damageEvent.Amount - usedReduction);
        reductionAmount -= usedReduction;

        if (reductionAmount <= 0)
            RemoveSelf();
    }
}
