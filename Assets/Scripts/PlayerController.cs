using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Rigidbody2D
    private Rigidbody2D rb;

    // PlayerHealth
    private PlayerHealth playerHealth;

    // 左右入力
    private float moveInput;

    // 移動速度
    [SerializeField]
    private float moveSpeed = 5f;

    // ジャンプ力
    [SerializeField]
    private float jumpPower = 12f;

    // 右向き判定
    private bool isFacingRight = true;

    // 接地判定
    private bool isGrounded;

    // 現在のジャンプ回数
    private int jumpCount = 0;

    // 最大ジャンプ回数
    [SerializeField]
    private int maxJumpCount = 2;

    // GroundCheck位置
    [SerializeField]
    private Transform groundCheck;

    // Ground判定半径
    [SerializeField]
    private float groundCheckRadius = 0.2f;

    // GroundLayer
    [SerializeField]
    private LayerMask groundLayer;

    // 攻撃判定位置
    [SerializeField]
    private Transform attackPoint;

    // 攻撃範囲
    [SerializeField]
    private float attackRadius = 1f;

    [SerializeField]
    private int attackDM = 1;

    // 敵Layer
    [SerializeField]
    private LayerMask enemyLayer;

    // デバッグ用攻撃Gizmo表示フラグ
    private bool isAttackGizmoVisible = false;

    // Gizmo表示時間
    [SerializeField]
    private float attackGizmoDuration = 0.2f;

    // ゲーム開始時に最初に呼ばれる
    private void Awake()
    {
        // Rigidbody2D取得
        rb = GetComponent<Rigidbody2D>();

        // PlayerHealth取得
        playerHealth = GetComponent<PlayerHealth>();
    }

    // 毎フレーム実行
    // 入力取得を行う
    private void Update()
    {
        // 左右入力 いったん旧入力システムで
        moveInput = Input.GetAxisRaw("Horizontal");

        // ノックバック中はジャンプ操作禁止
        if (playerHealth.IsKnockback)
        {
            return;
        }

        // 接地判定
        CheckGround();

        // 向き変更
        Flip();

        // ジャンプ入力
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log(jumpCount + "回目");
            // 最大回数未満ならジャンプ可能
            if (jumpCount < maxJumpCount)
            {
                Jump();
            }
        }

        // 攻撃入力
        if (Input.GetKeyDown(KeyCode.Z))
        {
            Debug.Log("攻撃開始");
            Attack();
        }
    }

    // 物理演算用
    private void FixedUpdate()
    {
        // ノックバック中は移動停止
        if (playerHealth.IsKnockback)
        {
            return;
        }

        // 左右移動
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    // ジャンプ処理    
    private void Jump()
    {
        Debug.Log("ジャンプ入力したよ");
        // Y方向速度リセット
        // 落下中でも一定ジャンプ力になる
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);

        // ジャンプ力を加える
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpPower);

        // ジャンプ回数加算
        jumpCount++;
    }
    private void CheckGround()
    {
        // 接地判定
        bool wasGrounded = isGrounded;

        //GroundCheck位置をPCの足元中心から円を作って、その円の範囲にGroundLayerが触れているか調べている
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );

        // 空中→接地になった瞬間
        if (!wasGrounded && isGrounded)
        {
            ResetJumpCount();
            Debug.Log(jumpCount);//リセット処理入ったかの確認
        }
    }

    // ジャンプ回数リセット
    private void ResetJumpCount()
    {
        jumpCount = 0;
    }

    // キャラクターの向きを変更する
    private void Flip()
    {
        // 左入力時、右向きなら反転
        if (moveInput < 0 && isFacingRight)
        {
            Turn();
        }

        // 右入力時、左向きなら反転
        else if (moveInput > 0 && !isFacingRight)
        {
            Turn();
        }
    }

    // 実際にSpriteを反転する
    private void Turn()
    {
        // 向き状態反転
        isFacingRight = !isFacingRight;

        // 現在Scale取得
        Vector3 scale = transform.localScale;

        // X方向反転
        scale.x *= -1;

        // Scale適用
        transform.localScale = scale;
    }

    // Scene上でGroundCheck確認用
    private void OnDrawGizmos()
    {
        // GroundCheck未設定なら終了
        if (groundCheck == null || attackPoint == null)
        {
            return;
        }

        // Gizmo色
        Gizmos.color = Color.red;

        // 円表示
        Gizmos.DrawWireSphere(
            groundCheck.position,
            groundCheckRadius
        );

        // 攻撃Gizmo表示中のみ
        if (isAttackGizmoVisible)
        {
            // 攻撃範囲色
            Gizmos.color = Color.blue;

            // 攻撃範囲表示
            Gizmos.DrawWireSphere(
                attackPoint.position,
                attackRadius
            );
        }
    }

    // 攻撃処理    
    private void Attack()
    {
        Debug.Log("アタック！");
        // 攻撃Gizmo表示開始
        StartCoroutine(ShowAttackGizmo());

        // 攻撃範囲内のEnemy取得
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(
            attackPoint.position,
            attackRadius,
            enemyLayer
        );

        // Enemy全処理
        foreach (Collider2D enemy in hitEnemies)
        {
            // EnemyHealth取得
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();

            // EnemyHealthが存在するなら
            if (enemyHealth != null)
            {
                // ダメージを与える
                enemyHealth.TakeDamage(attackDM, transform);
            }
        }
    }

    // 一定時間だけ攻撃Gizmo表示
    private System.Collections.IEnumerator ShowAttackGizmo()
    {
        // 表示ON
        isAttackGizmoVisible = true;

        // 指定時間待機
        yield return new WaitForSeconds(attackGizmoDuration);

        // 表示OFF
        isAttackGizmoVisible = false;
    }
}
