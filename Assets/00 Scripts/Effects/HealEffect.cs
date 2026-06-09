public class HealEffect : GameEffect
{
    GameUnit unit;

    public override void Initialize(EffectManager effectManager, object owner)
    {
        base.Initialize(effectManager, owner);
        unit = owner as GameUnit;
    }

    public override void Dispose()
    {
        unit = null;
        base.Dispose();
    }

    public void Apply(int amount)
    {
        if (unit != null)
            unit.Heal(amount);
    }
}
