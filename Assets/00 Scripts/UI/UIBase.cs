using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIBase : MonoBehaviour
{
    public bool allowBackKey;
    //public WindowID windowID;
    [SerializeField]
    protected Animator animatorUI;

    public Button buttonClose;
    public GameObject blockPanel;
    public GameObject hackObj;
    public static string openAnim = "Open";
    public static string normalAnim = "Normal";
    public static string closeAnim = "Close";
    protected bool IsAnimating;
    Coroutine showCoroutine;
    Coroutine hideCoroutine;
    int transitionVersion;
    bool isHiding;
    public virtual bool ShouldPauseGameplay => true;
    protected bool canAction => GameManager.Instance.GameState != EGameState.Loading;
    #region MonoBehaviour
    public virtual void OnDisable()
    {
        StopAllCoroutines();
        Dispose();
        if (UIManager.Instance != null)
            UIManager.Instance.HandleCloseUI(this);
    }

    #endregion
    #region Method

    private void Dispose()
    {
        if (buttonClose != null)
            buttonClose.onClick.RemoveAllListeners();
    }
    public virtual void Show()
    {
        DebugCustom.LogColor("Show popup", gameObject.name);
        transitionVersion++;
        isHiding = false;
        if (showCoroutine != null)
            StopCoroutine(showCoroutine);
        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        if (hackObj != null)
            hackObj.SetActive(GameManager.Instance.IsTester);
        if (blockPanel != null)
            blockPanel.SetActive(true);
        gameObject.SetActive(true);
        if (UIManager.Instance != null)
        {
            if (!UIManager.Instance.lstOpenningUI.Contains(this))
                UIManager.Instance.lstOpenningUI.Add(this);
        }
        if (buttonClose != null)
        {
            buttonClose.onClick.RemoveListener(Hide);
            buttonClose.onClick.AddListener(Hide);
        }
        this.transform.SetAsLastSibling();
        IsAnimating = true;
        showCoroutine = StartCoroutine(IEShow(transitionVersion));
        transform.SetAsLastSibling();

    }
    public virtual IEnumerator IEShow(int version)
    {
        if (animatorUI != null)
        {
            animatorUI.updateMode = AnimatorUpdateMode.UnscaledTime;
            animatorUI.Play(openAnim, 0, 0f);
            AnimationClip openClip = GetAnimationClip(openAnim);
            if (openClip != null)
                yield return new WaitForSecondsRealtime(openClip.length);
            //UiAnim.Play(normalAnim);
        }

        if (version != transitionVersion || isHiding || !gameObject.activeInHierarchy)
            yield break;

        if (blockPanel != null)
            blockPanel.SetActive(false);
        IsAnimating = false;
        showCoroutine = null;
        UIManager.Instance?.SyncGameplayPauseState();
        ActionAfterShow();
    }
    public AnimationClip GetAnimationClip(string name)
    {
        AnimationClip result = null;
        if (animatorUI == null || animatorUI.runtimeAnimatorController == null)
            return result;

        AnimationClip[] allClips = animatorUI.runtimeAnimatorController.animationClips;
        int length = allClips.Length;
        for (int i = 0; i < length; i++)
            if (allClips[i].name == name)
            {
                result = allClips[i];
                break;
            }

        return result;
    }
    public virtual void ActionAfterShow()
    {
    }

    public virtual void Hide()
    {
        if (!gameObject.activeInHierarchy || isHiding)
            return;

        transitionVersion++;
        isHiding = true;
        if (showCoroutine != null)
        {
            StopCoroutine(showCoroutine);
            showCoroutine = null;
        }

        hideCoroutine = StartCoroutine(IEClose(transitionVersion));
    }
    IEnumerator IEClose(int version)
    {
        if (GameplayManager.Instance != null)
        {
            //bool showingAds = true;
            //GameManager.Instance.TryShowInterAds(() => { showingAds = false; }, name);
            //yield return new WaitUntil(() => !showingAds);
        }
        if (animatorUI != null)
        {
            animatorUI.updateMode = AnimatorUpdateMode.UnscaledTime;
            animatorUI.Play(closeAnim, 0, 0f);
            AnimationClip closeClip = GetAnimationClip(closeAnim);
            if (closeClip != null)
                yield return new WaitForSecondsRealtime(closeClip.length);
        }

        if (version != transitionVersion)
            yield break;

        hideCoroutine = null;
        IsAnimating = false;
        isHiding = false;

        if (UIManager.Instance != null)
            UIManager.Instance.HandleCloseUI(this);

        AfterHideAction();
        gameObject.SetActive(false);
    }
    public virtual void AfterHideAction()
    {


    }
    #endregion
}
