using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PopupChapterRewardChoice : UIBase
{
    public TextMeshProUGUI txtTitle;
    public ChapterRewardChoiceItem itemPrefab;
    public Transform itemParent;

    [Header("Dice Preview")]
    public InventoryUIController inventoryUIController;
    public InventoryItem dicePreviewPrefab;
    public ItemPreviewGenerator previewGenerator;

    readonly List<ChapterRewardChoiceItem> spawnedItems = new();
    List<ChapterRewardChoiceOption> currentOptions = new();

    public void ShowChoices(List<ChapterRewardChoiceOption> options)
    {
        currentOptions = options ?? new List<ChapterRewardChoiceOption>();
        Show();
    }

    public override void Show()
    {
        base.Show();
        RefreshView();
    }

    void RefreshView()
    {
        if (txtTitle != null)
            txtTitle.text = "Choose 1 Reward";

        CachePreviewRefs();
        ClearItems();

        if (itemPrefab == null || itemParent == null)
            return;

        for (int i = 0; i < currentOptions.Count; i++)
        {
            ChapterRewardChoiceOption option = currentOptions[i];
            if (option == null)
                continue;

            ChapterRewardChoiceItem item = Instantiate(itemPrefab, itemParent);
            SetupRewardItem(item, option);
            spawnedItems.Add(item);
        }
    }

    void SetupRewardItem(ChapterRewardChoiceItem item, ChapterRewardChoiceOption option)
    {
        if (item == null)
            return;

        Sprite existingIcon = GetExistingOptionIcon(option);
        if (existingIcon != null)
        {
            item.Setup(option, OnSelectOption, existingIcon);
            return;
        }

        Texture2D capturedIcon = CaptureOptionIcon(option);
        if (capturedIcon != null)
        {
            item.Setup(option, OnSelectOption, capturedIcon);
            return;
        }

        item.Setup(option, OnSelectOption);
    }

    Sprite GetExistingOptionIcon(ChapterRewardChoiceOption option)
    {
        if (option == null)
            return null;

        if (option.type == ChapterRewardChoiceType.AddRune)
            return RuneViewManager.Instance != null ? RuneViewManager.Instance.GetRuneSprite(option.runeSkill) : null;

        DiceData diceData = GetOptionDice(option);
        if (inventoryUIController == null || diceData == null)
            return null;

        ItemToggle itemToggle = inventoryUIController.GetToggle(diceData);
        return itemToggle != null ? itemToggle.PreviewSprite : null;
    }

    Texture2D CaptureOptionIcon(ChapterRewardChoiceOption option)
    {
        DiceData diceData = GetOptionDice(option);
        if (diceData == null)
            return null;

        CachePreviewRefs();

        if (previewGenerator == null || dicePreviewPrefab == null)
        {
            Debug.LogWarning($"PopupChapterRewardChoice cannot capture dice icon for {diceData.diceName}: missing previewGenerator or dicePreviewPrefab.");
            return null;
        }

        Texture2D texture = previewGenerator.Capture(dicePreviewPrefab, diceData);
        if (texture == null)
            Debug.LogWarning($"PopupChapterRewardChoice failed to capture dice icon for {diceData.diceName}. Check preview camera/render texture.");

        return texture;
    }

    DiceData GetOptionDice(ChapterRewardChoiceOption option)
    {
        if (option == null)
            return null;

        switch (option.type)
        {
            case ChapterRewardChoiceType.UpgradeDice:
                return option.targetDice != null ? option.targetDice : option.sourceDice;
            case ChapterRewardChoiceType.AddDice:
                return option.targetDice;
            default:
                return null;
        }
    }

    void CachePreviewRefs()
    {
        if (inventoryUIController == null)
            inventoryUIController = FindFirstObjectByType<InventoryUIController>();

        if (inventoryUIController != null)
        {
            if (dicePreviewPrefab == null)
                dicePreviewPrefab = inventoryUIController.itemPrefab;

            if (previewGenerator == null)
                previewGenerator = inventoryUIController.previewGenerator;
        }

        if (previewGenerator == null)
            previewGenerator = FindFirstObjectByType<ItemPreviewGenerator>();
    }

    void OnSelectOption(ChapterRewardChoiceOption option)
    {
        if (GameplayManager.Instance != null)
            GameplayManager.Instance.ApplyChapterRewardChoice(option);

        Hide();
        if (UIManager.Instance != null)
            UIManager.Instance.ShowPopupEndGame();
    }

    void ClearItems()
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            if (spawnedItems[i] != null)
                Destroy(spawnedItems[i].gameObject);
        }

        spawnedItems.Clear();
    }
}