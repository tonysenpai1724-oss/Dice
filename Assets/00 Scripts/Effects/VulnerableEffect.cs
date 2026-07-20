using UnityEngine;

public class VulnerableEffect : GameEffect
{
    public override EffectType EffectType => EffectType.Debuff;

    public int percentPerStack = 10;
    protected override void Awake()
    {
        base.Awake();

        if (Unit != null)
            Unit.OnBeforeDamage += OnBeforeDamage;
    }

    protected override void OnDestroy()
    {
        if (Unit != null)
            Unit.OnBeforeDamage -= OnBeforeDamage;

        base.OnDestroy();
    }

    public void AddPercentStacks(int stacks, int percent)
    {
        percentPerStack = Mathf.Max(0, percent);
        AddStacks(stacks);
    }

    void OnBeforeDamage(GameUnitDamageEvent damageEvent)
    {
        if (damageEvent == null || damageEvent.Target != Unit || Stacks <= 0 || percentPerStack <= 0)
            return;

        int bonusDamage = Mathf.RoundToInt(damageEvent.Amount * ((percentPerStack * Stacks) / 100f));
        damageEvent.Amount = Mathf.Max(0, damageEvent.Amount + bonusDamage);
    }
}
