using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClonePanelDiceItem : MonoBehaviour
{
    public TextMeshProUGUI txtName;
    public TextMeshProUGUI txtLevel;
    public Image selectionHighlight;
    public Button button;

    Action<DiceData> onSelect;

    public DiceData DiceData { get; private set; }

    public void Setup(DiceData diceData, Action<DiceData> onSelectCallback)
    {
        DiceData = diceData;
        onSelect = onSelectCallback;

        if (txtName != null)
            txtName.text = diceData != null ? diceData.diceName : string.Empty;

        if (txtLevel != null)
            txtLevel.text = diceData != null ? $"Lv{diceData.level} - {diceData.type}" : string.Empty;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(Select);
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (selectionHighlight != null)
            selectionHighlight.enabled = isSelected;
    }

    void Select()
    {
        onSelect?.Invoke(DiceData);
    }
}