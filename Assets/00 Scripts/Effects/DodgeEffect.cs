public class DodgeEffect : GameEffect
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

        if (ConsumeStack())
            damageEvent.Cancel();
    }
}
