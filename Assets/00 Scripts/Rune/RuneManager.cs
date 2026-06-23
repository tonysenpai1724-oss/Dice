using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RuneManager : Singleton<RuneManager>
{
    [Header("Slots")]
    [SerializeField] private int slotCount = 4;
    [SerializeField] private int lockedSlotCount = 2;
    [SerializeField] private List<RuneSlot> slots = new();

    public IReadOnlyList<RuneSlot> Slots => slots;
    public int SlotCount => slots.Count;
    public int LockedSlotCount => Mathf.Clamp(lockedSlotCount, 0, slotCount);
    public int UnlockedSlotCount => Mathf.Max(0, SlotCount - LockedSlotCount);

    public List<RuneSkillData> RuneSkillDatas;
    public Vector3 LastRuneDropWorldPosition { get; private set; }
    public bool ShowGravityPreview { get; private set; }
    public float GravityPreviewRadius { get; private set; }



    protected override void OnAwake()
    {
        base.OnAwake();
        SyncSlots();
    }

    void OnValidate()
    {
        SyncSlots();
    }

    public bool IsSlotUnlocked(int index)
    {
        return IsValidSlot(index) && !slots[index].locked;
    }

    public bool IsSlotEmpty(int index)
    {
        return IsValidSlot(index) && slots[index].runeSkill == null;
    }

    public bool CanSetRune(int index)
    {
        return IsSlotUnlocked(index);
    }

    public RuneSkillData GetRune(int index)
    {
        if (!IsValidSlot(index))
            return null;

        return slots[index].runeSkill;
    }

    public bool TryAddRune(RuneSkillData runeSkill)
    {
        if (runeSkill == null)
            return false;

        for (int i = 0; i < slots.Count; i++)
        {
            if (!IsSlotUnlocked(i) || !IsSlotEmpty(i))
                continue;

            slots[i].runeSkill = runeSkill;
            NotifyRuneChanged();
            return true;
        }

        return false;
    }

    public bool TrySetRune(int index, RuneSkillData runeSkill)
    {
        if (!CanSetRune(index))
            return false;

        slots[index].runeSkill = runeSkill;
        NotifyRuneChanged();
        return true;
    }

    public bool RemoveRune(int index)
    {
        if (!CanSetRune(index))
            return false;

        slots[index].runeSkill = null;
        NotifyRuneChanged();
        return true;
    }

    public bool SwapRunes(int fromIndex, int toIndex)
    {
        if (!CanSetRune(fromIndex) || !CanSetRune(toIndex))
            return false;

        if (fromIndex == toIndex)
            return true;

        RuneSkillData fromRune = slots[fromIndex].runeSkill;
        slots[fromIndex].runeSkill = slots[toIndex].runeSkill;
        slots[toIndex].runeSkill = fromRune;
        NotifyRuneChanged();
        return true;
    }

    public void SetGravityPreview(Vector3 worldPosition, float radius)
    {
        LastRuneDropWorldPosition = worldPosition;
        GravityPreviewRadius = radius;
        ShowGravityPreview = true;
    }

    public void ClearGravityPreview()
    {
        ShowGravityPreview = false;
    }

    void OnDrawGizmos()
    {
        if (!ShowGravityPreview)
            return;

#if UNITY_EDITOR
        Handles.color = new Color(0.3f, 0.8f, 1f, 0.9f);
        Handles.DrawWireDisc(LastRuneDropWorldPosition, Vector3.up, GravityPreviewRadius);
#else
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.8f);
        Gizmos.DrawWireSphere(LastRuneDropWorldPosition, GravityPreviewRadius);
#endif
    }

    public void SetLastRuneDropWorldPosition(Vector3 worldPosition)
    {
        LastRuneDropWorldPosition = worldPosition;
    }

    public void ExecuteRune(int index)
    {
        if (!IsSlotUnlocked(index))
            return;

        EnemyManager enemyManager = EnemyManager.Instance;
        PlayerController player =
            enemyManager != null
                ? enemyManager.player
                : null;
        Enemy targetEnemy =
            enemyManager != null
                ? enemyManager.GetNearestAliveEnemy()
                : null;

        GameplayManager gameplay = GameplayManager.Instance;
        gameplay?.BeginDiceSkill(
            null,
             null,
            null,
            enemyManager,
            player,
            targetEnemy
        );

        slots[index].runeSkill?.Execute();
    }

    public void ExecuteAllRunes()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            ExecuteRune(i);
        }
    }

    public void UnlockSlot(int index)
    {
        if (!IsValidSlot(index))
            return;

        slots[index].locked = false;
        RecalculateLockedSlotCount();
        NotifyRuneChanged();
    }

    public void LockSlot(int index)
    {
        if (!IsValidSlot(index))
            return;

        slots[index].locked = true;
        slots[index].runeSkill = null;
        RecalculateLockedSlotCount();
        NotifyRuneChanged();
    }

    public void SetLockedSlotCount(int count)
    {
        lockedSlotCount = Mathf.Clamp(count, 0, slotCount);
        SyncSlots();
        NotifyRuneChanged();
    }

    void NotifyRuneChanged()
    {
        TigerForge.EventManager.EmitEvent(Constant.ON_RUNE_CHANGE);
    }

    bool IsValidSlot(int index)
    {
        return index >= 0 && index < slots.Count;
    }

    void SyncSlots()
    {
        slotCount = Mathf.Max(0, slotCount);
        lockedSlotCount = Mathf.Clamp(lockedSlotCount, 0, slotCount);

        while (slots.Count < slotCount)
        {
            slots.Add(new RuneSlot());
        }

        while (slots.Count > slotCount)
        {
            slots.RemoveAt(slots.Count - 1);
        }

        int firstLockedIndex = slotCount - lockedSlotCount;
        for (int i = 0; i < slots.Count; i++)
        {
            bool shouldLock = i >= firstLockedIndex;
            slots[i].locked = shouldLock;

            if (shouldLock)
                slots[i].runeSkill = null;
        }
    }

    void RecalculateLockedSlotCount()
    {
        int count = 0;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].locked)
                count++;
        }

        lockedSlotCount = count;
    }
}

[Serializable]
public class RuneSlot
{
    public bool locked;
    public RuneSkillData runeSkill;

    public bool HasRune => runeSkill != null;
}
