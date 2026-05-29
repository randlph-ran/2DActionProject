using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    // 移動速度設定
    [SerializeField]
    private float moveSpeed = 2f;

    // 右向きかどうか
    private bool isFacingRight = true;

    // Rigidbody2D
    private Rigidbody2D rb;

    // Animator参照
    private Animator animator;

    // EnemyHealth参照
    private EnemyHealth enemyHealth;

    // Player位置
    private Transform playerTransform;

    // 追尾開始距離　指定距離以内なら追尾
    [SerializeField]
    private float chaseDistance = 5f;

    // 攻撃開始距離
    [SerializeField]
    private float attackDistance = 1.5f;

    // 次攻撃までの間隔
    [SerializeField]
    private float attackCooldown = 2f;

    // 次攻撃可能時間
    private float nextAttackTime;

    // 攻撃判定位置
    [SerializeField]
    private Transform attackPoint;

    // 攻撃範囲
    [SerializeField]
    private float attackRadius = 0.8f;

    // PlayerLayer
    [SerializeField]
    private LayerMask playerLayer;

    // 壁確認位置
    // 前方の壁を調べるRayの開始地点
    [SerializeField]
    private Transform wallCheck;

    // 地面確認位置
    // 前方の足元を調べるRayの開始地点
    [SerializeField]
    private Transform groundCheck;

    // Rayの長さ
    // 数値を大きくすると遠くまで検知する
    [SerializeField]
    private float checkDistance = 0.3f;

    // 地面用Layer
    // GroundLayerだけを検知する
    [SerializeField]
    private LayerMask groundLayer;

    // 攻撃力
    [SerializeField]
    private int attackDamage = 1;

    // 初期化
    private void Awake()
    {
        // Rigidbody2D取得
        rb = GetComponent<Rigidbody2D>();

        //Scene内のTagがPlayerのものを探して入れる
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;

        // EnemyHealth取得
        enemyHealth = GetComponent<EnemyHealth>();

        // Animator取得
        animator = GetComponent<Animator>();

        Debug.Log(animator.runtimeAnimatorController.name);
    }

    // 物理更新
    private void FixedUpdate()
    {
        // ノックバック中は移動停止
        if (enemyHealth.IsKnockback)
        {
            return;
        }

        // Playerとの距離 Enemyの位置とPlayerの位置を測って入れる
        float distance = Vector2.Distance(transform.position, playerTransform.position);

        // 移動方向
        float moveDirection;

        // 攻撃距離内なら停止
        if (distance <= attackDistance)
        {
            // 移動停止
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

            // 攻撃可能なら攻撃
            if (Time.time >= nextAttackTime) Attack();

            return;
        }

        // Playerが近いなら追尾
        if (distance <= chaseDistance)
        {
            // Playerが右にいるか　Playerが右なら1　左なら - 1
            moveDirection = playerTransform.position.x > transform.position.x ? 1f : -1f;

            // Playerが右にいるか
            float targetDirection = playerTransform.position.x > transform.position.x ? 1f : -1f;

            // 向き違うなら反転
            if ((targetDirection > 0 && !isFacingRight) || (targetDirection < 0 && isFacingRight))
            {
                Flip();
            }

            // 移動方向
            moveDirection = isFacingRight ? 1f : -1f;
        }
        else
        {
            // 通常巡回　向きに合わせて巡回
            moveDirection = isFacingRight ? 1f : -1f;

            // ■ Playerを追っていない時だけ
            // 壁と崖を確認する
            CheckWallAndGround();
        }

        // 移動
        rb.linearVelocity = new Vector2(moveDirection * moveSpeed, rb.linearVelocity.y);

        // 横方向へ少しでも移動しているならtrue
        bool isMoving = Mathf.Abs(rb.linearVelocity.x) > 0.1f;

        // Animatorへ移動状態を送る
        animator.SetBool("isMoving", isMoving);

    }

    // 壁と地面を確認する処理
    private void CheckWallAndGround()
    {
        // 現在向いている方向を決める
        // 右向きなら右、左向きなら左
        Vector2 direction = isFacingRight ? Vector2.right : Vector2.left;

        // 前方向へRayを飛ばして壁確認
        RaycastHit2D wallHit = Physics2D.Raycast(
            wallCheck.position, // Ray開始位置
            direction,          // Ray方向
            checkDistance,      // Ray長さ
            groundLayer);         // 検知するLayer

        // 足元から下方向へRayを飛ばして地面確認
        RaycastHit2D groundHit = Physics2D.Raycast(
            groundCheck.position, // Ray開始位置
            Vector2.down,         // 下方向
            checkDistance,        // Ray長さ
            groundLayer);         // 検知するLayer

        // 壁に当たった場合
        bool hitWall = wallHit.collider != null;

        // 地面が無い場合
        bool noGround = groundHit.collider == null;

        // 壁がある または 地面が無いなら反転
        if (hitWall || noGround)
        {
            Debug.Log(hitWall + " 壁があったよ" + noGround + "もしくは地面がないよ");
            Flip();
        }
    }

    // 向き変更
    private void Flip()
    {
        // 向き反転
        isFacingRight = !isFacingRight;

        // Scale取得
        Vector3 scale = transform.localScale;

        // X反転
        scale.x *= -1;

        // Scale反映
        transform.localScale = scale;
    }

    // 攻撃処理
    private void Attack()
    {
        // Attackアニメ開始
        animator.SetTrigger("Attack");

        // 次回攻撃時間
        nextAttackTime = Time.time + attackCooldown;
    }

    // Gizmo表示
    private void OnDrawGizmos()
    {
        // AttackPoint未設定なら終了
        if (attackPoint == null) return;

        // 表示色は赤
        Gizmos.color = Color.red;

        // 攻撃範囲表示
        Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }

    // 実際にダメージを与える処理
    // Animation Eventから呼ばれる
    public void DealDamage()
    {
        // 攻撃範囲内のPlayer取得
        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, playerLayer);

        Debug.Log("Hitのカウント距離 : " + hitPlayers.Length);

        // 範囲内Player全員へ処理
        foreach (Collider2D player in hitPlayers)
        {
            // PlayerHealth取得
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();

            // PlayerHealthが存在するなら
            if (playerHealth != null)
            {
                // ダメージ処理
                playerHealth.TakeDamage(attackDamage, transform.position);
            }
        }
    }
}