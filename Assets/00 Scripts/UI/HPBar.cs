using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HPBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI hpText;

    public void SetHp(int currentHp, int maxHp)
    {
        currentHp = Mathf.Max(0, currentHp);
        maxHp = Mathf.Max(0, maxHp);

        if (fillImage != null)
        {
            fillImage.fillAmount = maxHp > 0 ? Mathf.Clamp01((float)currentHp / maxHp) : 0f;
        }

        if (hpText != null)
        {
            hpText.text = currentHp + "/" + maxHp;
        }
    }
}
