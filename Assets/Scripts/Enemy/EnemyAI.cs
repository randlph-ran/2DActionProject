using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    // デバッグ用：ノックバック発生後の追跡フレーム数
    private int debugTrackFrames = 0;

    [Header("移動設定")]

    // 移動速度設定
    [Tooltip("移動速度設定")]
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

    [Header("SE")]
    [Tooltip("近接攻撃開始時のSE")]
    [SerializeField]
    private AudioClip meleeAttackSE;

    [Tooltip("飛び道具攻撃開始時のSE")]
    [SerializeField]
    private AudioClip rangedAttackSE;

    [Header("索敵・追尾設定")]

    // 追尾開始距離　指定距離以内なら追尾
    [Tooltip("追尾開始距離　指定距離以内なら追尾")]
    [SerializeField]
    private float chaseDistance = 5f;

    [Tooltip("上下方向の索敵許容距離\nPlayerとの上下距離がこれを超えると索敵対象外になる")]
    [SerializeField]
    private float chaseHeight = 1.5f;

    // =========================
    // 近接攻撃設定
    // =========================
    [Header("近接攻撃設定")]

    // 近接攻撃距離
    [Tooltip("近接攻撃距離\nこの距離以内ならPlayerに近接攻撃する")]
    [SerializeField]
    private float meleeAttackDistance = 1.5f;


    // =========================
    // 遠距離攻撃設定
    // =========================
    [Header("遠距離攻撃設定")]

    // 飛び道具攻撃するかどうか
    [Tooltip("飛び道具攻撃するかどうか")]
    [SerializeField]
    private bool canShoot = false;

    // 飛び道具攻撃距離
    [Tooltip("飛び道具攻撃距離\nこの距離以内なら飛び道具で攻撃する")]
    [SerializeField]
    private float rangedAttackDistance = 0;

    // 飛び道具攻撃間隔
    [Tooltip("飛び道具攻撃間隔（秒）")]
    [SerializeField]
    private float rangedAttackCooldown = 3f;

    // 次回飛び道具攻撃可能時間
    private float nextRangedAttackTime;

    // ProjectilePrefab
    [Tooltip("飛び道具のPrefab")]
    [SerializeField]
    private Projectile projectilePrefab;

    // 発射位置
    [Tooltip("発射位置")]
    [SerializeField]
    private Transform firePoint;

    // =========================
    // 攻撃共通設定
    // =========================
    [Header("攻撃共通設定")]

    // 次攻撃までの間隔
    [Tooltip("次攻撃までの間隔（秒）\n近接攻撃のクールダウン")]
    [SerializeField]
    private float attackCooldown = 2f;

    // 次攻撃可能時間
    private float nextAttackTime;

    // 攻撃中判定
    private bool isAttacking = false;

    // 攻撃判定位置
    [Tooltip("攻撃判定位置\nここを中心に攻撃範囲を判定する")]
    [SerializeField]
    private Transform attackPoint;

    // 攻撃範囲
    [Tooltip("攻撃範囲（半径）")]
    [SerializeField]
    private float attackRadius = 0.8f;

    // PlayerLayer
    [Tooltip("攻撃判定で検知するPlayer用Layer")]
    [SerializeField]
    private LayerMask playerLayer;

    // 攻撃力
    [Tooltip("攻撃力")]
    [SerializeField]
    private int attackDamage = 1;

    // ノックバック力
    // Enemyごとに吹き飛ばし強さを変更できる
    [Tooltip("ノックバック力\nEnemyごとに吹き飛ばし強さを変更できる")]
    [SerializeField]
    private float knockbackForce = 5f;

    // =========================
    // 壁・地面センサー設定
    // =========================
    [Header("壁・地面センサー設定")]

    // 壁確認位置
    // 前方の壁を調べるRayの開始地点
    [Tooltip("前方の壁を調べるRayの開始地点")]
    [SerializeField]
    private Transform wallCheck;

    // 地面確認位置
    // 前方の足元を調べるRayの開始地点
    [Tooltip("前方の足元を調べるRayの開始地点")]
    [SerializeField]
    private Transform groundCheck;

    // Rayの長さ
    // 数値を大きくすると遠くまで検知する
    [Tooltip("Rayの長さ\n数値を大きくすると遠くまで検知する")]
    [SerializeField]
    private float checkDistance = 0.5f;

    // 地面用Layer
    // GroundLayerだけを検知する
    [Tooltip("地面用Layer\nGroundLayerだけを検知する")]
    [SerializeField]
    private LayerMask groundLayer;

    // =========================
    // 重量設定
    // 0 = 軽量
    // 1 = 同格
    // 2 = 重量級
    // =========================
    [Header("重量設定")]

    [Tooltip("重さレベル\n0:軽量 1:同格 2:重量級\nノックバック攻撃を受けたときの吹き飛びやすさに影響")]
    [SerializeField]
    private int weightLevel = 0;

    // 外部取得用
    public int WeightLevel => weightLevel;

    // =========================
    // 後退行動設定
    // =========================
    [Header("後退行動設定")]

    // 後退中判定
    private bool isRetreating = false;

    // 後退終了時間
    private float retreatEndTime;

    // 後退時間
    [Tooltip("後退時間（秒）\n近接攻撃後にPlayerから離れる時間")]
    [SerializeField]
    private float retreatDuration = 1.5f;

    // 後退速度
    [Tooltip("後退速度")]
    [SerializeField]
    private float retreatSpeed = 3f;

    // 後退方向の地面確認位置
    [Tooltip("後退方向の地面確認位置\n後退先に壁や崖が無いか確認する")]
    [SerializeField]
    private Transform retreatGroundCheck;

    // =========================
    // スタン設定
    // =========================
    [Header("スタン設定")]

    private bool isStunned;
    private float stunTimer;
    private bool isStunImmune;
    private float stunImmuneTimer;

    // スタン耐性レベル
    // 0 = 弱い
    // 1 = 普通
    // 2 = 強い
    [Tooltip("スタン耐性レベル\n0:弱い 1:普通 2:強い\n数値が高いほどスタン時間が短くなる")]
    [SerializeField]
    private int stunLevel = 1;

    // 外部取得用
    public int AttackDamage => attackDamage;
    // 外部取得用
    public float KnockbackForce => knockbackForce;

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

        // 距離設定の矛盾チェック
        ValidateDistanceSettings();
    }

    /// <summary>
    /// 距離系パラメータの設定ミスを検知して警告を出す。
    /// meleeAttackDistanceがchaseDistanceより大きいと、
    /// 追尾フェーズ（Flip・isMoving更新を行う処理）を経ずに
    /// 直接近接攻撃の待機分岐に入ってしまい、Playerが反転側に回り込んでも
    /// 向きが変わらず移動モーションだけが残り続けるという不具合の原因になる。
    /// </summary>
    private void ValidateDistanceSettings()
    {
        if (meleeAttackDistance > chaseDistance)
        {
            Debug.LogWarning(
                $"[EnemyAI] {gameObject.name}: meleeAttackDistance({meleeAttackDistance})が" +
                $"chaseDistance({chaseDistance})より大きく設定されています。" +
                "追尾フェーズをスキップして直接攻撃待機状態に入り、" +
                "Playerが反転側に回り込んでも向きが変わらなくなる場合があります。" +
                "meleeAttackDistanceをchaseDistance以下に調整することを推奨します。");
        }
    }

    private void Update()
    {
        // スタン時間
        if (isStunned)
        {
            Debug.Log("スタン時間残り：" + stunTimer);
            // スタン時間減らす
            stunTimer -= Time.deltaTime;
            // スタン時間終了したらスタン終了
            if (stunTimer <= 0f)
            {
                EndStun();
            }
        }

        // スタン無効時間
        if (isStunImmune)
        {
            // スタン無効時間減らす
            stunImmuneTimer -= Time.deltaTime;
            // スタン無効時間終了したらスタン無効終了
            if (stunImmuneTimer <= 0f)
            {
                // スタン無効終了
                isStunImmune = false;
            }
        }
    }


    // 物理更新
    private void FixedUpdate()
    {
        // ノックバックが新しく始まった瞬間を検知してログ追跡を開始
        if (enemyHealth.IsKnockback && debugTrackFrames <= 0)
        {
            debugTrackFrames = 15; // 以降15フレームだけ詳細ログ
        }

        if (debugTrackFrames > 0)
        {
            Debug.Log($"[TRACK] frame:{Time.frameCount} velocity:{rb.linearVelocity} IsKnockback:{enemyHealth.IsKnockback} IsLaunched:{enemyHealth.IsLaunched} isAttacking:{isAttacking} isStunned:{isStunned}");
            debugTrackFrames--;
        }

        // ★ゲーム開始前は何もしない
        if (!GameManager.IsGameStarted)
        {
            // 横移動停止
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        // スタン中は移動停止
        if (isStunned)
        {
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

        // Playerとの上下距離を測る
        float verticalDistance = Mathf.Abs(playerTransform.position.y - transform.position.y);
        // 上下距離が追尾許容範囲内かどうか
        bool canDetectPlayer = verticalDistance <= chaseHeight;

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

        // =========================
        // 近接攻撃距離
        // =========================
        if (canDetectPlayer && distance <= meleeAttackDistance)
        {
            // 移動停止
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

            // Player方向確認
            // meleeAttackDistanceがchaseDistanceより大きい設定の場合、
            // 追尾フェーズを経ずに直接この分岐へ入ることがあるため、向きの更新をここでも保険として行う
            float targetDirection = playerTransform.position.x > transform.position.x ? 1f : -1f;

            // 向き違うなら反転
            if ((targetDirection > 0 && !isFacingRight) || (targetDirection < 0 && isFacingRight))
            {
                Flip();
            }

            // 攻撃可能なら攻撃
            if (Time.time >= nextAttackTime) Attack();

            // 近接攻撃中は他の行動しない
            return;
        }

        // =========================
        // 飛び道具攻撃距離
        // =========================

        // 飛び道具攻撃可能なら攻撃
        if (canDetectPlayer && distance <= rangedAttackDistance)
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

                // 攻撃SE再生
                SoundManager.Instance?.PlaySE(rangedAttackSE);

                // 遠距離攻撃アニメ開始
                animator.SetTrigger("RangedAttack");

                // 次回攻撃時間更新
                nextRangedAttackTime =
                    Time.time + rangedAttackCooldown;
            }
            // 飛び道具攻撃中は他の行動しない
            return;
        }

        // =========================
        // Player追尾
        // =========================
        if (canDetectPlayer && distance <= chaseDistance)
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
            // ■ Playerを追っていない時だけ
            // 壁と崖を確認する（この中でFlip()が呼ばれる可能性がある）
            CheckWallAndGround();

            // Flip()による向き変更を反映してから移動方向を決定する
            moveDirection = isFacingRight ? 1f : -1f;
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

        // 攻撃SE再生
        SoundManager.Instance?.PlaySE(meleeAttackSE);

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
        if (!canShoot) return;

        if (firePoint == null || projectilePrefab == null)
            return;

        Vector2 direction = isFacingRight ? Vector2.right : Vector2.left;

        Projectile projectile =
            Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

        projectile.Initialize(
            direction,
            attackDamage,
            knockbackForce,
            1f,              // launchPower（Enemyは仮値でOK）
            gameObject
        );
    }

    // スタンレベルに応じた基礎スタン時間を取得
    private float GetBaseStunTime()
    {
        switch (stunLevel)
        {
            // スタンに弱い
            case 0:
                return 2f;

            // 標準
            case 1:
                return 1f;

            // スタンに強い
            case 2:
                return 0.5f;

            default:
                return 1f;
        }
    }


    // スタン処理
    // スタン付与
    // stunRate = 1.0で100%
    public void ApplyStun(float stunRate)
    {
        // スタン無効中なら終了
        if (isStunImmune)
        {
            return;
        }

        // 0～200%に制限
        stunRate = Mathf.Clamp(stunRate, 0f, 2f);

        // 最終スタン時間
        float finalStunTime =
            GetBaseStunTime() * stunRate;

        // スタン開始
        isStunned = true;
        stunTimer = finalStunTime;

        // 5秒間スタン耐性
        isStunImmune = true;
        stunImmuneTimer = 5f;

        // スタンアニメ開始
        animator.SetBool("isStunned", true);
    }

    // スタン終了
    private void EndStun()
    {
        Debug.Log(gameObject.name + " スタン終了");
        // スタン終了
        isStunned = false;
        // スタンアニメ終了
        animator.SetBool("isStunned", false);

    }
    // 攻撃キャンセル　Animation Eventから呼ばれる
    public void CancelAttack()
    {
        isAttacking = false;
    }
}