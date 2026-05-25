using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    private float knockbackPower = 1f;

    // ノックバック中
    public bool IsKnockback { get; private set; }

    //ノックバックを短時間だけ発生
    [SerializeField]
    private float knockbackDuration = 0.15f;

    // SpriteRenderer
    private SpriteRenderer spriteRenderer;

    // 点滅間隔
    [SerializeField]
    private float blinkInterval = 0.05f;

    // HP表示テキスト
    [SerializeField]
    private TMP_Text hpText;

    // 死亡済み判定
    private bool isDead = false;

    // Animator参照
    private Animator animator;

    // Player制御スクリプト参照
    private PlayerController playerController;

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

        // HP表示更新
        UpdateHPUI();

        // Animator取得
        animator = GetComponent<Animator>();

        // PlayerController取得
        playerController = GetComponent<PlayerController>();
    }

    // ダメージを受ける処理
    public void TakeDamage(int damage, Vector2 enemyPosition)
    {
        // 死亡済みなら処理しない
        if (isDead)
        {
            return;
        }
        // 無敵中ならダメージ無効
        if (isInvincible)
        {
            Debug.Log("無敵中だぜ");

            return;
        }
        // HPをダメージで減少させる
        currentHP -= damage;

        // HP表示更新
        UpdateHPUI();

        Debug.Log("Playerは" + damage + " のダメージを受けた");
        Debug.Log("現在HP: " + currentHP);

        // ノックバック方向計算
        //normalizedすると 長さ1 になる＝方向の+-が定まる
        // 敵が左右どちらにいるか判定
        float direction = Mathf.Sign(transform.position.x - enemyPosition.x);

        // 横方向だけノックバック
        Vector2 knockbackDirection = new Vector2(direction, 0.2f).normalized;

        // ノックバック状態に切替
        IsKnockback = true;

        //固定ノックバック
        StartCoroutine(KnockbackCoroutine(knockbackDirection));


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


        // 死亡済みにする
        isDead = true;

        // 実行中Coroutine停止
        StopAllCoroutines();

        // 死亡アニメ再生
        animator.SetTrigger("Die");

        // Player操作停止
        DisablePlayer();

        // GameOverSceneへ移動するCoroutine開始
        StartCoroutine(LoadGameOver());
    }

    // Playerの操作停止
    private void DisablePlayer()
    {
        // 移動スクリプト停止
        playerController.enabled = false;

        // Rigidbody取得
        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        // 完全停止
        rb.linearVelocity = Vector2.zero;

        // 動かないようにする
        rb.bodyType = RigidbodyType2D.Static;
    }

    // 数秒待ってGameOverSceneへ移動
    private IEnumerator LoadGameOver()
    {
        // 2秒待機
        yield return new WaitForSeconds(2f);

        // GameOverSceneへ移動
        SceneManager.LoadScene("GameOver");
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

        Debug.Log("無敵終了");
    }

    // HP表示更新
    private void UpdateHPUI()
    {
        // HPテキスト更新
        hpText.text = "HP : " + currentHP;
    }

    private System.Collections.IEnumerator KnockbackCoroutine(Vector2 direction)
    {
        // ノックバック開始
        IsKnockback = true;

        // ノックバック速度
        rb.linearVelocity = direction * knockbackPower;

        // 少しだけ吹っ飛ぶ
        yield return new WaitForSeconds(knockbackDuration);

        // 停止
        rb.linearVelocity = Vector2.zero;

        // ノックバック終了
        IsKnockback = false;
    }
}