using UnityEngine;

public static class CombatSystem
{
    public static int CalculatePlayerBaseAttackDamage(PlayerController player, int sourceDamage)
    {
        int runtimeDamage = player != null ? player.RuntimeDamage : 0;
        return Mathf.Max(0, sourceDamage + runtimeDamage);
    }

    public static int CalculateCriticalDamage(PlayerController player, int baseDamage)
    {
        int resolvedDamage = Mathf.Max(0, baseDamage);

        if (!RollCriticalHit(player))
            return resolvedDamage;

        float critMultiplier = player != null ? Mathf.Max(1f, player.RuntimeCritDamage) : 1f;
        return Mathf.Max(0, Mathf.RoundToInt(resolvedDamage * critMultiplier));
    }

    public static int CalculateFinalPlayerAttackDamage(PlayerController player, int sourceDamage)
    {
        int baseDamage = CalculatePlayerBaseAttackDamage(player, sourceDamage);
        return CalculateCriticalDamage(player, baseDamage);
    }

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
