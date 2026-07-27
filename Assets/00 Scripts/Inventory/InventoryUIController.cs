using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

public enum InventoryDiceSource
{
    ChapterSession,
    PlayerController,
    ManualList
}

public enum InventoryViewTab
{
    Dice,
    Relic
}

public class InventoryUIController : UIBase
{
    public InventoryItemPreview itemPrefab;
    public Transform itemParent;
    public ItemToggle itemTogglePrefab;
    public ItemPreviewGenerator previewGenerator;
    public InventoryDiceSource diceSource = InventoryDiceSource.ChapterSession;
    public List<DiceData> diceItems = new();
    public List<ItemToggle> itemToggles;

    [Header("Tabs")]
    public Button diceButton;
    [FormerlySerializedAs("runeButton")]
    public Button relicButton;
    public InventoryViewTab defaultTab = InventoryViewTab.Dice;

    public event Action<ItemToggle> ItemSelected;

    [Header("Layout")]
    public Vector3 startPosition = new Vector3(-10f, -3.15f, 0f);
    public Vector2 itemOffset = new Vector2(4.3f, 4.3f);
    [Min(1)] public int itemsPerRow = 5;

    readonly List<InventoryItemPreview> spawnedItems = new();
    readonly List<ItemToggle> spawnedToggles = new();
    InventoryViewTab currentTab = InventoryViewTab.Dice;
    bool itemToggleCanClick = true;

    void Start()
    {
        BindTabButtons();
        SelectTab(defaultTab, false);
        TigerForge.EventManager.StartListening(Constant.ON_EQUIMENT_DICE_CHANGED, RefreshCurrentTab);
    }

    public void SetDiceItems(List<DiceData> newDiceItems)
    {
        diceSource = InventoryDiceSource.ManualList;
        diceItems = newDiceItems != null
            ? new List<DiceData>(newDiceItems)
            : new List<DiceData>();

        if (currentTab == InventoryViewTab.Dice)
            RefreshItems();
    }

    void OnEnable()
    {
        BindTabButtons();
        SelectTab(defaultTab, false);
    }

    void OnDestroy()
    {
        TigerForge.EventManager.StopListening(Constant.ON_EQUIMENT_DICE_CHANGED, RefreshCurrentTab);
    }

    public void ShowDice()
    {
        SelectTab(InventoryViewTab.Dice);
    }

    // Legacy rune tab:
    // public void ShowRunes()
    // {
    //     SelectTab(InventoryViewTab.Rune);
    // }

    public void ShowRelics()
    {
        SelectTab(InventoryViewTab.Relic);
    }

    public void RefreshItems()
    {
        ClearItems();
        LoadDiceItemsFromSource();

        if (itemPrefab == null || itemTogglePrefab == null)
            return;

        if (previewGenerator == null)
            previewGenerator = ItemPreviewGenerator.Resolve();

        if (previewGenerator == null)
        {
            Debug.LogWarning("InventoryUIController needs ItemPreviewGenerator to render dice previews.");
            return;
        }

        Transform parent = itemParent != null ? itemParent : transform;
        itemToggles.Clear();

        for (int i = 0; i < diceItems.Count; i++)
        {
            DiceData diceData = diceItems[i];
            if (diceData == null)
                continue;

            Texture2D previewTexture = previewGenerator.Capture(itemPrefab, diceData);
            ItemToggle itemToggle = Instantiate(itemTogglePrefab, parent);
            itemToggle.Setup(diceData, previewTexture, OnItemToggleSelected);
            if (itemToggle.btn != null)
                itemToggle.btn.interactable = itemToggleCanClick;
            itemToggles.Add(itemToggle);
            spawnedToggles.Add(itemToggle);
        }
    }
    public void SetItemToggleButton(bool canClick)
    {
        itemToggleCanClick = canClick;
        foreach (var item in itemToggles)
        {
            if (item == null || item.btn == null)
                continue;

            item.btn.interactable = canClick;
        }
    }
    // Legacy rune tab:
    // public void RefreshRunes()
    // {
    //     ClearItems();
    //
    //     if (itemTogglePrefab == null)
    //         return;
    //
    //     Transform parent = itemParent != null ? itemParent : transform;
    //     itemToggles.Clear();
    //
    //     RuneManager runeManager = RuneManager.Instance;
    //     if (runeManager == null)
    //         return;
    //
    //     for (int i = 0; i < runeManager.SlotCount; i++)
    //     {
    //         if (!runeManager.IsSlotUnlocked(i))
    //             continue;
    //
    //         RuneSkillData runeData = runeManager.GetRune(i);
    //         if (runeData == null)
    //             continue;
    //
    //         Sprite runeSprite = runeData.runeSprite;
    //         ItemToggle itemToggle = Instantiate(itemTogglePrefab, parent);
    //         itemToggle.Setup(runeData, runeSprite, OnItemToggleSelected);
    //         if (itemToggle.btn != null)
    //             itemToggle.btn.interactable = itemToggleCanClick;
    //         itemToggles.Add(itemToggle);
    //         spawnedToggles.Add(itemToggle);
    //     }
    // }

    public void RefreshRelics()
    {
        ClearItems();

        if (itemTogglePrefab == null)
            return;

        Transform parent = itemParent != null ? itemParent : transform;
        itemToggles.Clear();

        RelicManager relicManager = RelicManager.Instance;
        if (relicManager == null)
            return;

        IReadOnlyList<RelicData> activeRelics = relicManager.ActiveRelics;
        for (int i = 0; i < activeRelics.Count; i++)
        {
            RelicData relicData = activeRelics[i];
            if (relicData == null)
                continue;

            Sprite relicSprite = relicData.relicSprite;
            ItemToggle itemToggle = Instantiate(itemTogglePrefab, parent);
            itemToggle.Setup(relicData, relicSprite, OnItemToggleSelected);
            if (itemToggle.btn != null)
                itemToggle.btn.interactable = itemToggleCanClick;
            itemToggles.Add(itemToggle);
            spawnedToggles.Add(itemToggle);
        }
    }

    public ItemToggle GetToggle(DiceData diceData)
    {
        if (diceData == null)
            return null;

        for (int i = 0; i < itemToggles.Count; i++)
        {
            ItemToggle itemToggle = itemToggles[i];
            if (itemToggle != null && itemToggle.data == diceData)
                return itemToggle;
        }

        return null;
    }

    void OnItemToggleSelected(ItemToggle itemToggle)
    {
        if (!itemToggleCanClick)
            return;

        if (itemToggle == null)
            return;

        ItemSelected?.Invoke(itemToggle);
        foreach (var item in itemToggles)
        {
            if (item != itemToggle)
            {
                item.SetSelect(false);
            }
            else
            {
                item.SetSelect(true);
            }
        }

        if (UIManager.Instance == null)
            return;

        switch (itemToggle.itemType)
        {
            case ItemToggleType.Dice:
                UIManager.Instance.ShowPopupDiceDetail(itemToggle.data, itemToggle.PreviewSprite);
                break;
            case ItemToggleType.Relic:
                UIManager.Instance.ShowPopupRelicDetail(itemToggle.relicData);
                break;
        }
    }

    void CacheItemToggles()
    {
        if (itemToggles == null)
            itemToggles = new List<ItemToggle>();

        if (itemToggles.Count == 0)
            itemToggles.AddRange(GetComponentsInChildren<ItemToggle>(true));
    }

    void LoadDiceItemsFromSource()
    {
        if (diceSource == InventoryDiceSource.ManualList)
            return;

        if (diceSource == InventoryDiceSource.PlayerController)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            diceItems = player != null && player.diceDatas != null
                ? new List<DiceData>(player.diceDatas)
                : new List<DiceData>();
            return;
        }

        ChapterDiceSession session = ChapterDiceSession.GetOrCreate();
        diceItems = session != null
            ? session.GetRuntimeDiceDatasCopy()
            : new List<DiceData>();
    }

    void BindTabButtons()
    {
        if (diceButton != null)
        {
            diceButton.onClick.RemoveListener(ShowDice);
            diceButton.onClick.AddListener(ShowDice);
        }

        if (relicButton != null)
        {
            relicButton.onClick.RemoveListener(ShowRelics);
            relicButton.onClick.AddListener(ShowRelics);
        }
    }

    void SelectTab(InventoryViewTab tab, bool refresh = true)
    {
        currentTab = tab;
        UpdateTabButtonState();

        if (!refresh)
        {
            RefreshCurrentTab();
            return;
        }

        RefreshCurrentTab();
    }

    void RefreshCurrentTab()
    {
        if (currentTab == InventoryViewTab.Relic)
        {
            RefreshRelics();
            return;
        }

        RefreshItems();
    }

    void UpdateTabButtonState()
    {
        if (diceButton != null)
            diceButton.interactable = currentTab != InventoryViewTab.Dice;

        if (relicButton != null)
            relicButton.interactable = currentTab != InventoryViewTab.Relic;
    }

    Vector3 GetItemPosition(int index)
    {
        int column = index % itemsPerRow;
        int row = index / itemsPerRow;

        return startPosition + new Vector3(
            column * itemOffset.x,
            -row * itemOffset.y,
            0f
        );
    }

    void ClearItems()
    {
        if (itemToggles == null)
            itemToggles = new List<ItemToggle>();

        for (int i = spawnedToggles.Count - 1; i >= 0; i--)
        {
            if (spawnedToggles[i] != null)
                Destroy(spawnedToggles[i].gameObject);
        }

        spawnedToggles.Clear();
        itemToggles.Clear();

        for (int i = spawnedItems.Count - 1; i >= 0; i--)
        {
            InventoryItemPreview item = spawnedItems[i];
            if (item == null)
                continue;

            Destroy(item.gameObject);
        }

        spawnedItems.Clear();
    }
}



