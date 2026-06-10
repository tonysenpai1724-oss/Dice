using UnityEngine;

public class DamageReductionEffect : GameEffect
{
    public int reductionAmount;

    GameUnit unit;

    protected override void Awake()
    {
        unit = GetComponent<GameUnit>();

        if (unit != null)
            unit.OnBeforeDamage += OnBeforeDamage;
    }

    protected override void OnDestroy()
    {
        if (unit != null)
            unit.OnBeforeDamage -= OnBeforeDamage;

        base.OnDestroy();
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
