using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupUpgrade : UIBase
{
    public static PopupUpgrade Instance;

    [Header("Chance")]
    [Range(0f, 1f)] public float upgradeSuccessChance = 0.8f;

    [Header("List")]
    public ClonePanelDiceItem itemPrefab;
    public Transform itemParent;
    public InventoryUIController inventoryUIController;

    [Header("Info")]
    public TextMeshProUGUI txtSelectedDice;
    public TextMeshProUGUI txtDescription;
    public TextMeshProUGUI txtChance;
    public TextMeshProUGUI txtResult;
    public Image diceImg;

    [Header("Actions")]
    public Button buttonUse;
    public Button buttonExit;

    readonly List<DiceData> currentDiceDatas = new();

    DiceData selectedDiceData;

    void Awake()
    {
        Instance = this;
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
        if (inventoryUIController != null)
            inventoryUIController.ItemSelected += OnInventoryItemSelected;
        //gameObject.SetActive(false);
    }
    void Start()
    {
        Show();
    }

    void OnEnable()
    {
        RefreshView();

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
        RefreshSelectedView();
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
        RefreshSelectedDiceImage();
    }

    void OnInventoryItemSelected(ItemToggle itemToggle)
    {
        if (itemToggle == null)
            return;

        OnSelectDice(itemToggle.data);
        SetDiceImage(itemToggle.PreviewSprite);
    }

    void RefreshSelectedView()
    {
        ChapterDiceSession session = ChapterDiceSession.GetOrCreate();
        DiceData targetDice = session != null ? session.GetSummedUpgradeTarget(selectedDiceData) : null;
        int totalLevel = selectedDiceData != null && session != null ? session.GetTotalDiceLevelByType(selectedDiceData.type) : 0;

        if (txtSelectedDice != null)
            txtSelectedDice.text = selectedDiceData != null
                ? $"{selectedDiceData.diceName} Lv{selectedDiceData.level}"
                : "";

        if (txtDescription != null)
        {
            if (selectedDiceData == null)
            {
                txtDescription.text = "";
            }
            else if (targetDice == null)
            {
                txtDescription.text = $"Total same-type level = {totalLevel}. No valid upgrade target found.";
            }
            else
            {
                txtDescription.text = $"Total same-type level = {totalLevel}. Use to upgrade selected dice to Lv{targetDice.level}. Other dice stay unchanged.";
            }
        }

        if (txtChance != null)
            txtChance.text = $"Upgrade Chance: {Mathf.RoundToInt(GetSuccessChance01() * 100f)}%";

        if (buttonUse != null)
            buttonUse.interactable = selectedDiceData != null && targetDice != null;

        RefreshSelectedDiceImage();
    }

    void RefreshSelectedDiceImage()
    {
        if (diceImg == null)
            return;

        ItemToggle selectedToggle = inventoryUIController != null
            ? inventoryUIController.GetToggle(selectedDiceData)
            : null;

        SetDiceImage(selectedToggle != null ? selectedToggle.PreviewSprite : null);
    }

    void SetDiceImage(Sprite sprite)
    {
        if (diceImg == null)
            return;

        diceImg.sprite = sprite;
        diceImg.enabled = sprite != null;
        diceImg.preserveAspect = true;
    }

    float GetSuccessChance01()
    {
        return Mathf.Clamp01(upgradeSuccessChance);
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

        DiceData previewTarget = session.GetSummedUpgradeTarget(selectedDiceData);
        if (previewTarget == null)
        {
            if (txtResult != null)
                txtResult.text = "Upgrade failed: no valid target.";
            RefreshSelectedView();
            return;
        }

        float roll = UnityEngine.Random.value;
        float successChance = GetSuccessChance01();
        bool success = roll <= successChance;

        if (!success)
        {
            if (session.DowngradeDiceData(selectedDiceData, out DiceData downgradedDiceData))
            {
                if (txtResult != null)
                    txtResult.text = $"Upgrade failed ({Mathf.RoundToInt(roll * 100f)} / {Mathf.RoundToInt(successChance * 100f)}%). Selected dice dropped to Lv{downgradedDiceData.level}.";
            }
            else if (txtResult != null)
            {
                txtResult.text = $"Upgrade failed ({Mathf.RoundToInt(roll * 100f)} / {Mathf.RoundToInt(successChance * 100f)}%).";
            }

            RefreshView();
            return;
        }

        if (!session.UpgradeDiceDataByTypeSum(selectedDiceData, out DiceData upgradedDiceData))
        {
            if (txtResult != null)
                txtResult.text = "Upgrade failed.";
            RefreshSelectedView();
            return;
        }

        selectedDiceData = upgradedDiceData;
        RefreshView();

        if (txtResult != null)
            txtResult.text = $"Upgrade success! Selected dice is now Lv{upgradedDiceData.level}.";
    }
}