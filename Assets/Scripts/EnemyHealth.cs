using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
public class EnemyHealth : MonoBehaviour
{
    // 最大HP
    [SerializeField]
    private int maxHP = 10;

    // ノックバック力
    [SerializeField]
    private float knockbackForce = 8f;

    // 現在HP
    private int currentHP;

    // HPバー参照
    private EnemyHPBar enemyHPBar;

    // SpriteRenderer参照
    private SpriteRenderer spriteRenderer;

    // Rigidbody2D参照
    private Rigidbody2D rb;

    // 現在HP取得用
    public int CurrentHP => currentHP;

    // 最大HP取得用
    public int MaxHP => maxHP;

    // ノックバック中か
    public bool IsKnockback { get; private set; }

    // 打ち上げ中か
    public bool IsLaunched { get; private set; }

    // 死亡中か
    private bool isDead;

    // 重さレベル（0:軽い, 1:普通, 2:重い）
    [SerializeField]
    private int weightLevel = 0;

    // 重さレベルに応じたノックバック倍率
    [Header("Launch Multiplier")]

    // 軽い敵はノックバックが大きく、重い敵は小さくなるように設定
    [SerializeField]
    private float weight0Multiplier = 1.0f;

    // 普通の敵はノックバックが通常通り
    [SerializeField]
    private float weight1Multiplier = 0.5f;
    // 重い敵はノックバックが小さくなるように設定
    [SerializeField]
    private float weight2Multiplier = 0.0f;

    // 着地判定用
    [SerializeField]
    private Transform landingCheck;

    // 着地判定半径
    [SerializeField]
    private float landingCheckRadius = 0.15f;

    // GroundLayer
    [SerializeField]
    private LayerMask groundLayer;

    private void Awake()
    {
        // 初期HP設定
        currentHP = maxHP;

        // 子オブジェクトからHPバー取得
        enemyHPBar = GetComponentInChildren<EnemyHPBar>();

        // SpriteRenderer取得
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Rigidbody2D取得
        rb = GetComponent<Rigidbody2D>();
    }

    // 着地判定
    private void Update()
    {
        CheckLanding();
    }


    // ダメージ受信

    public void TakeDamage(int damage, Transform attacker, float knockbackPower, float launchPower)
    {
        // 死亡中なら処理しない
        if (isDead) return;

        // 現在HP減少
        currentHP -= damage;

        // 攻撃方向判定 Player(与ダメ側)の位置とEnemy(被ダメ側)の正負を判定
        float direction = transform.position.x - attacker.position.x;

        // ノックバック開始
        IsKnockback = true;

        // 左右方向へノックバック Playerの位置との正負方向にノックバックさせる
        rb.AddForce(new Vector2(direction > 0 ? 1 : -1, 0) * knockbackPower, ForceMode2D.Impulse);

        // ノックバック時間開始
        StartCoroutine(KnockbackCoroutine());

        // 被ダメ点滅開始
        StartCoroutine(FlashCoroutine());

        // HPバー更新
        if (enemyHPBar != null)
        {
            enemyHPBar.UpdateHPBar();
        }

        // 重さレベルに応じたノックバック倍率を取得
        float launchMultiplier = 1f;

        switch (weightLevel)
        {
            // 軽い敵はノックバックが大きく、重い敵は小さくなるように設定
            case 0:
                launchMultiplier = weight0Multiplier;
                break;
            // 普通の敵はノックバックが通常通り
            case 1:
                launchMultiplier = weight1Multiplier;
                break;
            // 重い敵はノックバックが小さくなるように設定
            case 2:
                launchMultiplier = weight2Multiplier;
                break;
        }
        // ノックバック力に重さレベルに応じた倍率を掛ける
        float finalLaunchPower = launchPower * launchMultiplier;

        // 打ち上げ攻撃なら打ち上げ状態にする
        if (finalLaunchPower > 0f)
        {
            // 打ち上げ状態に切替
            IsLaunched = true;
            // 打ち上げ力を加える
            rb.AddForce(Vector2.up * finalLaunchPower, ForceMode2D.Impulse);
        }
        // 浮かせ力を加える
        if (finalLaunchPower > 0f)
        {
            rb.AddForce(Vector2.up * finalLaunchPower, ForceMode2D.Impulse);
        }

        Debug.Log(gameObject.name + " Weight=" + weightLevel + " Multiplier=" + launchMultiplier + " Launch=" + finalLaunchPower);
        // ダメージログ
        Debug.Log(gameObject.name + " に " + damage + " ダメージ");
        Debug.Log("Enemyの残HP : " + currentHP);
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

        // 死亡状態
        isDead = true;

        // AI停止
        EnemyAI enemyAI = GetComponent<EnemyAI>();

        if (enemyAI != null)
        {
            enemyAI.enabled = false;
        }

        // Collider無効化
        Collider2D col = GetComponent<Collider2D>();

        if (col != null)
        {
            col.enabled = false;
        }

        // 死亡演出開始
        StartCoroutine(DeathCoroutine());
    }

    // 被ダメ点滅処理
    private IEnumerator FlashCoroutine()
    {
        for (int i = 0; i < 3; i++)
        {
            // 赤色へ変更
            spriteRenderer.color = Color.white;

            // 少し待機
            yield return new WaitForSeconds(0.08f);

            // 白色へ戻す
            spriteRenderer.color = Color.red;

            // 少し待機
            yield return new WaitForSeconds(0.08f);
        }
    }

    // ノックバック時間管理
    private IEnumerator KnockbackCoroutine()
    {
        // 少し待機
        yield return new WaitForSeconds(0.4f);

        // ノックバック終了
        IsKnockback = false;
    }

    // 死亡演出
    private IEnumerator DeathCoroutine()
    {
        // 5回点滅
        for (int i = 0; i < 5; i++)
        {
            // 非表示
            spriteRenderer.enabled = false;

            // 少し待機
            yield return new WaitForSeconds(0.08f);

            // 表示
            spriteRenderer.enabled = true;

            // 少し待機
            yield return new WaitForSeconds(0.08f);
        }

        // Boss-1ならクリア画面へ
        if (gameObject.name.Contains("Boss-1"))
        {
            SceneManager.LoadScene("ClearScene");
        }
        else
        {
            // 通常Enemyは削除
            Destroy(gameObject);
        }
    }

    // 着地判定
    private void CheckLanding()
    {
        // 打ち上げ中でないなら終了
        if (!IsLaunched)
        {
            return;
        }
        // 着地判定
        bool isGrounded =
            Physics2D.OverlapCircle(
                landingCheck.position,
                landingCheckRadius,
                groundLayer);

        // 着地したら解除
        if (isGrounded)
        {
            IsLaunched = false;
        }
    }

}