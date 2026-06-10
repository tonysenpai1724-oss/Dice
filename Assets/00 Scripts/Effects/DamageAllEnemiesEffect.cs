public class DamageAllEnemiesEffect : GameEffect
{
    EnemyManager enemyManager;

    protected override void Awake()
    {
        enemyManager = GetComponent<EnemyManager>();
    }

    public void Apply(int amount)
    {
        if (enemyManager == null || amount <= 0)
            return;

        for (int i = 0; i < enemyManager.enemies.Count; i++)
        {
            Enemy enemy = enemyManager.enemies[i];
            if (enemy == null || !enemy.IsAlive())
                continue;

            enemy.OnTakeDamage(amount);
        }
    }
}
