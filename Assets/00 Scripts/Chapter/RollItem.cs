using UnityEngine;
using UnityEngine.UI;
public class RollItem : MonoBehaviour
{
    public Image bg;
    public Image header;
    public Image iconDie1;
    public Image iconDie2;
    public Image typeIcon;
    public Button button;
    public RollGuessType rollGuessType;
    public void SetupRollItem(Sprite bgSprite, Sprite headerSprite, Sprite iconDie1Sprite,
     Sprite iconDie2Sprite, Sprite typeIconSprite, RollGuessType guessType, System.Action actionClick)
    {
        if (bg != null && bgSprite != null)
            bg.sprite = bgSprite;
        if (header != null && headerSprite != null)
            header.sprite = headerSprite;
        if (iconDie1 != null && iconDie1Sprite != null)
            iconDie1.sprite = iconDie1Sprite;
        if (iconDie2 != null && iconDie2Sprite != null)
            iconDie2.sprite = iconDie2Sprite;
        if (typeIcon != null && typeIconSprite != null)
            typeIcon.sprite = typeIconSprite;
        rollGuessType = guessType;
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            if (actionClick != null)
                button.onClick.AddListener(actionClick.Invoke);
        }
    }

    public void SetInteractable(bool interactable)
    {
        if (button != null)
            button.interactable = interactable;
    }


}
