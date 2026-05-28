
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int hp;
    public int currentHp;
    public int damage;

    public void SetHp(int hp, int damage)
    {
        this.hp = hp;
        this.damage = damage;
    }
    public void TakeDamage(int amount)
    {
        currentHp -= amount;

        if (currentHp <= 0)
        {
            Die();
        }
    }
    void Die()
    {
        Debug.Log("Enemy Died");
    }
}

public class EnemyData : MonoBehaviour
{
    public int hp;
    public int damage;


}