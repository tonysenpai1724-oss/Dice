using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClonePanel : UIBase
{
    public static ClonePanel Instance;

    [Header("Chance")]
    [Range(0f, 1f)] public float cloneSuccessChance = 0.8f;

    [Header("List")]
    public ClonePanelDiceItem itemPrefab;
    public Transform itemParent;

    [Header("Info")]
    public TextMeshProUGUI txtSelectedDice;
    public TextMeshProUGUI txtDescription;
    public TextMeshProUGUI txtChance;
    public TextMeshProUGUI txtResult;

    [Header("Actions")]
    public Button buttonUse;
    public Button buttonExit;

    readonly List<ClonePanelDiceItem> spawnedItems = new();
    readonly List<DiceData> currentDiceDatas = new();

    DiceData selectedDiceData;

    void Awake()
    {
        Instance = this;
        // gameObject.SetActive(false);
    }
    void Start()
    {
        Show();
    }
    void OnEnable()
    {
        if (buttonUse != null)
        {
            buttonUse.onClick.RemoveListener(OnClickUse);
            buttonUse.onClick.AddListener(OnClickUse);
        }

        if (buttonExit != null)
        {
            buttonExit.onClick.RemoveListener(Hide);
            buttonExit.onClick.AddListener(Hide);
        }
    }

    void OnDisable()
    {
        if (buttonUse != null)
            buttonUse.onClick.RemoveListener(OnClickUse);

        if (buttonExit != null)
            buttonExit.onClick.RemoveListener(Hide);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        RefreshView();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    void RefreshView()
    {
        ClearResult();
        BuildDiceList();
        SelectDefaultDice();
        RebuildItems();
        RefreshSelectedView();
    }

    void BuildDiceList()
    {
        currentDiceDatas.Clear();

        ChapterDiceSession session = ChapterDiceSession.GetOrCreate();
        if (session == null)
            return;

        List<DiceData> cloneable = session.GetCloneableDiceOptions();
        if (cloneable == null)
            return;

        currentDiceDatas.AddRange(cloneable);
    }

    void RebuildItems()
    {
        ClearItems();

        if (itemPrefab == null || itemParent == null)
            return;

        for (int i = 0; i < currentDiceDatas.Count; i++)
        {
            DiceData diceData = currentDiceDatas[i];
            if (diceData == null)
                continue;

            ClonePanelDiceItem item = Instantiate(itemPrefab, itemParent);
            item.Setup(diceData, OnSelectDice);
            item.SetSelected(diceData == selectedDiceData);
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

    void SelectDefaultDice()
    {
        if (selectedDiceData != null && currentDiceDatas.Contains(selectedDiceData))
            return;

        selectedDiceData = currentDiceDatas.Count > 0 ? currentDiceDatas[0] : null;
    }

    void OnSelectDice(DiceData diceData)
    {
        selectedDiceData = diceData;
        ClearResult();
        RefreshSelectedView();
        RefreshItemSelection();
    }

    void RefreshSelectedView()
    {
        if (txtSelectedDice != null)
            txtSelectedDice.text = selectedDiceData != null
                ? $"Selected: {selectedDiceData.diceName} Lv{selectedDiceData.level}"
                : "Selected: None";

        if (txtDescription != null)
            txtDescription.text = selectedDiceData != null
                ? $"Use the altar to try cloning 1 extra {selectedDiceData.type} dice."
                : "No dice available to clone.";

        if (txtChance != null)
            txtChance.text = $"Clone Chance: {Mathf.RoundToInt(GetSuccessChance01() * 100f)}%";

        if (buttonUse != null)
            buttonUse.interactable = selectedDiceData != null;
    }

    void RefreshItemSelection()
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            ClonePanelDiceItem item = spawnedItems[i];
            if (item == null)
                continue;

            item.SetSelected(item.DiceData == selectedDiceData);
        }
    }

    float GetSuccessChance01()
    {
        return Mathf.Clamp01(cloneSuccessChance);
    }

    void ClearResult()
    {
        if (txtResult != null)
            txtResult.text = string.Empty;
    }

    void OnClickUse()
    {
        if (selectedDiceData == null)
            return;

        ChapterDiceSession session = ChapterDiceSession.GetOrCreate();
        if (session == null)
            return;

        float roll = UnityEngine.Random.value;
        float successChance = GetSuccessChance01();
        bool success = roll <= successChance;

        if (!success)
        {
            if (txtResult != null)
                txtResult.text = $"Clone failed ({Mathf.RoundToInt(roll * 100f)} / {Mathf.RoundToInt(successChance * 100f)}%).";
            return;
        }

        if (!session.CloneDiceData(selectedDiceData))
        {
            if (txtResult != null)
                txtResult.text = "Clone failed: selected dice is no longer available.";
            return;
        }

        if (txtResult != null)
            txtResult.text = $"Clone success! {selectedDiceData.diceName} was duplicated.";

        RefreshView();
    }
}