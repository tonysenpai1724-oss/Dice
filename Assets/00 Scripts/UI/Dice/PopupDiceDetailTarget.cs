using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PopupDiceDetailTarget : UIBase
{
    public override bool ShouldPauseGameplay => false;

    public Image diceImage;
    public TextMeshProUGUI diceNameText;
    public TextMeshProUGUI diceDescriptionText;
    public TextMeshProUGUI diceStatsText;
    public DiceData diceData;
    Coroutine hideCoroutine;
    int hideVersion;

    public override void Show()
    {
        hideVersion++;
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        if (hackObj != null)
            hackObj.SetActive(GameManager.Instance.IsTester);

        if (blockPanel != null)
            blockPanel.SetActive(false);

        IsAnimating = false;
        gameObject.SetActive(true);
        ResetAnimatorAfterCancelHide();

        if (UIManager.Instance != null && UIManager.Instance.lstOpenningUI != null)
            UIManager.Instance.lstOpenningUI.Remove(this);

        if (buttonClose != null)
        {
            buttonClose.onClick.RemoveListener(Hide);
            buttonClose.onClick.AddListener(Hide);
        }

        this.transform.SetAsLastSibling();
    }

    public override void Hide()
    {
        if (!gameObject.activeSelf)
            return;

        if (hideCoroutine != null)
            return;

        int version = ++hideVersion;

        if (buttonClose != null)
            buttonClose.onClick.RemoveListener(Hide);

        if (UIManager.Instance != null && UIManager.Instance.lstOpenningUI != null)
            UIManager.Instance.lstOpenningUI.Remove(this);

        hideCoroutine = StartCoroutine(IEHideTarget(version));
    }

    IEnumerator IEHideTarget(int version)
    {
        IsAnimating = true;

        if (animatorUI != null)
        {
            animatorUI.updateMode = AnimatorUpdateMode.UnscaledTime;
            animatorUI.Play(closeAnim, 0, 0f);
            AnimationClip closeClip = GetAnimationClip(closeAnim);
            if (closeClip != null)
                yield return new WaitForSecondsRealtime(closeClip.length);
        }

        if (version != hideVersion)
            yield break;

        hideCoroutine = null;
        IsAnimating = false;
        diceData = null;
        gameObject.SetActive(false);
    }

    void ResetAnimatorAfterCancelHide()
    {
        if (animatorUI == null)
            return;

        animatorUI.updateMode = AnimatorUpdateMode.UnscaledTime;

        if (GetAnimationClip(normalAnim) != null)
        {
            animatorUI.Play(normalAnim, 0, 0f);
            return;
        }

        if (GetAnimationClip(openAnim) != null)
            animatorUI.Play(openAnim, 0, 1f);
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
