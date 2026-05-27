
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int hp;

    public void TakeDamage(int amount)
    {
        hp -= amount;

        if (hp <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Destroy(gameObject);
    }
}