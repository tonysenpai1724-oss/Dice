public class DamageAllEnemiesEffect : GameEffect
{
    EnemyManager enemyManager;

    public override void Initialize(EffectManager effectManager, object owner)
    {
        base.Initialize(effectManager, owner);
        enemyManager = owner as EnemyManager;
    }

    public override void Dispose()
    {
        enemyManager = null;
        base.Dispose();
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

            enemy.TakeDamage(amount);
        }
    }
}
