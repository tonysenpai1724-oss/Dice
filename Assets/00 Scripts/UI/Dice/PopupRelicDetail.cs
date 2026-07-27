using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PopupRelicDetail : UIBase
{
    public static PopupRelicDetail Instance;

    [Header("UI Elements")]
    public TextMeshProUGUI relicNameText;
    public TextMeshProUGUI relicDescriptionText;
    public Image relicImage;
    public RelicData relicData;

    void Awake()
    {
        Instance = this;
    }

    public void ShowRelicDetails(RelicData relicData)
    {
        if (relicData == null)
            return;

        if (relicNameText != null)
            relicNameText.text = relicData.name;

        if (relicDescriptionText != null)
            relicDescriptionText.text = relicData.description;

        if (relicImage != null)
        {
            relicImage.sprite = relicData.relicSprite;
            relicImage.enabled = relicData.relicSprite != null;
            relicImage.preserveAspect = true;
        }

        this.relicData = relicData;
        transform.localPosition = new Vector3(0, 335, 0);
    }
}
