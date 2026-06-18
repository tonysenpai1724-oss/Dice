using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PopupChapterRewardChoice : UIBase
{
    public TextMeshProUGUI txtTitle;
    public ChapterRewardChoiceItem itemPrefab;
    public Transform itemParent;

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

        ClearItems();

        if (itemPrefab == null || itemParent == null)
            return;

        for (int i = 0; i < currentOptions.Count; i++)
        {
            ChapterRewardChoiceOption option = currentOptions[i];
            if (option == null)
                continue;

            ChapterRewardChoiceItem item = Instantiate(itemPrefab, itemParent);
            item.Setup(option, OnSelectOption);
            spawnedItems.Add(item);
        }
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
