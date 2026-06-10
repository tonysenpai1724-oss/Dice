public class ShieldEffect : GameEffect
{
    protected override void Awake()
    {
        base.Awake();

        if (Unit != null)
            Unit.BeforeDamage += OnBeforeDamage;
    }

    protected override void OnDestroy()
    {
        if (Unit != null)
            Unit.BeforeDamage -= OnBeforeDamage;

        base.OnDestroy();
    }

    void OnBeforeDamage(GameUnitDamageEvent damageEvent)
    {
        if (damageEvent == null || damageEvent.Target != Unit)
            return;

        DodgeEffect dodgeEffect = Unit != null ? Unit.effectManager?.GetEffect<DodgeEffect>() : null;
        if (dodgeEffect != null && dodgeEffect.Stacks > 0)
            return;

        if (ConsumeStack())
            damageEvent.Cancel();
    }
}
