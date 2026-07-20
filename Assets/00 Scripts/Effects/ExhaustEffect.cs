using UnityEngine;

public class ExhaustEffect : GameEffect
{
    public override EffectType EffectType => EffectType.Debuff;

    public int percentPerStack = 10;
    public void AddPercentStacks(int stacks, int percent)
    {
        percentPerStack = Mathf.Max(0, percent);
        AddStacks(stacks);
    }

    public int ApplyToDamage(int damage)
    {
        if (damage <= 0 || Stacks <= 0 || percentPerStack <= 0)
            return Mathf.Max(0, damage);

        int reductionPercent = Mathf.Clamp(percentPerStack * Stacks, 0, 100);
        return Mathf.RoundToInt(damage * ((100 - reductionPercent) / 100f));
    }
}
