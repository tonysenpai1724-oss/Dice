public class EnemyTurnSkipEffect : GameEffect
{
    public override EffectType EffectType => EffectType.Debuff;

    public int turnsRemaining;
    public void AddTurns(int amount)
    {
        if (amount <= 0)
            return;

        AddStacks(amount);
        turnsRemaining = Stacks;
    }

    public bool ConsumeTurnSkip()
    {
        if (Stacks <= 0 && turnsRemaining > 0)
            Stacks = turnsRemaining;

        if (!ConsumeStack())
            return false;

        turnsRemaining = Stacks;
        return true;
    }
}
