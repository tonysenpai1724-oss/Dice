public class HealEffect : GameEffect
{
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
