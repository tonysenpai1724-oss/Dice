using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PopupRuneDetail : UIBase
{
    public static PopupRuneDetail Instance;

    [Header("UI Elements")]
    // public GameObject popupRoot;
    public TextMeshProUGUI runeNameText;
    public TextMeshProUGUI runeDescriptionText;
    public Image runeImage;
    public RuneSkillData runeData;

    void Awake()
    {
        Instance = this;
    }

    public void ShowRuneDetails(RuneSkillData runeData)
    {
        if (runeData == null)
            return;

        if (runeNameText != null)
            runeNameText.text = runeData.runeName;

        if (runeDescriptionText != null)
            runeDescriptionText.text = runeData.description;

        if (runeImage != null)
        {
            runeImage.sprite = runeData.runeSprite;
            runeImage.enabled = runeData.runeSprite != null;
            runeImage.preserveAspect = true;
        }

        this.runeData = runeData;
        this.transform.localPosition = new Vector3(0, 335, 0);
    }


}
