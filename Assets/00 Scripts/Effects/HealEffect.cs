public class HealEffect : GameEffect
{
    public override EffectType EffectType => EffectType.Neutral;
    GameUnit unit;

    protected override void Awake()
    {
        unit = GetComponent<GameUnit>();
    }

    public void Apply(int amount)
    {
        if (unit != null)
            unit.OnHeal(amount);
    }
}

