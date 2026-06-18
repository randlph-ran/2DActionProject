using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    // ダメージ量
    [Tooltip("ダメージ量")]
    [SerializeField]
    private int damage = 1;

    // ノックバック力
    [Tooltip("ノックバック力")]
    [SerializeField]
    private float knockbackForce = 5f;

    // Collider接触時に呼ばれる
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Playerと接触したか確認
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Playerに接触");

            // PlayerHealth取得
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();

            // PlayerHealth存在確認
            if (playerHealth != null)
            {
                // ダメージ処理
                playerHealth.TakeDamage(damage, transform.position, knockbackForce);
            }
        }
    }
}