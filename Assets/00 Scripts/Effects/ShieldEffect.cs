public class ShieldEffect : UnitStackEffect
{
    protected override void OnInitialized()
    {
        if (Unit != null)
            Unit.BeforeDamage += OnBeforeDamage;
    }

    protected override void OnDisposed()
    {
        if (Unit != null)
            Unit.BeforeDamage -= OnBeforeDamage;
    }

    void OnBeforeDamage(GameUnitDamageEvent damageEvent)
    {
        if (damageEvent == null || damageEvent.Target != Unit)
            return;

        DodgeEffect dodgeEffect = Unit != null ? Unit.effectManager?.GetEffect<DodgeEffect>(Unit) : null;
        if (dodgeEffect != null && dodgeEffect.Stacks > 0)
            return;

        if (ConsumeStack())
            damageEvent.Cancel();
    }
}
