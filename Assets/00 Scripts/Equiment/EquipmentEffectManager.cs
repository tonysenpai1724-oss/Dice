using System.Collections.Generic;
using UnityEngine;

public abstract class EquipmentRuntimeEffect : MonoBehaviour
{
    public EquipmentEffectManager EffectManager { get; private set; }
    public PlayerController Player { get; private set; }
    public BaseEquiment Equipment { get; private set; }
    public BaseEquipmentEffect SourceEffect { get; private set; }

    public virtual void Initialize(
        EquipmentEffectManager effectManager,
        PlayerController player,
        BaseEquiment equipment,
        BaseEquipmentEffect sourceEffect)
    {
        EffectManager = effectManager;
        Player = player;
        Equipment = equipment;
        SourceEffect = sourceEffect;
    }

    protected virtual void OnDestroy()
    {
        if (EffectManager != null)
            EffectManager.UnregisterEffect(this);
    }

    protected void RemoveSelf()
    {
        Destroy(this);
    }
}

public class EquipmentEffectManager : MonoBehaviour
{
    [SerializeField] List<EquipmentRuntimeEffect> currentEffects = new();

    public IReadOnlyList<EquipmentRuntimeEffect> CurrentEffects => currentEffects;

    void Awake()
    {
        InitializeExistingEffects();
    }

    public T GetEffect<T>() where T : EquipmentRuntimeEffect
    {
        for (int i = 0; i < currentEffects.Count; i++)
        {
            if (currentEffects[i] is T effect)
                return effect;
        }

        return null;
    }

    public T GetEffect<T>(BaseEquipmentEffect sourceEffect) where T : EquipmentRuntimeEffect
    {
        for (int i = 0; i < currentEffects.Count; i++)
        {
            if (currentEffects[i] is not T effect)
                continue;

            if (effect.SourceEffect == sourceEffect)
                return effect;
        }

        return null;
    }

    public T AddEffect<T>(PlayerController player, BaseEquiment equipment, BaseEquipmentEffect sourceEffect)
        where T : EquipmentRuntimeEffect
    {
        T effect = GetEffect<T>(sourceEffect);
        if (effect != null)
            return effect;

        effect = gameObject.AddComponent<T>();
        RegisterEffect(effect, player, equipment, sourceEffect);
        return effect;
    }

    public bool Contains(EquipmentRuntimeEffect effect)
    {
        return effect != null && currentEffects.Contains(effect);
    }

    public void RegisterEffect(
        EquipmentRuntimeEffect effect,
        PlayerController player,
        BaseEquiment equipment,
        BaseEquipmentEffect sourceEffect)
    {
        if (effect == null)
            return;

        if (!currentEffects.Contains(effect))
            currentEffects.Add(effect);

        effect.Initialize(this, player, equipment, sourceEffect);
    }

    public void UnregisterEffect(EquipmentRuntimeEffect effect)
    {
        if (effect == null)
            return;

        currentEffects.Remove(effect);
    }

    void InitializeExistingEffects()
    {
        EquipmentRuntimeEffect[] effects = GetComponents<EquipmentRuntimeEffect>();
        for (int i = 0; i < effects.Length; i++)
        {
            if (!currentEffects.Contains(effects[i]))
                currentEffects.Add(effects[i]);
        }
    }
}
