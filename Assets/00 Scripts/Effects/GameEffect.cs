public abstract class GameEffect
{
    public EffectManager EffectManager { get; private set; }
    public object Owner { get; private set; }

    public bool IsActiveEffect => EffectManager != null && EffectManager.Contains(this);

    public virtual void Initialize(EffectManager effectManager, object owner)
    {
        EffectManager = effectManager;
        Owner = owner;
    }

    public virtual void Dispose()
    {
        EffectManager = null;
        Owner = null;
    }

    protected void RemoveSelf()
    {
        EffectManager?.RemoveEffect(this);
    }
}
