using UnityEngine;

public static class CombatSystem
{
    public static int CalculatePlayerBaseAttackDamage(PlayerController player, int sourceDamage)
    {
        int runtimeDamage = player != null ? player.RuntimeDamage : 0;
        return Mathf.Max(0, sourceDamage + runtimeDamage);
    }

    // public static int CalculateCriticalDamage(PlayerController player, int baseDamage)
    // {
    //     return CalculateCriticalDamage(player, baseDamage, out _);
    // }

    public static int CalculateCriticalDamage(PlayerController player, int baseDamage, out bool isCritical)
    {
        return CalculateCriticalDamage(player, baseDamage, null, out isCritical);
    }

    public static int CalculateCriticalDamage(PlayerController player, int baseDamage, DiceData diceData, out bool isCritical)
    {
        int resolvedDamage = Mathf.Max(0, baseDamage);
        isCritical = RollCriticalHit(player);

        if (!isCritical)
            return resolvedDamage;
        float critMultiplier = player != null ? Mathf.Max(1f, player.RuntimeCritDamage) : 1f;
        //return Mathf.Max(0, Mathf.RoundToInt(resolvedDamage * critMultiplier));
        return Mathf.Max(0, Mathf.RoundToInt(diceData.level + (diceData.level * critMultiplier)));

    }


    // public static int CalculateFinalPlayerAttackDamage(PlayerController player, int sourceDamage)
    // {
    //     return CalculateFinalPlayerAttackDamage(player, sourceDamage, out _);
    // }

    // public static int CalculateFinalPlayerAttackDamage(PlayerController player, int sourceDamage, out bool isCritical)
    // {
    //     int baseDamage = CalculatePlayerBaseAttackDamage(player, sourceDamage);
    //     return CalculateCriticalDamage(player, baseDamage, out isCritical);
    // }

    public static int ApplyDefenseToPlayer(PlayerController player, int incomingDamage)
    {
        int defense = player != null ? player.RuntimeDefense : 0;
        return Mathf.Max(0, incomingDamage - defense);
    }

    public static bool RollCriticalHit(PlayerController player)
    {
        float critChance = 0f;

        if (player != null)
            critChance = Mathf.Max(0f, player.RuntimeCritRate + player.RuntimeLuck);

        return Random.value <= critChance;
    }
}
