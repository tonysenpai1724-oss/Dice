using UnityEngine;

public class PoisonEffect : GameEffect
{
    public int turnsRemaining;
    public int damagePerTurn;

    GameUnit unit;
    bool ticking;

    public override void Initialize(EffectManager effectManager, object owner)
    {
        base.Initialize(effectManager, owner);
        unit = owner as GameUnit;

        if (unit != null)
        {
            unit.TurnStarted += OnTurnStarted;
            unit.Died += OnUnitDied;
        }
    }

    public override void Dispose()
    {
        if (unit != null)
        {
            unit.TurnStarted -= OnTurnStarted;
            unit.Died -= OnUnitDied;
        }

        unit = null;
        base.Dispose();
    }

    public void Apply(int turns, int damage)
    {
        if (turns <= 0 || damage <= 0)
            return;

        turnsRemaining += turns;
        damagePerTurn = Mathf.Max(damagePerTurn, damage);
    }

    void OnTurnStarted(GameUnit target)
    {
        if (target != unit || turnsRemaining <= 0 || damagePerTurn <= 0 || ticking)
            return;

        ticking = true;
        unit.TakeDamage(damagePerTurn);
        ticking = false;

        turnsRemaining--;

        if (turnsRemaining <= 0)
            RemoveSelf();
    }

    void OnUnitDied(GameUnit target)
    {
        if (target == unit)
            RemoveSelf();
    }
}
