public class DodgeEffect : UnitStackEffect
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

        if (ConsumeStack())
            damageEvent.Cancel();
    }
}
