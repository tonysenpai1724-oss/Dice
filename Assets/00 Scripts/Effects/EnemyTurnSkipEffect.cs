public class EnemyTurnSkipEffect : GameEffect
{
    public int turnsRemaining;

    public void AddTurns(int amount)
    {
        if (amount <= 0)
            return;

        turnsRemaining += amount;
    }

    public bool ConsumeTurnSkip()
    {
        if (turnsRemaining <= 0)
            return false;

        turnsRemaining--;

        if (turnsRemaining <= 0)
            RemoveSelf();

        return true;
    }
}
