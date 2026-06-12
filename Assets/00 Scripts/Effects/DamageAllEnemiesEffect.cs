using UnityEngine;

public class DamageAllEnemiesEffect : GameEffect
{
    EnemyManager enemyManager;

    protected override void Awake()
    {
        enemyManager = EnemyManager.Instance;

    }

    public void Apply(int amount)
    {
        if (enemyManager == null || amount <= 0)
        {
            RemoveSelf();
            return;
        }
        Debug.Log("DamageAllEnemiesEffect" + amount);

        for (int i = 0; i < enemyManager.enemies.Count; i++)
        {
            Enemy enemy = enemyManager.enemies[i];
            if (enemy == null || !enemy.IsAlive())
                continue;

            enemy.OnTakeDamage(amount);
        }

        RemoveSelf();
    }
}
