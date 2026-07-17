using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupUpgradeCloneDice : UIBase
{
    public static PopupUpgradeCloneDice Instance;

    [Header("Dice List")]
    public ItemToggle itemTogglePrefab;
    public InventoryItemPreview previewItemPrefab;
    public ItemPreviewGenerator previewGenerator;
    public Transform itemParent;

    [Header("Panels")]
    public GameObject clonePanel;
    public GameObject upgradePanel;

    [Header("Selected Dice")]
    public Image diceImg;

    [Header("Clone Panel")]
    public TextMeshProUGUI txtCloneChance;
    public Button buttonClone;

    [Header("Upgrade Panel")]
    public TextMeshProUGUI txtUpgradeChance;
    public Button buttonUpgrade;

    [Header("Actions")]
    public Button buttonExit;

    readonly List<DiceData> currentDiceDatas = new();
    readonly List<ItemToggle> spawnedItems = new();

    DiceData selectedDiceData;
    int selectedDiceIndex = -1;
    Sprite selectedDiceSprite;

    void Awake()
    {
        Instance = this;
        BindButtons();
    }

    void OnDestroy()
    {
        ClearItems();
    }

    public override void Show()
    {
        base.Show();
        RefreshView();
    }

    public override void AfterHideAction()
    {
        GameManager.Instance?.CompleteCurrentSpecialLevel(LevelType.Upgrade);
    }

    void BindButtons()
    {
        if (buttonClone != null)
        {
            buttonClone.onClick.RemoveAllListeners();
            buttonClone.onClick.AddListener(OnClickClone);
        }

        if (buttonUpgrade != null)
        {
            buttonUpgrade.onClick.RemoveAllListeners();
            buttonUpgrade.onClick.AddListener(OnClickUpgrade);
        }

        if (buttonExit != null)
        {
            buttonExit.onClick.RemoveAllListeners();
            buttonExit.onClick.AddListener(Hide);
        }
    }

    void RefreshView()
    {
        BuildDiceList();
        RebuildItems();

        if (!IsSelectedDiceValid())
            ClearSelection();

        RefreshSelectedView();
        RefreshItemSelection();
    }

    void BuildDiceList()
    {
        currentDiceDatas.Clear();

        ChapterDiceSession session = ChapterDiceSession.GetOrCreate();
        if (session == null)
            return;

        List<DiceData> diceDatas = session.GetRuntimeDiceDatasCopy();
        if (diceDatas == null)
            return;

        currentDiceDatas.AddRange(diceDatas);
    }

    void RebuildItems()
    {
        ClearItems();

        if (itemTogglePrefab == null || itemParent == null)
            return;

        if (itemTogglePrefab.transform.IsChildOf(itemParent))
            itemTogglePrefab.gameObject.SetActive(false);

        EnsurePreviewGenerator();

        for (int i = 0; i < currentDiceDatas.Count; i++)
        {
            DiceData diceData = currentDiceDatas[i];
            if (diceData == null)
                continue;

            ItemToggle item = Instantiate(itemTogglePrefab, itemParent);
            item.gameObject.SetActive(true);
            int diceIndex = i;
            item.Setup(diceData, CaptureDicePreview(diceData), _ => OnSelectDice(diceIndex));
            spawnedItems.Add(item);
        }
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

    void OnSelectDice(int diceIndex)
    {
        selectedDiceIndex = diceIndex;
        selectedDiceData = diceIndex >= 0 && diceIndex < currentDiceDatas.Count ? currentDiceDatas[diceIndex] : null;
        RefreshSelectedView();
        RefreshItemSelection();
    }

    void RefreshSelectedView()
    {
        bool hasSelection = selectedDiceData != null;

        if (clonePanel != null)
            clonePanel.SetActive(hasSelection);
        if (upgradePanel != null)
            upgradePanel.SetActive(hasSelection);

        if (!hasSelection)
        {
            ClearRateTexts();
            SetDiceImage(null, null);
            SetActionButtons(false, false);
            return;
        }

        DiceData upgradeTarget = selectedDiceData.GetUpgradeData();
        int chancePercent = GetSuccessChancePercent(selectedDiceData);

        SetRateTexts(chancePercent);
        SetDiceImage(GetSelectedDiceSprite(), GetSelectedDiceTexture());
        SetActionButtons(true, upgradeTarget != null);
    }

    void ClearRateTexts()
    {
        if (txtCloneChance != null)
            txtCloneChance.text = string.Empty;
        if (txtUpgradeChance != null)
            txtUpgradeChance.text = string.Empty;
    }

    void SetRateTexts(int chancePercent)
    {
        if (txtCloneChance != null)
            txtCloneChance.text = $"Rate: {chancePercent}%";
        if (txtUpgradeChance != null)
            txtUpgradeChance.text = $"Rate: {chancePercent}%";
    }

    void RefreshItemSelection()
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            ItemToggle item = spawnedItems[i];
            if (item == null || item.btn == null)
                continue;

            item.btn.interactable = i != selectedDiceIndex;
        }
    }

    void SetActionButtons(bool canClone, bool canUpgrade)
    {
        if (buttonClone != null)
            buttonClone.interactable = canClone;
        if (buttonUpgrade != null)
            buttonUpgrade.interactable = canUpgrade;
    }

    void OnClickClone()
    {
        if (selectedDiceData == null)
            return;

        ChapterDiceSession session = ChapterDiceSession.GetOrCreate();
        if (session == null)
            return;

        DiceData attemptedDice = selectedDiceData;
        int chancePercent = GetSuccessChancePercent(attemptedDice);
        bool success = RollSuccess(chancePercent);

        if (success)
        {
            if (session.CloneDiceDataAt(selectedDiceIndex))
                Debug.Log("Clone success");
            else
                Debug.Log("Clone failed");
        }
        else
        {
            RemoveFailedDice(session, attemptedDice, "Clone failed");
        }

        RefreshAfterAction(attemptedDice, success);
    }

    void OnClickUpgrade()
    {
        if (selectedDiceData == null)
            return;

        ChapterDiceSession session = ChapterDiceSession.GetOrCreate();
        if (session == null)
            return;

        DiceData attemptedDice = selectedDiceData;
        DiceData upgradeTarget = attemptedDice.GetUpgradeData();
        if (upgradeTarget == null)
        {
            Debug.Log("Cannot upgrade");
            RefreshSelectedView();
            return;
        }

        int chancePercent = GetSuccessChancePercent(attemptedDice);
        bool success = RollSuccess(chancePercent);

        if (success)
        {
            if (session.UpgradeDiceDataAt(selectedDiceIndex, upgradeTarget))
            {
                selectedDiceData = upgradeTarget;
                Debug.Log("Upgrade success");
            }
            else
            {
                Debug.Log("Upgrade failed");
            }
        }
        else
        {
            RemoveFailedDice(session, attemptedDice, "Upgrade failed");
        }

        RefreshAfterAction(attemptedDice, success);
    }

    void RemoveFailedDice(ChapterDiceSession session, DiceData attemptedDice, string message)
    {
        if (session.RemoveDiceDataAt(selectedDiceIndex))
        {
            ClearSelection();
            Debug.Log(message);
        }
        else
        {
            Debug.Log("Failed");
        }
    }

    void RefreshAfterAction(DiceData attemptedDice, bool success)
    {
        BuildDiceList();
        RebuildItems();

        if (!success)
        {
            ClearSelection();
        }
        else if (!IsSelectedDiceValid())
        {
            ClearSelection();
        }

        RefreshSelectedView();
        RefreshItemSelection();
    }

    bool IsSelectedDiceValid()
    {
        return selectedDiceIndex >= 0 &&
               selectedDiceIndex < currentDiceDatas.Count &&
               selectedDiceData != null &&
               currentDiceDatas[selectedDiceIndex] == selectedDiceData;
    }

    void ClearSelection()
    {
        selectedDiceData = null;
        selectedDiceIndex = -1;
    }

    int GetSuccessChancePercent(DiceData diceData)
    {
        int level = diceData != null ? Mathf.Max(1, diceData.level) : 1;
        return Mathf.Clamp(100 - level * 10, 0, 100);
    }

    bool RollSuccess(int chancePercent)
    {
        return UnityEngine.Random.Range(1, 101) <= chancePercent;
    }

    Sprite GetSelectedDiceSprite()
    {
        if (selectedDiceIndex < 0 || selectedDiceIndex >= spawnedItems.Count)
            return null;

        ItemToggle item = spawnedItems[selectedDiceIndex];
        return item != null ? item.PreviewSprite : null;
    }

    Texture2D GetSelectedDiceTexture()
    {
        if (selectedDiceIndex < 0 || selectedDiceIndex >= spawnedItems.Count)
            return null;

        ItemToggle item = spawnedItems[selectedDiceIndex];
        return item != null ? item.PreviewTexture : null;
    }

    Texture2D CaptureDicePreview(DiceData diceData)
    {
        EnsurePreviewGenerator();

        if (previewGenerator == null || previewItemPrefab == null || diceData == null)
            return null;

        return previewGenerator.Capture(previewItemPrefab, diceData);
    }

    void EnsurePreviewGenerator()
    {
        if (previewGenerator == null)
            previewGenerator = FindFirstObjectByType<ItemPreviewGenerator>(FindObjectsInactive.Include);
    }

    void SetDiceImage(Sprite sprite, Texture2D texture)
    {
        ReleaseSelectedDiceSprite();

        if (diceImg == null)
            return;

        if (sprite == null && texture != null)
        {
            selectedDiceSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );
            sprite = selectedDiceSprite;
        }

        diceImg.sprite = sprite;
        diceImg.enabled = sprite != null;
        diceImg.preserveAspect = true;
    }

    void ReleaseSelectedDiceSprite()
    {
        if (selectedDiceSprite == null)
            return;

        Destroy(selectedDiceSprite);
        selectedDiceSprite = null;
    }
}