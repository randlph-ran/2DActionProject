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

    // =========================
    // 近接攻撃設定
    // =========================

    // 近接攻撃距離
    [SerializeField]
    private float meleeAttackDistance = 1.5f;

    // =========================
    // 遠距離攻撃設定
    // =========================

    // 飛び道具攻撃するかどうか
    [SerializeField]
    private bool canShoot = false;

    // 飛び道具攻撃距離
    [SerializeField]
    private float rangedAttackDistance = 0;

    // 飛び道具攻撃間隔
    [SerializeField]
    private float rangedAttackCooldown = 3f;

    // 次回飛び道具攻撃可能時間
    private float nextRangedAttackTime;

    // 次攻撃までの間隔
    [SerializeField]
    private float attackCooldown = 2f;

    // 次攻撃可能時間
    private float nextAttackTime;

    // 攻撃中判定
    private bool isAttacking = false;

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

    // ノックバック力
    // Enemyごとに吹き飛ばし強さを変更できる
    [SerializeField]
    private float knockbackForce = 5f;

    // =========================
    // 重量設定
    // 0 = 軽量
    // 1 = 同格
    // 2 = 重量級
    // =========================

    [SerializeField]
    private int weightLevel = 0;

    // 外部取得用
    public int WeightLevel => weightLevel;

    // ProjectilePrefab
    [SerializeField]
    private BossProjectile projectilePrefab;

    // 発射位置
    [SerializeField]
    private Transform firePoint;

    // =========================
    // 後退行動設定
    // =========================

    // 後退中判定
    private bool isRetreating = false;

    // 後退終了時間
    private float retreatEndTime;

    // 後退時間
    [SerializeField]
    private float retreatDuration = 1.5f;

    // 後退速度
    [SerializeField]
    private float retreatSpeed = 3f;

    // 後退方向の地面確認位置
    [SerializeField]
    private Transform retreatGroundCheck;


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

    }


    // 物理更新
    private void FixedUpdate()
    {
        // ★ゲーム開始前は何もしない
        if (!GameManager.IsGameStarted)
        {
            // 横移動停止
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        // 飛ばされ中は移動停止
        if (enemyHealth.IsLaunched)
        {
            // 横移動停止
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

            return;
        }

        // ノックバック中は移動停止
        if (enemyHealth.IsKnockback)
        {
            return;
        }
        // 打ち上げ中はAI停止
        if (enemyHealth.IsLaunched)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

            return;
        }
        // 攻撃中は移動停止
        if (isAttacking)
        {
            // 横移動停止
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

            return;
        }

        // Playerとの距離 Enemyの位置とPlayerの位置を測って入れる
        float distance = Vector2.Distance(transform.position, playerTransform.position);

        // 後退中はPlayerと逆方向へ移動し、Player方向は向き続ける
        if (isRetreating)
        {
            // 後退方向に壁や崖があれば後退終了
            if (CheckRetreatObstacle())
            {
                isRetreating = false;

                // 横移動停止
                rb.linearVelocity =
                    new Vector2(
                        0,
                        rb.linearVelocity.y);

                return;
            }

            // Playerと逆方向へ移動
            float retreatDirection = playerTransform.position.x > transform.position.x ? -1f : 1f;

            // 移動
            rb.linearVelocity = new Vector2(retreatDirection * retreatSpeed, rb.linearVelocity.y);

            // Player方向は向き続ける
            float targetDirection = playerTransform.position.x > transform.position.x ? 1f : -1f;

            // 向き違うなら反転
            if ((targetDirection > 0 && !isFacingRight) || (targetDirection < 0 && isFacingRight))
            {
                Flip();
            }

            // 時間終了 
            if (Time.time >= retreatEndTime)
            {
                isRetreating = false;

                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
            // 後退中は他の行動しない
            return;
        }

        // 移動方向
        float moveDirection;

        // 攻撃距離内なら停止
        if (distance <= meleeAttackDistance)
        {
            // 移動停止
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

            // 攻撃可能なら攻撃
            if (Time.time >= nextAttackTime) Attack();

            // 近接攻撃中は他の行動しない
            return;
        }

        // =========================
        // 飛び道具攻撃距離
        // =========================

        // 飛び道具攻撃距離内なら停止して攻撃
        if (distance <= rangedAttackDistance)
        {
            // 移動停止
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

            // Player方向確認
            float targetDirection = playerTransform.position.x > transform.position.x ? 1f : -1f;

            // 向き違うなら反転
            if ((targetDirection > 0 && !isFacingRight) || (targetDirection < 0 && isFacingRight))
            {
                // 向き反転
                Flip();
            }

            // 飛び道具攻撃可能なら攻撃開始
            if (Time.time >= nextRangedAttackTime)
            {
                // 攻撃中状態
                isAttacking = true;

                // 遠距離攻撃アニメ開始
                animator.SetTrigger("RangedAttack");

                // 次回攻撃時間更新
                nextRangedAttackTime =
                    Time.time + rangedAttackCooldown;
            }
            // 飛び道具攻撃中は他の行動しない
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
                // 向き反転
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
            Flip();
        }
    }

    /// <summary>
    /// 後退方向に壁や崖があるか確認する
    /// true なら後退を終了する
    /// </summary>
    private bool CheckRetreatObstacle()
    {
        // 後退方向を決定
        // Playerが右にいるなら左へ後退
        // Playerが左にいるなら右へ後退
        Vector2 retreatDirection =
            playerTransform.position.x > transform.position.x
            ? Vector2.left
            : Vector2.right;

        // 後退方向の壁確認
        RaycastHit2D wallHit =
            Physics2D.Raycast(
                wallCheck.position,
                retreatDirection,
                checkDistance,
                groundLayer);

        // 後退方向の地面確認
        RaycastHit2D groundHit =
            Physics2D.Raycast(
                retreatGroundCheck.position,
                Vector2.down,
                checkDistance,
                groundLayer);

        // 壁に当たったか
        bool hitWall = wallHit.collider != null;

        // 地面が無いか
        bool noGround = groundHit.collider == null;

        // 壁または崖ならtrue
        return hitWall || noGround;
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
        Debug.Log("近接攻撃開始");

        // 攻撃中ON
        isAttacking = true;

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

        // wallCheck確認
        if (wallCheck != null)
        {
            // 表示色は青
            Gizmos.color = Color.blue;
            // 現在向いている方向を決める
            Vector2 direction = isFacingRight ? Vector2.right : Vector2.left;
            // Rayを表示
            Gizmos.DrawLine(wallCheck.position, (Vector2)wallCheck.position + direction * checkDistance);
        }

        // groundCheck確認
        if (groundCheck != null)
        {
            // 表示色は緑
            Gizmos.color = Color.green;
            // Rayを表示
            Gizmos.DrawLine(groundCheck.position, (Vector2)groundCheck.position + Vector2.down * checkDistance);
        }
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
                playerHealth.TakeDamage(attackDamage, transform.position, knockbackForce);
            }
        }
    }
    // 攻撃終了
    // Animation Eventから呼ばれる
    public void EndAttack()
    {
        // 攻撃中終了
        isAttacking = false;

        // 後退開始
        isRetreating = true;

        // 後退終了時刻 = 現在時刻 + 後退時間
        retreatEndTime = Time.time + retreatDuration;
    }

    // Projectile発射
    public void FireProjectile()
    {
        // そもそも遠距離タイプじゃないなら何もしない
        if (!canShoot) return;

        // FirePointとProjectilePrefabが設定されていないなら終了
        if (firePoint == null || projectilePrefab == null)
            return;
        // 現在向いている方向を決める
        Vector2 direction = isFacingRight ? Vector2.right : Vector2.left;
        // Projectile生成
        BossProjectile projectile =
            Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        // Projectileに方向をセット
        projectile.SetDirection(direction);
    }

    // Enemyと接触中
    private void OnCollisionStay2D(Collision2D collision)
    {
        // Player以外なら終了
        if (!collision.gameObject.CompareTag("Player"))
        {
            return;
        }

        // PlayerController取得
        PlayerController playerController = collision.gameObject.GetComponent<PlayerController>();

        // PlayerController無ければ終了
        if (playerController == null)
        {
            return;
        }
        // 重量級ならPlayerを停止させる
        if (weightLevel >= 2)
        {
            // Player停止
            playerController.SetBlocked(true);
        }
    }

    // 接触終了
    private void OnCollisionExit2D(Collision2D collision)
    {
        // Player以外なら終了
        if (!collision.gameObject.CompareTag("Player"))
        {
            return;
        }

        // PlayerController取得
        PlayerController playerController = collision.gameObject.GetComponent<PlayerController>();

        // 無ければ終了
        if (playerController == null)
        {
            return;
        }

        // 停止解除
        playerController.SetBlocked(false);
    }
}