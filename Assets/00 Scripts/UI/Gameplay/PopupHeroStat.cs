using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class PopupHeroStat : UIBase
{
    [Header("Hero")]
    public HeroData heroData;

    [Header("Start Dice")]
    public ItemToggle itemTogglePrefab;
    [FormerlySerializedAs("parent")] public RectTransform diceParent;
    public InventoryItemPreview itemPreviewPrefab;
    public ItemPreviewGenerator previewGenerator;

    readonly List<ItemToggle> spawnedDiceToggles = new();

    void OnEnable()
    {
        RefreshStartDice();
    }

    public override void Show()
    {
        base.Show();
        RefreshStartDice();
    }

    public override void OnDisable()
    {
        ClearStartDice();
        base.OnDisable();
    }

    public void SetHero(HeroData newHeroData)
    {
        heroData = newHeroData;
        RefreshStartDice();
    }

    public void RefreshStartDice()
    {
        ClearStartDice();

        HeroData currentHeroData = ResolveHeroData();
        if (currentHeroData == null || currentHeroData.startDiceLevelConfig == null)
            return;

        if (!currentHeroData.startDiceLevelConfig.TryGetValue(currentHeroData.level, out List<DiceData> startDices))
            return;

        if (startDices == null || itemTogglePrefab == null)
            return;

        Transform targetParent = diceParent != null ? diceParent : transform;
        ItemPreviewGenerator generator = ResolvePreviewGenerator();

        for (int i = 0; i < startDices.Count; i++)
        {
            DiceData diceData = startDices[i];
            if (diceData == null)
                continue;

            Texture2D previewTexture = generator != null && itemPreviewPrefab != null
                ? generator.Capture(itemPreviewPrefab, diceData)
                : null;

            ItemToggle itemToggle = Instantiate(itemTogglePrefab, targetParent);
            itemToggle.Setup(diceData, previewTexture, null);
            spawnedDiceToggles.Add(itemToggle);
        }
    }

    HeroData ResolveHeroData()
    {
        return HeroDataResolver.Resolve(heroData);
    }

    ItemPreviewGenerator ResolvePreviewGenerator()
    {
        if (previewGenerator == null)
            previewGenerator = FindFirstObjectByType<ItemPreviewGenerator>();

        return previewGenerator;
    }

    void OnStartDiceSelected(ItemToggle itemToggle)
    {
        if (itemToggle == null || itemToggle.data == null || UIManager.Instance == null)
            return;

        UIManager.Instance.ShowPopupDiceDetail(itemToggle.data, itemToggle.PreviewSprite);
    }

    void ClearStartDice()
    {
        for (int i = 0; i < spawnedDiceToggles.Count; i++)
        {
            if (spawnedDiceToggles[i] != null)
                Destroy(spawnedDiceToggles[i].gameObject);
        }

        spawnedDiceToggles.Clear();
    }
}
