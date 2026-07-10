using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PopupDiceDetail : UIBase
{
    public Image diceImage;
    public TextMeshProUGUI diceNameText;
    public TextMeshProUGUI diceDescriptionText;
    public TextMeshProUGUI diceStatsText;
    public DiceData diceData;

    public void SetDiceDetails(DiceData data, Sprite sprite)
    {
        if (data == null)
            return;

        if (diceImage != null)
        {
            diceImage.sprite = sprite;
            diceImage.enabled = sprite != null;
            diceImage.preserveAspect = true;
        }

        if (diceNameText != null)
            diceNameText.text = data.diceName;

        if (diceDescriptionText != null)
            diceDescriptionText.text = data.description;

        if (diceStatsText != null)
            diceStatsText.text = data.diceStatsDes;

        diceData = data;
        this.transform.localPosition = new Vector3(0, 335, 0);
    }
}
