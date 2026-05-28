using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerController : MonoBehaviour
{
    public int hp;
    public int currentHp;
    public void SetHp(int hp, int currentHp)
    {
        this.hp = hp;
        this.currentHp = currentHp;
    }
    public void Heal(int amount)
    {
        currentHp += amount;
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
        Debug.Log("Player Died");
    }
}


