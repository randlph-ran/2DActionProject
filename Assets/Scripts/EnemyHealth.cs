using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    // 最大HP
    [SerializeField]
    private int maxHP = 3;

    // 現在HP
    private int currentHP;

    private void Start()
    {
        // 初期HP設定
        currentHP = maxHP;
    }

    // ダメージ受信

    public void TakeDamage(int damage)
    {
        // 現在HP減少
        currentHP -= damage;

        // ダメージログ
        Debug.Log(gameObject.name + " に " + damage + " ダメージ");

        // 現在HP確認
        if (currentHP <= 0)
        {
            Die();
        }
    }

    // 死亡処理
    private void Die()
    {
        Debug.Log(gameObject.name + " を撃破したぞ");

        // GameObject削除
        Destroy(gameObject);
    }
}