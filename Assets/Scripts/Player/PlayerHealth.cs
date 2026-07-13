using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    // 最大HP
    [Tooltip("最大HP")]
    [SerializeField]
    private int maxHP = 5;

    // 現在HP
    private int currentHP;

    // 無敵状態の有無
    private bool isInvincible = false;

    // 無敵時間用変数
    [Tooltip("無敵時間")]
    [SerializeField]
    private float invincibleTime = 1.0f;

    // Rigidbody2D
    private Rigidbody2D rb;

    // ノックバック力
    [Tooltip("ノックバック力")]
    [SerializeField]
    private float knockbackPower = 5f;

    // ノックバック（＝行動不能）中か。
    // 状態の実体はPlayerControllerのCurrentState(Knockback)で管理し、ここはその判定を公開するだけ。
    // 既存の各ゲート（移動・ジャンプ・攻撃前進など）がこのプロパティを参照している。
    public bool IsKnockback => playerController != null && playerController.CurrentState == PlayerState.Knockback;

    //ノックバック速度が乗っている時間（この間だけ吹っ飛び速度が加わる）
    [Tooltip("ノックバック速度が乗っている時間（この間だけ吹っ飛び速度が加わる）")]
    [SerializeField]
    private float knockbackDuration = 0.15f;

    // SpriteRenderer
    private SpriteRenderer spriteRenderer;

    // 点滅間隔
    [Tooltip("点滅間隔")]
    [SerializeField]
    private float blinkInterval = 0.05f;

    // このY座標より下に落ちたら死亡
    [Tooltip("このY座標より下に落ちたら死亡")]
    [SerializeField]
    private float fallDeathY = -10f;

    // HP表示テキスト
    [Tooltip("HP表示テキスト")]
    [SerializeField]
    private TMP_Text hpText;

    // 死亡済み判定
    private bool isDead = false;

    // Animator参照
    private Animator animator;

    // Player制御スクリプト参照
    private PlayerController playerController;

    // 死亡時吹っ飛び力
    [Tooltip("死亡時吹っ飛び力")]
    [SerializeField]
    private float deathKnockbackPower = 8f;

    // 死亡時上方向力
    [Tooltip("死亡時上方向力")]
    [SerializeField]
    private float deathUpPower = 3f;

    // HPUI参照
    [Tooltip("HPUI参照")]
    [SerializeField]
    private PlayerHPUI hpUI;

    // 被ダメージ時のエフェクトPrefab
    [Header("被ダメージエフェクト")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private GameObject guardEffectPrefab;

    // 被ダメージ/ガード時のSE
    [Header("被ダメージ/ガードSE")]
    [Tooltip("被ダメージ時の共通SE（Enemy種別を問わない）")]
    [SerializeField] private AudioClip damageSE;
    [Tooltip("ガード成功時のSE")]
    [SerializeField] private AudioClip guardSE;

    // ゲーム開始時に呼ばれる
    private void Start()
    {
        // HP初期化
        currentHP = maxHP;

        Debug.Log("Player HPを最大HPで初期化しました: " + currentHP);

        // GameData が存在する場合はそこから HP を読み込む（2シーン目以降）
        if (GameData.Instance != null && GameData.Instance.HasData)
        {
            currentHP = GameData.Instance.CurrentHP;
        }


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

    // 毎フレーム実行
    private void Update()
    {
        // 落下死亡チェック
        CheckFallDeath();
    }

    // HPを回復する処理
    public void Heal(int amount)
    {
        // 死亡済みなら処理しない
        if (isDead)
        {
            return;
        }

        // HPを回復量分増やす（最大HPを超えないようにする）
        currentHP = Mathf.Min(currentHP + amount, maxHP);

        // HP表示更新
        UpdateHPUI();

        Debug.Log("Playerは" + amount + " 回復した。現在HP: " + currentHP);
    }

    // ダメージを受ける処理
    public void TakeDamage(int damage, Vector2 enemyPosition, float knockbackForce)
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

        // ガード中ならダメージ軽減
        if (playerController.IsGuarding)
        {
            damage = Mathf.Max(1, Mathf.CeilToInt(damage * 0.5f));

            Debug.Log("ガード成功");
        }

        // 被ダメージエフェクトを発生させる
        SpawnHitEffect();


        // HPをダメージで減少させる
        currentHP -= damage;

        // HP表示更新
        UpdateHPUI();

        Debug.Log("Playerは" + damage + " のダメージを受けた");
        Debug.Log("現在HP: " + currentHP);

        // HP0以下なら死亡（ノックバック状態にはせず、そのまま死亡演出へ移る）
        if (currentHP <= 0)
        {
            // Enemy方向へ向き直る
            playerController.FaceEnemy(enemyPosition);
            // 死亡処理
            Die(enemyPosition);
            return;
        }

        // ガード中はノックバック・無敵を発生させない
        if (playerController.IsGuarding)
        {
            return;
        }

        // 被弾時は敵の方を向く（後方へ吹っ飛ぶリアクションを自然に見せるため。不要なら次の1行を削除）
        playerController.FaceEnemy(enemyPosition);

        // ノックバック方向計算（normalizeで方向の+-が定まる。横方向中心に少し上向き）
        float direction = Mathf.Sign(transform.position.x - enemyPosition.x);
        Vector2 knockbackDirection = new Vector2(direction, 0.2f).normalized;

        // ノックバック速度を与える（物理的な吹っ飛び）
        StartCoroutine(KnockbackVelocityCoroutine(knockbackDirection, knockbackForce));

        // Knockback状態へ入る（専用アニメ再生＋行動不能。解除はアニメ末尾のEndKnockback / セーフティ）
        playerController.EnterKnockback();

        // 無敵＋点滅開始
        StartCoroutine(InvincibleCoroutine());
    }
    // 被ダメージエフェクトをPlayerの位置に出す
    private void SpawnHitEffect()
    {
        // ガード中かどうかでPrefabとSEを切り替える
        GameObject prefab = playerController.IsGuarding ? guardEffectPrefab : hitEffectPrefab;
        AudioClip  se      = playerController.IsGuarding ? guardSE          : damageSE;

        // SE再生
        SoundManager.Instance?.PlaySE(se);

        // nullチェック
        if (prefab == null)
        {
            return;
        }
        // Playerの位置にエフェクトを出す
        Instantiate(prefab, transform.position, Quaternion.identity);
    }
    // HP0時処理
    private void Die(Vector2 enemyPosition)
    {
        Debug.Log("Playerは死んでしまった");


        // 死亡済みにする
        isDead = true;

        // 実行中Coroutine停止
        StopAllCoroutines();

        // 点滅中に死亡した場合、
        // Spriteが非表示状態で止まることがあるため強制表示
        spriteRenderer.enabled = true;

        // 死亡吹っ飛び実行
        DeathKnockback(enemyPosition);

        // 空気抵抗を増やして減速
        rb.linearDamping = 3f;

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

        /*// 完全停止
        rb.linearVelocity = Vector2.zero;

        // 重力停止
        rb.gravityScale = 0f;*/

        // 当たり判定停止
        //GetComponent<Collider2D>().enabled = false;
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
        // =========================
        // ① テキスト表示（既存維持）
        // =========================
        // hpTextがアタッチされている場合のみテキスト更新
        hpText.text = "HP : " + currentHP;

        // =========================
        // ② HPバー更新（追加）
        // =========================
        // hpUIがアタッチされている場合のみHPバー更新
        if (hpUI != null)
        {
            // HPバー更新
            hpUI.SetHP(currentHP, maxHP);
        }
    }

    // ノックバックの「物理的な吹っ飛び速度」だけを担当するコルーチン。
    // 行動不能State（Knockback）の開始/終了はPlayerController側が管理する。
    private IEnumerator KnockbackVelocityCoroutine(Vector2 direction, float knockbackForce)
    {
        // ノックバック速度を与える
        rb.linearVelocity = direction * knockbackForce;

        // 吹っ飛び速度が乗っている時間だけ待つ
        yield return new WaitForSeconds(knockbackDuration);

        // 速度を止める（以降はKnockback Stateのまま静止。State解除はアニメ末尾のEndKnockbackで行う）
        rb.linearVelocity = Vector2.zero;
    }

    // 死亡時の大きな吹っ飛び処理
    private void DeathKnockback(Vector2 enemyPosition)
    {
        // 敵の位置から吹っ飛ぶ方向を決める
        float direction = Mathf.Sign(transform.position.x - enemyPosition.x);

        // 吹っ飛び方向作成
        Vector2 force = new Vector2(direction * deathKnockbackPower, deathUpPower);

        // 一度速度リセット
        rb.linearVelocity = Vector2.zero;

        // 強い力を加える
        rb.AddForce(force, ForceMode2D.Impulse);
    }

    // 落下死亡判定
    private void CheckFallDeath()
    {
        // すでに死亡済みなら処理しない
        if (isDead)
        {
            return;
        }

        // 一定Y座標より下へ落ちたら死亡
        if (transform.position.y < fallDeathY)
        {
            Debug.Log("落下死亡");

            // 落下死実行
            Die(transform.position);
        }
    }

    /// <summary>
    /// シーン遷移時に HP を GameData へ保存する
    /// </summary>
    private void OnDestroy()
    {
        if (GameData.Instance != null)
        {
            GameData.Instance.SaveHP(currentHP, maxHP);
        }
    }

}