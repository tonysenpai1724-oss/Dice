using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PopupDiceDetailTarget : UIBase
{
    public Image diceImage;
    public TextMeshProUGUI diceNameText;
    public TextMeshProUGUI diceDescriptionText;
    public TextMeshProUGUI diceStatsText;
    public DiceData diceData;
    public override void Show()
    {
        DebugCustom.LogColor("Show popup", gameObject.name);
        if (hackObj != null)
            hackObj.SetActive(GameManager.Instance.IsTester);
        if (blockPanel != null)
            blockPanel.SetActive(false);
        gameObject.SetActive(true);
        // if (GameplayManager.Instance != null)
        // {
        //     GameplayManager.Instance.SetState(EGamePlayState.Pause);
        // }
        if (UIManager.Instance != null)
        {
            if (!UIManager.Instance.lstOpenningUI.Contains(this))
                UIManager.Instance.lstOpenningUI.Add(this);
        }
        if (buttonClose != null)
        {
            buttonClose.onClick.AddListener(() =>
            {
                Hide();
            });
        }
        this.transform.SetAsLastSibling();

    }

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
        //this.transform.localPosition = new Vector3(0, 335, 0);
    }
}
