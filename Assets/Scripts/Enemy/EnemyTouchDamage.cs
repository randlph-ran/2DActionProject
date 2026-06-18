using UnityEngine;

public class EnemyTouchDamage : MonoBehaviour
{
    // 接触ダメージ共通値
    private const int TOUCH_DAMAGE = 1;

    // 接触ノックバック共通値
    private const float TOUCH_KNOCKBACK = 2.5f;

    // EnemyAI取得
    private EnemyAI enemyAI;

    private void Awake()
    {
        // EnemyAIコンポーネントを取得
        enemyAI = GetComponentInParent<EnemyAI>();
    }
    // プレイヤーが接触したときの処理
    private void OnTriggerEnter2D(Collider2D other)
    {
        // プレイヤー以外のオブジェクトは無視
        if (!other.CompareTag("Player"))
        {
            return;
        }
        // PlayerHealthコンポーネントを取得
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        // PlayerHealthが見つからない場合は処理を終了
        if (playerHealth == null)
        {
            return;
        }
        // プレイヤーにダメージとノックバックを与える
        playerHealth.TakeDamage(TOUCH_DAMAGE, enemyAI.transform.position, TOUCH_KNOCKBACK);
    }
}