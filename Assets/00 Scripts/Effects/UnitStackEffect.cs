using System;

public abstract class UnitStackEffect : GameEffect
{
    public int Stacks { get; private set; }

    public event Action<UnitStackEffect, int> StacksChanged;
    protected GameUnit Unit { get; private set; }

    public override void Initialize(EffectManager effectManager, object owner)
    {
        base.Initialize(effectManager, owner);
        Unit = owner as GameUnit;
        OnInitialized();
    }

    public override void Dispose()
    {
        OnDisposed();
        Unit = null;
        base.Dispose();
    }

    protected virtual void OnInitialized()
    {
    }

    protected virtual void OnDisposed()
    {
    }

    public virtual void AddStacks(int amount)
    {
        if (amount <= 0)
            return;

        Stacks += amount;
        NotifyStacksChanged();
    }

    protected bool ConsumeStack()
    {
        if (Stacks <= 0)
            return false;

        Stacks--;
        NotifyStacksChanged();
        return true;
    }

    protected void NotifyStacksChanged()
    {
        StacksChanged?.Invoke(this, Stacks);
    }
}
