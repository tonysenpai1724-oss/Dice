using System;
using UnityEngine;

public abstract class GameEffect : MonoBehaviour
{
    public EffectManager EffectManager { get; private set; }
    public GameUnit Unit { get; private set; }
    public int Stacks;

    public event Action<GameEffect, int> StacksChanged;

    public bool IsActiveEffect => EffectManager != null && EffectManager.Contains(this);

    protected virtual void Awake()
    {
        Unit = GetComponent<GameUnit>();
    }

    public virtual void Initialize(EffectManager effectManager)
    {
        EffectManager = effectManager;

        if (Unit == null)
            Unit = GetComponent<GameUnit>();
    }

    public virtual void AddStacks(int amount)
    {
        if (amount <= 0)
            return;

        Stacks += amount;
        Debug.Log("Stacks: " + Stacks);
        NotifyStacksChanged();
    }

    protected bool ConsumeStack()
    {
        if (Stacks <= 0)
            return false;

        Stacks--;
        Debug.Log("Stacks: " + Stacks);
        NotifyStacksChanged();

        if (Stacks <= 0)
            RemoveSelf();

        return true;
    }

    protected virtual void OnDestroy()
    {
        Unit = null;

        if (EffectManager != null)
            EffectManager.UnregisterEffect(this);
    }

    protected void RemoveSelf()
    {
        Destroy(this);
    }

    protected void NotifyStacksChanged()
    {
        StacksChanged?.Invoke(this, Stacks);
    }
}
