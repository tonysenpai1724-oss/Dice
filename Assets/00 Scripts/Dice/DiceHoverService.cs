using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class DiceHoverService
{
    readonly Func<bool> isPaused;
    readonly Action<Dice> showDiceDetails;
    readonly Action hideDiceDetails;

    Dice currentHover;

    public DiceHoverService(
        Func<bool> isPaused,
        Action<Dice> showDiceDetails,
        Action hideDiceDetails)
    {
        this.isPaused = isPaused;
        this.showDiceDetails = showDiceDetails;
        this.hideDiceDetails = hideDiceDetails;
    }

    public void UpdateHover()
    {
        if (isPaused != null && isPaused())
            return;

        if (Mouse.current == null || Camera.main == null)
        {
            ClearHover();
            return;
        }

        Dice hitDice = FindHoveredDice(Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue()));
        if (currentHover == hitDice)
            return;

        SetCurrentHover(hitDice);
    }

    public void ClearHover()
    {
        SetCurrentHover(null);
    }

    Dice FindHoveredDice(Ray ray)
    {
        Dice hitDice = null;
        float nearestDistance = float.MaxValue;

        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            Mathf.Infinity,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null || hit.distance >= nearestDistance)
                continue;

            Dice dice = hit.collider.GetComponentInParent<Dice>();
            if (!CanHover(dice))
                continue;

            hitDice = dice;
            nearestDistance = hit.distance;
        }

        return hitDice;
    }

    static bool CanHover(Dice dice)
    {
        if (dice == null || !dice.gameObject.activeInHierarchy)
            return false;

        return dice.state != DiceState.Merging && dice.state != DiceState.FlyingCombo;
    }

    void SetCurrentHover(Dice nextHover)
    {
        if (currentHover != null)
            currentHover.SetHovered(false);

        currentHover = nextHover;

        if (currentHover != null)
        {
            currentHover.SetHovered(true);
            showDiceDetails?.Invoke(currentHover);
            return;
        }

        hideDiceDetails?.Invoke();
    }
}
