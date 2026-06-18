using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChapterRewardChoiceItem : MonoBehaviour
{
    public TextMeshProUGUI txtTitle;
    public TextMeshProUGUI txtDescription;
    public Button button;

    ChapterRewardChoiceOption currentOption;
    Action<ChapterRewardChoiceOption> onSelect;

    public void Setup(ChapterRewardChoiceOption option, Action<ChapterRewardChoiceOption> onSelectCallback)
    {
        currentOption = option;
        onSelect = onSelectCallback;

        if (txtTitle != null)
            txtTitle.text = option != null ? option.title : string.Empty;

        if (txtDescription != null)
            txtDescription.text = option != null ? option.description : string.Empty;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(Select);
        }
    }

    void Select()
    {
        onSelect?.Invoke(currentOption);
    }
}
