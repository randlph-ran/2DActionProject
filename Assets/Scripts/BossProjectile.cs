using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    // =========================
    // 移動設定
    // =========================

    // 飛行速度
    [Tooltip("飛行速度")]
    [SerializeField]
    private float moveSpeed = 8f;

    // =========================
    // 攻撃設定
    // =========================

    // ダメージ量
    [Tooltip("ダメージ量")]
    [SerializeField]
    private int damage = 1;

    // ノックバック力
    [Tooltip("ノックバック力")]
    [SerializeField]
    private float knockbackForce = 6f;

    // =========================
    // 自動削除設定
    // =========================

    // 生存時間
    [Tooltip("生存時間")]
    [SerializeField]
    private float lifeTime = 5f;

    // Rigidbody2D
    private Rigidbody2D rb;

    // 発射方向
    private Vector2 moveDirection;

    // 初期化
    private void Awake()
    {
        // Rigidbody2D取得
        rb = GetComponent<Rigidbody2D>();
    }

    // ゲーム開始時
    private void Start()
    {
        // 一定時間後にProjectile削除
        Destroy(gameObject, lifeTime);
    }

    // 発射方向設定
    // Boss側から呼ぶ
    public void SetDirection(Vector2 direction)
    {
        // 正規化して方向だけ取り出す
        moveDirection = direction.normalized;

        // Projectile移動開始
        rb.linearVelocity = moveDirection * moveSpeed;
    }

    // Trigger接触時
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Player接触確認
        if (collision.CompareTag("Player"))
        {
            // PlayerHealth取得
            PlayerHealth playerHealth =
                collision.GetComponent<PlayerHealth>();

            // PlayerHealth存在確認
            if (playerHealth != null)
            {
                // ダメージ処理
                playerHealth.TakeDamage(
                    damage,
                    transform.position,
                    knockbackForce
                );
            }

            // Projectile削除
            Destroy(gameObject);
        }
    }
}
