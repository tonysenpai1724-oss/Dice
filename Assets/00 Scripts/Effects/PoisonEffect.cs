using UnityEngine;

public class PoisonEffect : GameEffect
{
    public int turnsRemaining;
    public int damagePerTurn;

    GameUnit unit;
    bool ticking;

    protected override void Awake()
    {
        unit = GetComponent<GameUnit>();

        if (unit != null)
        {
            unit.TurnStarted += OnTurnStarted;
            unit.Died += OnUnitDied;
        }
    }

    protected override void OnDestroy()
    {
        if (unit != null)
        {
            unit.TurnStarted -= OnTurnStarted;
            unit.Died -= OnUnitDied;
        }

        base.OnDestroy();
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
