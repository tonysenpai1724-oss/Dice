public class BurnEffect : GameEffect
{
    public override EffectType EffectType => EffectType.Debuff;

    public int damagePerTurn;

    public void Apply(int turns, int damage)
    {
        damagePerTurn = damage;
        AddStacks(turns);
    }

    protected override void Awake()
    {
        base.Awake();

        if (Unit != null)
            Unit.OnTurnStarted += OnTurnStarted;
    }

    protected override void OnDestroy()
    {
        if (Unit != null)
            Unit.OnTurnStarted -= OnTurnStarted;

        base.OnDestroy();
    }

    void OnTurnStarted(GameUnit unit)
    {
        if (unit == null || damagePerTurn <= 0)
            return;

        unit.OnTakeDamage(damagePerTurn);
        ConsumeStack();
    }
}
