using System.Collections.Generic;
using UnityEngine;

public class EffectManager : MonoBehaviour
{
    [SerializeField] private List<GameEffect> currentEffects = new();

    public IReadOnlyList<GameEffect> CurrentEffects => currentEffects;

    void Awake()
    {
        InitializeExistingEffects();
    }

    public T GetEffect<T>() where T : GameEffect
    {
        for (int i = 0; i < currentEffects.Count; i++)
        {
            if (currentEffects[i] is T effect)
                return effect;
        }

        return null;
    }

    public T AddEffect<T>() where T : GameEffect
    {
        T effect = GetEffect<T>();
        if (effect != null)
            return effect;

        effect = gameObject.AddComponent<T>();
        RegisterEffect(effect);
        return effect;
    }

    public bool Contains(GameEffect effect)
    {
        return effect != null && currentEffects.Contains(effect);
    }

    public void RegisterEffect(GameEffect effect)
    {
        if (effect == null)
            return;

        if (!currentEffects.Contains(effect))
            currentEffects.Add(effect);

        effect.Initialize(this);
    }

    public void RemoveEffect<T>() where T : GameEffect
    {
        T effect = GetEffect<T>();
        if (effect != null)
            Destroy(effect);
    }

    public void RemoveEffectsByType(EffectType effectType)
    {
        for (int i = currentEffects.Count - 1; i >= 0; i--)
        {
            GameEffect effect = currentEffects[i];
            if (effect != null && effect.EffectType == effectType)
                Destroy(effect);
        }
    }

    public void UnregisterEffect(GameEffect effect)
    {
        if (effect == null)
            return;

        currentEffects.Remove(effect);
    }

    void InitializeExistingEffects()
    {
        GameEffect[] effects = GetComponents<GameEffect>();
        for (int i = 0; i < effects.Length; i++)
        {
            RegisterEffect(effects[i]);
        }
    }
}
