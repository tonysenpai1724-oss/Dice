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

        manager.SetLastRuneDropWorldPosition(GetBoardDropWorldPosition(eventData));
        manager.ExecuteRune(slotIndex);
    }
    bool IsDropOnBoard(PointerEventData eventData)
    {
        if (Camera.main == null)
            return false;

        if (DiceManager.Instance == null || DiceManager.Instance.boardCollider == null)
            return false;

        Collider boardCollider = DiceManager.Instance.boardCollider;

        Ray ray = Camera.main.ScreenPointToRay(eventData.position);

        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            Mathf.Infinity,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide
        );

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider == boardCollider)
                return true;
        }

        return false;
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

        UpdateGravityPreview(eventData);

    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging)
            return;

        dragging = false;
        RuneManager.Instance?.ClearGravityPreview();
        canvasGroup.blocksRaycasts = true;

        RuneManager manager = RuneManager.Instance;

        bool used = false;

        if (manager != null && IsDropOnBoard(eventData))
        {
            manager.SetLastRuneDropWorldPosition(GetBoardDropWorldPosition(eventData));
            manager.ExecuteRune(slotIndex);
            manager.RemoveRune(slotIndex); // hoặc SetRune(slotIndex, null)
            used = true;
        }

        draggingSlot = null;

        if (!used)
        {
            ResetDragPosition();
        }

        RefreshAllRuneSlots();

        TigerForge.EventManager.EmitEvent(Constant.ON_DROP_RUNE);
        // if (!dragging)
        //     return;

        // dragging = false;
        // canvasGroup.blocksRaycasts = true;
        // draggingSlot = null;
        // ResetDragPosition();
        // RefreshAllRuneSlots();
        // TigerForge.EventManager.EmitEvent(Constant.ON_DROP_RUNE);
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

    void UpdateGravityPreview(PointerEventData eventData)
    {
        RuneManager manager = RuneManager.Instance;
        if (manager == null)
            return;

        RuneSkillData runeSkill = manager.GetRune(slotIndex);
        if (runeSkill == null || runeSkill.TargetType != RuneType.Gravity)
        {
            manager.ClearGravityPreview();
            return;
        }

        if (!IsDropOnBoard(eventData))
        {
            manager.ClearGravityPreview();
            return;
        }

        Vector3 worldPosition = GetBoardDropWorldPosition(eventData);
        float radius = Mathf.Max(1.5f, runeSkill.valueApply);
        manager.SetGravityPreview(worldPosition, radius);
    }

    Vector3 GetBoardDropWorldPosition(PointerEventData eventData)
    {
        if (Camera.main == null)
            return Vector3.zero;

        Ray ray = Camera.main.ScreenPointToRay(eventData.position);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return hit.point;

        Plane plane = new Plane(Vector3.up, Vector3.zero);
        if (plane.Raycast(ray, out float enter))
            return ray.GetPoint(enter);

        return Vector3.zero;
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
