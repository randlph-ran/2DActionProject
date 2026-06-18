using UnityEngine;

/// <summary>
/// Projectile（飛び道具）
/// 
/// 【役割】
/// ・直進移動
/// ・寿命管理
/// ・IDamageableへダメージ転送
/// ・発射者以外にヒット
/// </summary>
public class Projectile : MonoBehaviour
{
    // =========================
    // Move Settings
    // =========================

    [Header("Move Settings")]
    [Tooltip("移動速度")]
    [SerializeField] private float moveSpeed = 10f;

    private Vector2 direction;
    private Rigidbody2D rb;

    // =========================
    // Damage Settings
    // =========================

    [Header("Damage Settings")]
    [Tooltip("ダメージ量（計算は行わずそのままIDamageableへ渡す）")]
    [SerializeField] private int damage = 1;

    [Tooltip("ノックバック強度（処理は受け側で解釈する）")]
    [SerializeField] private float knockbackPower = 3f;

    [Tooltip("打ち上げ強度（処理は受け側で解釈する）")]
    [SerializeField] private float launchPower = 1f;

    // =========================
    // Owner Settings
    // =========================

    [Header("Owner Settings")]
    [Tooltip("発射者（Player / Enemy / Bossなど）")]
    [SerializeField] private GameObject owner;

    // =========================
    // Lifetime Settings
    // =========================

    [Header("Lifetime Settings")]
    [Tooltip("存在時間（秒）")]
    [SerializeField] private float lifeTime = 3f;

    private float timer;
    private bool isInitialized;

    // =====================================================
    // Unity LifeCycle
    // =====================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }
    }

    private void FixedUpdate()
    {
        if (!isInitialized) return;

        Move();
        HandleLifetime();
    }

    // =====================================================
    // 初期化
    // =====================================================

    /// <summary>
    /// 発射時に必ず呼ぶ
    /// </summary>
    public void Initialize(
        Vector2 direction,
        int damage,
        float knockbackPower,
        float launchPower,
        GameObject owner
    )
    {
        this.direction = direction.normalized;

        // =====================================
        // 見た目を移動方向へ向ける
        // =====================================

        // directionから角度を計算
        float angle = Mathf.Atan2(this.direction.y, this.direction.x) * Mathf.Rad2Deg;

        // Projectileを回転
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // =====================================
        // ダメージ関連の初期化
        // =====================================
        this.damage = damage;
        this.knockbackPower = knockbackPower;
        this.launchPower = launchPower;
        this.owner = owner;

        isInitialized = true;

        if (rb != null)
        {
            rb.linearVelocity = this.direction * moveSpeed;
        }
    }

    // =====================================================
    // 移動
    // =====================================================

    /// <summary>
    /// 直進移動
    /// </summary>
    private void Move()
    {
        if (rb == null) return;

        rb.linearVelocity = direction * moveSpeed;
    }

    // =====================================================
    // 寿命
    // =====================================================

    /// <summary>
    /// lifeTimeで自動消滅
    /// </summary>
    private void HandleLifetime()
    {
        timer += Time.deltaTime;

        if (timer >= lifeTime)
        {
            Destroy(gameObject);
        }
    }

    // =====================================================
    // 当たり判定
    // =====================================================

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 初期化前は当たり判定を無効化
        if (!isInitialized) return;
        // 発射者には当たらないようにする
        if (owner != null && other.gameObject == owner)
            return;
        // IDamageableを持つオブジェクトにダメージを与える
        IDamageable target = other.GetComponent<IDamageable>();
        // ダメージ転送
        if (target != null)
        {
            // IDamageableのTakeDamageにダメージを転送
            target.TakeDamage(
                damage,
                owner != null ? owner.transform : transform,
                knockbackPower,
                launchPower,
                AttackType.Item
            );
            // ヒットしたら消える
            Destroy(gameObject);
        }
    }
}