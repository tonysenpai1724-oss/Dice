using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RuneUI : MonoBehaviour,
    IPointerClickHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IDropHandler
{
    [Header("Slot")]
    public int slotIndex;

    [Header("View")]
    public Image iconImage;
    public Sprite lockedSprite;

    [Header("Drag")]
    public bool enableDrag = true;
    public bool executeOnClick = true;

    static RuneUI draggingSlot;

    RuneViewManager viewManager;
    RectTransform rectTransform;
    CanvasGroup canvasGroup;
    Transform originalParent;
    Vector2 originalAnchoredPosition;
    bool dragging;

    void Awake()
    {
        rectTransform = transform as RectTransform;
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        Refresh();
    }

    public void SetIndex(int index)
    {
        slotIndex = index;
        Refresh();
    }

    public void SetViewManager(RuneViewManager manager)
    {
        viewManager = manager;
    }

    public void SetLockedSprite(Sprite locked)
    {
        lockedSprite = locked;
    }

    public void Refresh()
    {
        RuneManager manager = RuneManager.Instance;
        if (manager == null)
            return;

        bool unlocked = manager.IsSlotUnlocked(slotIndex);
        RuneSkillData runeSkill = manager.GetRune(slotIndex);


        if (iconImage == null)
            return;

        if (!unlocked)
        {
            iconImage.enabled = lockedSprite != null;
            iconImage.sprite = lockedSprite;
            return;
        }

        if (runeSkill == null)
        {
            iconImage.enabled = false;
            iconImage.sprite = null;
            return;
        }

        Sprite runeSprite = GetRuneSprite(runeSkill);

        iconImage.enabled = true;
        iconImage.sprite = runeSprite;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!executeOnClick || dragging)
            return;

        RuneManager manager = RuneManager.Instance;
        if (manager == null || !manager.IsSlotUnlocked(slotIndex))
            return;

        manager.ExecuteRune(slotIndex);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        RuneManager manager = RuneManager.Instance;
        if (!enableDrag || manager == null)
            return;

        if (!manager.IsSlotUnlocked(slotIndex) || manager.GetRune(slotIndex) == null)
            return;

        dragging = true;
        draggingSlot = this;
        originalParent = transform.parent;

        if (rectTransform != null)
            originalAnchoredPosition = rectTransform.anchoredPosition;

        canvasGroup.blocksRaycasts = false;
        transform.SetAsLastSibling();
        TigerForge.EventManager.EmitEvent(Constant.ON_DRAG_RUNE);

    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging || rectTransform == null)
            return;

        rectTransform.position = eventData.position;

    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging)
            return;

        dragging = false;
        canvasGroup.blocksRaycasts = true;
        draggingSlot = null;
        ResetDragPosition();
        RefreshAllRuneSlots();
        TigerForge.EventManager.EmitEvent(Constant.ON_DROP_RUNE);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (draggingSlot == null || draggingSlot == this)
            return;

        RuneManager manager = RuneManager.Instance;
        if (manager == null)
            return;

        manager.SwapRunes(draggingSlot.slotIndex, slotIndex);
        RefreshAllRuneSlots();
    }

    Sprite GetRuneSprite(RuneSkillData runeSkill)
    {
        RuneViewManager manager = viewManager != null ? viewManager : RuneViewManager.Instance;
        if (manager != null)
            return manager.GetRuneSprite(runeSkill);

        return null;
    }

    void ResetDragPosition()
    {
        if (originalParent != null)
            transform.SetParent(originalParent, true);

        if (rectTransform != null)
            rectTransform.anchoredPosition = originalAnchoredPosition;
    }

    static void RefreshAllRuneSlots()
    {
        if (RuneViewManager.Instance != null)
        {
            RuneViewManager.Instance.RefreshAllSlots();
            return;
        }

        RuneUI[] runeSlots = FindObjectsByType<RuneUI>(FindObjectsSortMode.None);
        for (int i = 0; i < runeSlots.Length; i++)
        {
            runeSlots[i].Refresh();
        }
    }
}
