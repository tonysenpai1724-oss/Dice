public class ShieldEffect : GameEffect
{
    public override EffectType EffectType => EffectType.Buff;

    public void SetStacks(int amount)
    {
        Stacks = amount > 0 ? amount : 0;
        NotifyStacksChanged();

        if (Stacks <= 0)
            RemoveSelf();
    }

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
