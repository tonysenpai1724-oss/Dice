using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    public EnemyType type;
    public int hp;
    public int currentHp;
    public int damage;
    public EnemyData data;
    public int distanceToPlayer;
    public int attackRange;
    public Image image;
    public TextMeshProUGUI hpText;

    public void Setup(EnemyData newData)
    {
        data = newData;
        type = data.type;
        SetHp(data.hp, data.damage);
        distanceToPlayer = Mathf.Max(0, data.startDistance);
        attackRange = data.type == EnemyType.Melee ? Mathf.Max(1, data.attackRange) : int.MaxValue;

        if (image == null)
            image = GetComponent<Image>();

        if (image != null)
        {
            image.sprite = data.sprite;
            image.enabled = data.sprite != null;
        }
        if (hpText != null)
        {
            hpText.text = currentHp.ToString() + "/" + hp.ToString();
        }
    }

    public void SetHp(int hp, int damage)
    {
        this.hp = hp;
        this.damage = damage;
        currentHp = hp;
    }

    public bool IsAlive()
    {
        return currentHp > 0;
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive())
            return;

        currentHp -= amount;
        hpText.text = currentHp.ToString() + "/" + hp.ToString();

        if (currentHp <= 0)
        {
            Die();
        }
    }

    public bool CanAttack()
    {
        if (!IsAlive())
            return false;

        if (type == EnemyType.Range)
            return true;

        return distanceToPlayer <= attackRange;
    }

    public void MoveTowardPlayer(int amount)
    {
        if (!IsAlive())
            return;

        distanceToPlayer = Mathf.Max(0, distanceToPlayer - amount);
    }

    void Die()
    {
        Debug.Log("Enemy Died");
        EnemyManager.Instance?.RemoveEnemy(this);
    }
}

[CreateAssetMenu(menuName = "RuneDice/Enemy")]
public class EnemyData : ScriptableObject
{
    public int hp;
    public int damage;
    public EnemyType type;
    public Sprite sprite;
    public int startDistance = 3;
    public int attackRange = 1;

}
public enum EnemyType
{
    Normal,
    Melee,
    Range,
}
