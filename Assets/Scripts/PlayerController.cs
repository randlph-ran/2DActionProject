using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Animator
    private Animator animator;

    // 現在のコンボ段階（1〜3）
    private int comboStep = 0;

    // 最後に攻撃した時間
    private float lastAttackTime;

    // コンボ受付時間
    [SerializeField]
    private float comboResetTime = 0.8f;

    // Rigidbody2D
    private Rigidbody2D rb;

    // PlayerHealth
    private PlayerHealth playerHealth;

    // 左右入力
    private float moveInput;

    // 移動速度
    [SerializeField]
    private float moveSpeed = 5f;

    // 攻撃中判定
    private bool isAttacking = false;

    // 次コンボへ進めるか
    private bool canNextCombo = false;

    // 攻撃継続時間
    //[SerializeField]
    //private float attackDuration = 0.3f;

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
    private float groundCheckRadius = 0.35f;

    // GroundLayer
    [SerializeField]
    private LayerMask groundLayer;


    // =========================
    // Attack1設定
    // =========================

    // Attack1の攻撃位置オフセット
    [SerializeField]
    private Vector2 attack1Offset;

    // Attack1の攻撃範囲
    [SerializeField]
    private float attack1Radius = 1.0f;


    // =========================
    // Attack2設定
    // =========================

    // Attack2の攻撃位置オフセット
    [SerializeField]
    private Vector2 attack2Offset;

    // Attack2の攻撃範囲
    [SerializeField]
    private float attack2Radius = 1.2f;


    // =========================
    // Attack3設定
    // =========================

    // Attack3の攻撃位置オフセット
    [SerializeField]
    private Vector2 attack3Offset;

    // Attack3の攻撃範囲
    [SerializeField]
    private float attack3Radius = 1.5f;


    // =========================
    // 現在使用中の攻撃情報
    // =========================

    // 現在の攻撃位置
    private Vector2 currentAttackOffset;

    // 現在の攻撃範囲
    private float currentAttackRadius;
    /*
    // 攻撃判定位置
    [SerializeField]
    private Transform attackPoint;

    // 攻撃範囲
    [SerializeField]
    private float attackRadius = 5f;*/

    [SerializeField]
    private int attackDM = 1;

    // 敵Layer
    [SerializeField]
    private LayerMask enemyLayer;

    // デバッグ用攻撃Gizmo表示フラグ
    private bool isAttackGizmoVisible = false;

    // 攻撃範囲を常時表示するか
    [SerializeField]
    private bool alwaysShowAttackGizmo = true;

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

        //Animator取得
        animator = GetComponent<Animator>();
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

        // 攻撃中は向き固定
        if (!isAttacking)
        {
            Flip();
        }

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
            // 攻撃中でないならAttack1開始
            if (!isAttacking)
            {
                HandleAttackInput();
            }

            // 攻撃中なら、次段受付時のみ許可
            else if (canNextCombo)
            {
                HandleAttackInput();

            }

            Debug.Log("攻撃開始");
            Debug.Log("現在コンボ段数：" + comboStep);
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

        // 攻撃中は横移動停止
        if (isAttacking)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

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

    // Enemy方向へ向き直る
    public void FaceEnemy(Vector2 enemyPosition)
    {
        // Enemyが右側にいるか
        bool enemyIsRight = enemyPosition.x > transform.position.x;

        // Enemyが右にいて、現在左向きなら反転
        if (enemyIsRight && !isFacingRight)
        {
            Turn();
        }

        // Enemyが左にいて、現在右向きなら反転
        else if (!enemyIsRight && isFacingRight)
        {
            Turn();
        }
    }

    // Scene上でGroundCheck確認用
    private void OnDrawGizmos()
    {
        // GroundCheck未設定なら終了
        if (groundCheck == null)
        {
            return;
        }

        // Gizmo色
        Gizmos.color = Color.red;

        // 向きによって攻撃位置反転
        Vector2 gizmoPosition = (Vector2)transform.position + new Vector2(currentAttackOffset.x * (isFacingRight ? 1 : -1), currentAttackOffset.y);

        // 攻撃範囲表示
        Gizmos.DrawWireSphere(gizmoPosition, currentAttackRadius);

        // 攻撃中 or 常時表示
        if (isAttackGizmoVisible || alwaysShowAttackGizmo)
        {
            // 攻撃範囲色
            Gizmos.color = Color.blue;

            // 攻撃範囲表示
            Gizmos.DrawWireSphere(
                gizmoPosition,
                currentAttackRadius
            );
        }
    }

    // 攻撃処理    
    public void Attack()
    {
        Debug.Log("アタック！");
        // 攻撃Gizmo表示開始
        StartCoroutine(ShowAttackGizmo());

        // 向きによって攻撃位置反転
        Vector2 attackPosition = (Vector2)transform.position + new Vector2(currentAttackOffset.x * (isFacingRight ? 1 : -1), currentAttackOffset.y);

        // 攻撃範囲内のEnemy取得
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(
            attackPosition,
            currentAttackRadius,
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

    /// <summary>
    /// コンボ段階管理    アニメ再生
    /// </summary>
    private void HandleAttackInput()
    {

        canNextCombo = false;

        comboStep++;

        if (comboStep > 3)
        {
            comboStep = 1;
        }

        // コンボ段階ごとの攻撃設定切替
        switch (comboStep)
        {
            // Attack1
            case 1:
                currentAttackOffset = attack1Offset;
                currentAttackRadius = attack1Radius;
                Debug.Log(currentAttackOffset);
                Debug.Log(currentAttackRadius);
                break;

            // Attack2
            case 2:
                currentAttackOffset = attack2Offset;
                currentAttackRadius = attack2Radius;
                Debug.Log(currentAttackOffset);
                Debug.Log(currentAttackRadius);
                break;

            // Attack3
            case 3:
                currentAttackOffset = attack3Offset;
                currentAttackRadius = attack3Radius;
                Debug.Log(currentAttackOffset);
                Debug.Log(currentAttackRadius);
                break;
        }

        animator.SetInteger("ComboStep", comboStep);

        if (comboStep == 1)
        {
            animator.SetTrigger("Attack");
        }

        isAttacking = true;
    }
    // 次コンボ入力を許可する
    // Animation Event から呼ばれる
    public void EnableNextCombo()
    {
        canNextCombo = true;

        Debug.Log("次コンボ受付開始");
    }
    public void EndAttack()
    {
        Debug.Log("EndAttack呼ばれた");

        // 遷移中なら終了処理しない
        if (animator.IsInTransition(0))
        {
            Debug.Log("遷移中なので終了スキップ");
            return;
        }

        isAttacking = false;

        canNextCombo = false;

        comboStep = 0;

        animator.SetInteger("ComboStep", 0);

        Debug.Log("受付終了");
        Debug.Log("Attack終了");
    }

}
