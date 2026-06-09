using System.Collections.Generic;

public sealed class EffectManager : Singleton<EffectManager>
{

    readonly List<GameEffect> currentEffects = new();

    public IReadOnlyList<GameEffect> CurrentEffects => currentEffects;

    EffectManager()
    {
    }

    public T GetEffect<T>(object owner) where T : GameEffect
    {
        for (int i = 0; i < currentEffects.Count; i++)
        {
            if (currentEffects[i] is T effect && ReferenceEquals(effect.Owner, owner))
                return effect;
        }

        return null;
    }

    public T AddEffect<T>(object owner) where T : GameEffect, new()
    {
        if (owner == null)
            return null;

        T effect = GetEffect<T>(owner);
        if (effect != null)
            return effect;

        effect = new T();
        currentEffects.Add(effect);
        effect.Initialize(this, owner);
        return effect;
    }

    public bool Contains(GameEffect effect)
    {
        return effect != null && currentEffects.Contains(effect);
    }

    public void RemoveEffect(GameEffect effect)
    {
        if (effect == null)
            return;

        if (!currentEffects.Remove(effect))
            return;

        effect.Dispose();
    }

    public void RemoveEffects(object owner)
    {
        if (owner == null)
            return;

        for (int i = currentEffects.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(currentEffects[i].Owner, owner))
                RemoveEffect(currentEffects[i]);
        }
    }
}
