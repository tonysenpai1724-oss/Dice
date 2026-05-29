using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerController : MonoBehaviour
{
    public int hp;
    public int currentHp;
    public TextMeshProUGUI hpText;
    void Start()
    {
        SetHp(50, 50);
        if (hpText != null)
        {
            hpText.text = currentHp.ToString() + "/" + hp.ToString();
        }
    }
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
        hpText.text = currentHp.ToString() + "/" + hp.ToString();


        if (currentHp <= 0)
        {
            Die();
        }


    }
    void Die()
    {
        Debug.Log("Player Died");
        if (GameplayManager.Instance != null)
            GameplayManager.Instance.EndGame(false);
    }
}


