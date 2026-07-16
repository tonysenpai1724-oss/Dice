using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum PlayerEffectIndicatorType
{
    QueuedAttackDamage,
    Dodge,
    Shield,
    DamageReduction
}

[Serializable]
public class PlayerEffectIndicatorConfig
{
    public PlayerEffectIndicatorType type;
    public Sprite icon;
    public bool hideWhenZero = true;
}

public class PlayerEffectIndicatorPanel : MonoBehaviour
{
    [Header("References")]
    public PlayerController player;
    public Transform holder;
    public PlayerEffectIndicatorItem itemPrefab;

    [Header("Indicators")]
    public List<PlayerEffectIndicatorConfig> indicators = new();

    [Header("Runtime")]
    public float refreshInterval = 0.1f;

    readonly Dictionary<PlayerEffectIndicatorType, PlayerEffectIndicatorItem> spawnedItems = new();
    float refreshTimer;

    void Reset()
    {
        holder = transform;
        EnsureDefaultIndicators();
    }

    void Awake()
    {
        if (holder == null)
            holder = transform;

        EnsureDefaultIndicators();
        SpawnItems();
    }

    void OnEnable()
    {
        Refresh(true);
    }

    void Update()
    {
        refreshTimer -= Time.unscaledDeltaTime;
        if (refreshTimer > 0f)
            return;

        refreshTimer = refreshInterval;
        Refresh(false);
    }

    void EnsureDefaultIndicators()
    {
        if (indicators != null && indicators.Count > 0)
            return;

        indicators = new List<PlayerEffectIndicatorConfig>
        {
            new PlayerEffectIndicatorConfig { type = PlayerEffectIndicatorType.QueuedAttackDamage },
            new PlayerEffectIndicatorConfig { type = PlayerEffectIndicatorType.Dodge },
            new PlayerEffectIndicatorConfig { type = PlayerEffectIndicatorType.Shield },
            new PlayerEffectIndicatorConfig { type = PlayerEffectIndicatorType.DamageReduction },
        };
    }

    void SpawnItems()
    {
        spawnedItems.Clear();

        for (int i = 0; i < indicators.Count; i++)
        {
            PlayerEffectIndicatorConfig config = indicators[i];
            if (config == null || spawnedItems.ContainsKey(config.type))
                continue;

            PlayerEffectIndicatorItem item = CreateItem(config.type.ToString());
            if (item == null)
                continue;

            item.Setup(config.icon, 0);
            spawnedItems[config.type] = item;
        }
    }

    PlayerEffectIndicatorItem CreateItem(string itemName)
    {
        if (itemPrefab != null)
            return Instantiate(itemPrefab, holder);

        GameObject itemObject = new GameObject(itemName, typeof(RectTransform));
        itemObject.transform.SetParent(holder, false);

        HorizontalLayoutGroup layoutGroup = itemObject.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.spacing = 6f;

        GameObject iconObject = new GameObject("Icon", typeof(RectTransform));
        iconObject.transform.SetParent(itemObject.transform, false);
        Image iconImage = iconObject.AddComponent<Image>();
        RectTransform iconRect = iconObject.transform as RectTransform;
        if (iconRect != null)
            iconRect.sizeDelta = new Vector2(32f, 32f);

        GameObject textObject = new GameObject("Text", typeof(RectTransform));
        textObject.transform.SetParent(itemObject.transform, false);
        TextMeshProUGUI valueText = textObject.AddComponent<TextMeshProUGUI>();
        valueText.alignment = TextAlignmentOptions.Center;
        valueText.fontSize = 24f;
        valueText.color = Color.white;
        valueText.raycastTarget = false;
        RectTransform textRect = textObject.transform as RectTransform;
        if (textRect != null)
            textRect.sizeDelta = new Vector2(60f, 32f);

        PlayerEffectIndicatorItem item = itemObject.AddComponent<PlayerEffectIndicatorItem>();
        item.icon = iconImage;
        item.valueText = valueText;
        return item;
    }

    void Refresh(bool force)
    {
        PlayerController targetPlayer = GetPlayer();

        for (int i = 0; i < indicators.Count; i++)
        {
            PlayerEffectIndicatorConfig config = indicators[i];
            if (config == null || !spawnedItems.TryGetValue(config.type, out PlayerEffectIndicatorItem item) || item == null)
                continue;

            int value = GetValue(config.type, targetPlayer);
            item.SetValue(value, !force);

            bool shouldShow = !config.hideWhenZero || value > 0;
            if (force || item.gameObject.activeSelf != shouldShow)
                item.gameObject.SetActive(shouldShow);
        }
    }

    PlayerController GetPlayer()
    {
        if (player != null)
            return player;

        if (EnemyManager.Instance != null && EnemyManager.Instance.player != null)
        {
            player = EnemyManager.Instance.player;
            return player;
        }

        player = FindFirstObjectByType<PlayerController>();
        return player;
    }

    int GetValue(PlayerEffectIndicatorType type, PlayerController targetPlayer)
    {
        switch (type)
        {
            case PlayerEffectIndicatorType.QueuedAttackDamage:
                return GetQueuedAttackDamage(targetPlayer);
            case PlayerEffectIndicatorType.Dodge:
                return GetEffectStacks<DodgeEffect>(targetPlayer);
            case PlayerEffectIndicatorType.Shield:
                return GetEffectStacks<ShieldEffect>(targetPlayer);
            case PlayerEffectIndicatorType.DamageReduction:
                return GetDamageReduction(targetPlayer);
            default:
                return 0;
        }
    }

    int GetQueuedAttackDamage(PlayerController targetPlayer)
    {
        if (DiceQueueUI.Instance != null)
            return DiceQueueUI.Instance.GetQueuedAttackDamage(targetPlayer);

        if (DiceQueueManager.Instance != null)
            return DiceQueueManager.Instance.GetQueuedAttackDamage(targetPlayer);

        return 0;
    }

    int GetEffectStacks<T>(PlayerController targetPlayer) where T : GameEffect
    {
        T effect = targetPlayer != null ? targetPlayer.effectManager?.GetEffect<T>() : null;
        return effect != null ? Mathf.Max(0, effect.Stacks) : 0;
    }

    int GetDamageReduction(PlayerController targetPlayer)
    {
        DamageReductionEffect effect = targetPlayer != null ? targetPlayer.effectManager?.GetEffect<DamageReductionEffect>() : null;
        return effect != null ? Mathf.Max(0, effect.reductionAmount) : 0;
    }
}
