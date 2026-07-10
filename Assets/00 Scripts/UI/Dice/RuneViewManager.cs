using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class RuneViewManager : SerializedMonoBehaviour
{
    public static RuneViewManager Instance { get; private set; }

    [Header("Data")]
    public Sprite lockedSprite;

    [Header("Slots")]
    public bool autoCollectChildSlots = true;
    public List<RuneUI> runeSlots = new();
    public List<RectTransform> slotPositions = new();
    public RuneUI runePrefab;

    void Awake()
    {
        Instance = this;
        InitializeSlots();
        TigerForge.EventManager.StartListening(Constant.ON_RUNE_CHANGE, OnRuneChange);
    }

    void OnRuneChange()
    {
        RefreshAllSlots();
    }

    void OnEnable()
    {
        InitializeSlots();
        RefreshAllSlots();
    }

    void OnDestroy()
    {
        TigerForge.EventManager.StopListening(Constant.ON_RUNE_CHANGE, OnRuneChange);

        if (Instance == this)
            Instance = null;
    }

    [Button]
    public void InitializeSlots()
    {
        if (autoCollectChildSlots)
        {
            runeSlots.Clear();
            GetComponentsInChildren(true, runeSlots);
        }

        for (int i = 0; i < runeSlots.Count; i++)
        {
            RuneUI runeUI = runeSlots[i];
            if (runeUI == null)
                continue;

            runeUI.SetViewManager(this);
            runeUI.SetLockedSprite(lockedSprite);
            runeUI.SetIndex(i);
            ApplySlotPosition(runeUI, i);
        }

        RefreshAllSlots();
    }

    public Sprite GetRuneSprite(RuneSkillData runeSkill)
    {
        return runeSkill != null ? runeSkill.runeSprite : null;
    }

    void ApplySlotPosition(RuneUI runeUI, int index)
    {
        if (runeUI == null || index < 0 || index >= slotPositions.Count)
            return;

        RectTransform target = slotPositions[index];
        RectTransform runeRect = runeUI.transform as RectTransform;
        if (target == null || runeRect == null)
            return;

        runeRect.SetParent(target.parent, false);
        runeRect.anchorMin = target.anchorMin;
        runeRect.anchorMax = target.anchorMax;
        runeRect.pivot = target.pivot;
        runeRect.anchoredPosition = target.anchoredPosition;
        runeRect.sizeDelta = target.sizeDelta;
    }

    public void RefreshSlot(int index)
    {
        if (index < 0 || index >= runeSlots.Count)
            return;

        if (runeSlots[index] != null)
            runeSlots[index].Refresh();
    }

    public void RefreshAllSlots()
    {
        for (int i = 0; i < runeSlots.Count; i++)
        {
            if (runeSlots[i] != null)
                runeSlots[i].Refresh();
        }
    }
}

