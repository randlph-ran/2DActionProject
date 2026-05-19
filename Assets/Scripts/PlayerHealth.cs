using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    // 最大HP
    [SerializeField]
    private int maxHP = 5;

    // 現在HP
    private int currentHP;

    // 無敵状態の有無
    private bool isInvincible = false;

    // 無敵時間用変数
    [SerializeField]
    private float invincibleTime = 1.0f;

    // Rigidbody2D
    private Rigidbody2D rb;

    // ノックバック力
    [SerializeField]
    private float knockbackPower = 15f;

    // ノックバック中
    public bool IsKnockback { get; private set; }

    // SpriteRenderer
    private SpriteRenderer spriteRenderer;

    // 点滅間隔
    [SerializeField]
    private float blinkInterval = 0.05f;

    // ゲーム開始時に呼ばれる
    private void Start()
    {
        // HP初期化
        currentHP = maxHP;

        Debug.Log("Player HPを最大HPで初期化しました: " + currentHP);

        // Rigidbody2D取得
        rb = GetComponent<Rigidbody2D>();

        // SpriteRenderer取得　Playerの見た目にしているSpriteを取得
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // ダメージを受ける処理
    public void TakeDamage(int damage, Vector2 enemyPosition)
    {
        // 無敵中ならダメージ無効
        if (isInvincible)
        {
            Debug.Log("無敵中だぜ");

            return;
        }
        // HPをダメージで減少させる
        currentHP -= damage;

        Debug.Log("Playerは" + damage + " のダメージを受けた");
        Debug.Log("現在HP: " + currentHP);

        // ノックバック方向計算
        //normalizedすると 長さ1 になる＝方向の+-が定まる
        Vector2 knockbackDirection = (transform.position - (Vector3)enemyPosition).normalized;

        // ノックバック状態に切替
        IsKnockback = true;

        // 力を加える　ForceMode2D.Impulse＝瞬間的に強く押す
        rb.AddForce(knockbackDirection * knockbackPower, ForceMode2D.Impulse);

        StartCoroutine(InvincibleCoroutine());

        // HP0以下なら死亡
        if (currentHP <= 0)
        {
            Die();
        }
    }

    // HP0時処理
    private void Die()
    {
        Debug.Log("Playerは死んでしまった");

        // 仮で非表示
        gameObject.SetActive(false);
    }

    // 一定時間無敵状態にする
    private System.Collections.IEnumerator InvincibleCoroutine()
    {
        // 無敵ON
        isInvincible = true;

        Debug.Log("無敵開始");

        // ノックバック開始
        IsKnockback = true;

        // 経過時間
        float timer = 0f;

        // 無敵時間終わるまで点滅表示をループ
        while (timer < invincibleTime)
        {
            // 表示OFF
            spriteRenderer.enabled = false;

            // 点滅待機
            yield return new WaitForSeconds(blinkInterval);

            // 表示ON
            spriteRenderer.enabled = true;

            // 点滅待機
            yield return new WaitForSeconds(blinkInterval);

            // timerを加算して、条件(timer<invincibleTime)を満たすまでループさせる
            timer += blinkInterval * 2;
        }

        // 表示戻す
        spriteRenderer.enabled = true;

        // 無敵OFF
        isInvincible = false;

        // ノックバック終了
        IsKnockback = false;

        Debug.Log("無敵終了");
    }
}