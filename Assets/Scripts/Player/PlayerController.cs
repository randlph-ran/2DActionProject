using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // PlayerInputReader
    private PlayerInputReader inputReader;

    // Animator
    private Animator animator;

    // 攻撃エフェクトPrefab
    [SerializeField] private GameObject slashEffectPrefab;

    // 現在のコンボ段階（1〜3）
    private int comboStep = 0;

    // Rigidbody2D
    private Rigidbody2D rb;

    // PlayerHealth
    private PlayerHealth playerHealth;

    // InventoryManager
    private InventoryManager inventoryManager;

    // 左右入力
    private float moveInput;

    // 移動速度
    [Header("移動")]
    [Tooltip("移動速度")]
    [SerializeField]
    private float moveSpeed = 5f;

    // 攻撃中判定
    private bool isAttacking = false;

    // 次コンボへ進めるか
    private bool canNextCombo = false;


    // ジャンプ力
    [Header("ジャンプ")]
    [Tooltip("ジャンプ力")]
    [SerializeField]
    private float jumpPower = 12f;

    // 空中ジャンプ攻撃使用済みか
    private bool hasUsedJumpAttack;

    // 右向き判定
    private bool isFacingRight = true;

    // 接地判定
    private bool isGrounded;

    // 現在のジャンプ回数
    private int jumpCount = 0;

    // 最大ジャンプ回数
    [Tooltip("最大ジャンプ回数")]
    [SerializeField]
    private int maxJumpCount = 2;

    // GroundCheck位置
    [Header("接地判定")]
    [Tooltip("GroundCheck位置")]
    [SerializeField]
    private Transform groundCheck;

    // 攻撃中の移動用GroundCheck距離
    [Tooltip("攻撃中の移動用GroundCheck距離")]
    [SerializeField]
    private float attackMoveGroundCheckDistance = 0.6f;

    // Ground判定半径
    [Tooltip("Ground判定半径")]
    [SerializeField]
    private float groundCheckRadius = 0.1f;

    // GroundLayer
    [Tooltip("GroundLayer")]
    [SerializeField]
    private LayerMask groundLayer;


    // =========================
    // Attack1設定
    // =========================
    [Header("攻撃 - Attack1")]

    // Attack1の攻撃位置オフセット
    [Tooltip("Attack1の攻撃位置オフセット")]
    [SerializeField]
    private Vector2 attack1Offset;

    // Attack1の攻撃範囲
    [Tooltip("Attack1の攻撃範囲")]
    [SerializeField]
    private float attack1Radius = 1.0f;


    // =========================
    // Attack2設定
    // =========================
    [Header("攻撃 - Attack2")]

    // Attack2の攻撃位置オフセット
    [Tooltip("Attack2の攻撃位置オフセット")]
    [SerializeField]
    private Vector2 attack2Offset;

    // Attack2の攻撃範囲
    [Tooltip("Attack2の攻撃範囲")]
    [SerializeField]
    private float attack2Radius = 1.2f;


    // =========================
    // Attack3設定
    // =========================
    [Header("攻撃 - Attack3")]

    // Attack3の攻撃位置オフセット
    [Tooltip("Attack3の攻撃位置オフセット")]
    [SerializeField]
    private Vector2 attack3Offset;

    // Attack3の攻撃範囲
    [Tooltip("Attack3の攻撃範囲")]
    [SerializeField]
    private float attack3Radius = 1.5f;

    // Attack1浮かせ力
    [Header("攻撃 - 効果")]
    [Tooltip("Attack1浮かせ力")]
    [SerializeField]
    private float attack1LaunchPower = 1.8f;

    // Attack2浮かせ力
    [Tooltip("Attack2浮かせ力")]
    [SerializeField]
    private float attack2LaunchPower = 0f;

    // Attack3浮かせ力
    [Tooltip("Attack3浮かせ力")]
    [SerializeField]
    private float attack3LaunchPower = 0f;

    // 現在の浮かせ力
    private float currentLaunchPower;


    //==============================
    // アイテム
    //==============================

    [Header("アイテム")]

    // 現在装備中のアイテム
    // InventoryManagerが管理する装備状態をそのまま参照する
    private ItemData currentItem => inventoryManager != null ? inventoryManager.EquippedItem : null;

    // 次回発射可能時間
    private float nextShootTime;

    // 発射方向キャッシュ
    private Vector2 cachedShootDirection;

    // =========================
    // 現在使用中の攻撃情報
    // =========================

    // 現在の攻撃位置
    private Vector2 currentAttackOffset;

    // 現在の攻撃範囲
    private float currentAttackRadius;

    [Header("攻撃 - 共通")]
    [SerializeField]
    private int attackDM = 1;

    // Attack1用ノックバック
    [Tooltip("Attack1用ノックバック")]
    [SerializeField]
    private float attack1Knockback = 2f;

    // Attack2用ノックバック
    [Tooltip("Attack2用ノックバック")]
    [SerializeField]
    private float attack2Knockback = 3f;

    // Attack3用ノックバック
    [Tooltip("Attack3用ノックバック")]
    [SerializeField]
    private float attack3Knockback = 8f;

    // 敵Layer
    [Tooltip("敵Layer")]
    [SerializeField]
    private LayerMask enemyLayer;

    // デバッグ用攻撃Gizmo表示フラグ
    private bool isAttackGizmoVisible = false;

    [Header("デバッグ/表示")]
    // 攻撃範囲を常時表示するか
    [Tooltip("攻撃範囲を常時表示するか")]
    [SerializeField]
    private bool alwaysShowAttackGizmo = true;

    // Gizmo表示時間
    [Tooltip("Gizmo表示時間")]
    [SerializeField]
    private float attackGizmoDuration = 0.2f;

    //Weight負け中移動停止フラグ
    private bool isBlocked;

    [Header("落下/初期設定")]
    // 落下速度上限
    [Tooltip("落下速度上限")]
    [SerializeField]
    private float maxFallSpeed = 10f;

    // 最初の向き右フラグ
    [Tooltip("最初の向き右フラグ")]
    [SerializeField]
    private bool startFacingRight = true;

    [Header("攻撃 - 移動")]
    // 攻撃中の移動距離(Attack1は多め、Attack3は少なめ)
    [Tooltip("攻撃中の移動距離(Attack1は多め、Attack3は少なめ)")]
    [SerializeField] private float attack1MoveDistance = 0.55f;
    [Tooltip("攻撃中の移動距離(Attack1は多め、Attack3は少なめ)")]
    [SerializeField] private float attack2MoveDistance = 0.55f;
    [Tooltip("攻撃中の移動距離(Attack1は多め、Attack3は少なめ)")]
    [SerializeField] private float attack3MoveDistance = 0.275f;

    // コンボ追尾対象 Attack1の攻撃開始時に設定され、Attack1～3の移動で追尾する
    private Transform comboTarget;

    // 攻撃中の移動がGround切れで途中終了したときに、攻撃終了まで移動させないようにするフラグ
    private bool isAttackLocked = false;

    // 攻撃終了後の硬直時間（調整ポイント）
    [Tooltip("攻撃終了後の硬直時間（調整ポイント）")]
    [SerializeField] private float attack1EndLock = 0.25f;
    [Tooltip("攻撃終了後の硬直時間（調整ポイント）")]
    [SerializeField] private float attack2EndLock = 0.12f;
    [Tooltip("攻撃終了後の硬直時間（調整ポイント）")]
    [SerializeField] private float attack3EndLock = 0.25f;

    // ジャンプ攻撃中フラグ
    private bool isJumpAttacking;

    // =========================
    // JumpAttack設定
    // =========================

    [Header("ジャンプ攻撃")]
    // 前方判定
    [Tooltip("前方判定")]
    [SerializeField]
    private Vector2 jumpAttackForwardOffset;

    // 下方向判定
    [Tooltip("下方向判定")]
    [SerializeField]
    private Vector2 jumpAttackDownOffset;

    // 判定半径
    [Tooltip("判定半径")]
    [SerializeField]
    private float jumpAttackRadius = 1.0f;

    // ノックバック
    [Tooltip("ノックバック")]
    [SerializeField]
    private float jumpAttackKnockback = 10f;

    // ダメージ
    [Tooltip("ダメージ")]
    [SerializeField]
    private int jumpAttackDamage = 1;

    // ジャンプ攻撃中の移動停止(落下停止)
    [Tooltip("ジャンプ攻撃中の移動停止(落下停止)")]
    [SerializeField]
    private float jumpAttackStopTime = 0.15f;

    // スタン係数
    // 1.0 = 100%
    // 2.0 = 200%
    // 0.5 = 50%
    [Tooltip("スタン時間")]
    [SerializeField]
    private float jumpAttackStunRate = 1.0f;


    /// <summary>
    /// 現在ガード中か
    /// </summary>
    [SerializeField]
    [Tooltip("現在ガード状態かどうか（デバッグ確認用）")
    ]
    private bool isGuarding;

    /// <summary>
    /// 現在ガード中か
    /// </summary>
    public bool IsGuarding => isGuarding;

    private bool previousGuardState;

    private void Start()
    {
        // 初期向き設定
        InitFacingDirection();
    }

    // ゲーム開始時に最初に呼ばれる
    private void Awake()
    {
        // Rigidbody2D取得
        rb = GetComponent<Rigidbody2D>();

        // PlayerHealth取得
        playerHealth = GetComponent<PlayerHealth>();

        //Animator取得
        animator = GetComponent<Animator>();

        // PlayerInputReader取得
        inputReader = GetComponent<PlayerInputReader>();

        // InventoryManager取得
        inventoryManager = GetComponent<InventoryManager>();
    }

    // 毎フレーム実行
    // 入力取得を行う
    private void Update()
    {
        // ゲーム開始前の処理
        if (!GameManager.IsGameStarted)
        {
            // 接地判定だけ更新
            CheckGround();

            // 接地状態をAnimatorへ送る
            animator.SetBool("isGrounded", isGrounded);

            // ゲーム開始前は移動停止
            animator.SetBool("isRunning", false);
            // ゲーム開始前に落下速度を0にする(開始演出時の落下アニメループ防止)
            animator.SetFloat("verticalSpeed", 0);

            return;
        }

        // 左右入力
        moveInput = inputReader.MoveInput.x;

        // 接地判定
        CheckGround();
        // ガード状態更新
        UpdateGuardState();

        // Animatorへガード状態を送る
        animator.SetBool("isGuarding", isGuarding);

        // 移動状態は、左右入力があるかつガードしていないとき
        bool isRunning = Mathf.Abs(moveInput) > 0.1f && !isGuarding;

        // Animatorへ移動状態を送る
        animator.SetBool("isRunning", isRunning);

        // 接地状態をAnimatorへ送る
        animator.SetBool("isGrounded", isGrounded);

        // 縦速度をAnimatorへ送る
        animator.SetFloat("verticalSpeed", rb.linearVelocity.y);

        // ノックバック中はジャンプ操作禁止
        if (playerHealth.IsKnockback)
        {
            return;
        }
        // ガード中は操作不可
        if (isGuarding)
        {
            return;
        }

        // 攻撃中、アイテム中は向き固定
        if (!isAttacking && !isShooting)
        {
            Flip();
        }

        // 攻撃入力
        if (inputReader.AttackPressed)
        {
            // 空中ならジャンプ攻撃
            if (!isGrounded)
            {
                // ジャンプ攻撃処理
                HandleJumpAttack();
                return;
            }

            // 地上攻撃が可能か判定
            if (!CanAttack())
            {
                Debug.Log($"[INPUT] CanAttack()がfalseで弾かれた frame:{Time.frameCount}");
                return;
            }
            // 攻撃ロック中なら攻撃できない
            if (isAttackLocked)
            {
                Debug.Log($"[INPUT] isAttackLockedで弾かれた frame:{Time.frameCount}");
                return;
            }
            // 地上なら通常攻撃
            if (!isAttacking)
            {
                Debug.Log($"[INPUT] 新規攻撃開始 frame:{Time.frameCount}");
                // 攻撃処理
                HandleAttackInput();
            }
            // 攻撃中で、次コンボ受付中なら、次のコンボへ進める
            else if (canNextCombo)
            {
                Debug.Log($"[INPUT] コンボ継続 frame:{Time.frameCount}");
                // 攻撃処理
                HandleAttackInput();
            }
            else
            {
                Debug.Log($"[INPUT] 入力は来たが無視された(isAttacking=true, canNextCombo=false) frame:{Time.frameCount}");
            }
            Debug.Log("攻撃開始");
            Debug.Log("現在コンボ段数：" + comboStep);
        }

        // ジャンプ入力
        // 攻撃中・アイテム使用中・ガード中はジャンプできない
        if (CanJump())
        {
            // ジャンプ入力
            if (inputReader.JumpPressed)
            {
                // ジャンプカウントが最大未満なら2段ジャンプする
                if (jumpCount < maxJumpCount)
                {
                    Jump();
                }
            }
        }

        // 攻撃中の移動処理
        float direction = isFacingRight ? 1f : -1f;

        // アイテム入力処理
        // Attack/JumpAttack入力判定の後に呼ぶことで、
        // 同一フレームにAttackとItemが同時入力されても
        // Attack側が先に isJumpAttacking=true をセットするため
        // CanShoot()のisJumpAttackingチェックが正しく機能する
        HandleShootInput();

    }

    // 物理演算用
    private void FixedUpdate()
    {
        // ゲーム開始前は移動停止
        if (!GameManager.IsGameStarted)
        {
            // 完全に停止させるために速度もゼロにする
            rb.linearVelocity = Vector2.zero;
            // Animatorへ移動状態を送る
            return;
        }

        // ノックバック中は移動停止
        if (playerHealth.IsKnockback)
        {
            return;
        }

        // 攻撃中、アイテム中は横移動停止
        if (isAttacking || isShooting)
        {
            // 横移動停止
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

            return;
        }

        // ガード中は移動停止
        if (isGuarding)
        {
            rb.linearVelocity =
                new Vector2(0, rb.linearVelocity.y);

            return;
        }


        //Weight負け中は移動停止
        if (isBlocked)
        {
            // 横移動停止
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

            return;
        }

        // 左右移動
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);


        // 落下速度制限
        if (rb.linearVelocity.y < -maxFallSpeed)
        {
            rb.linearVelocity =
                new Vector2(rb.linearVelocity.x, -maxFallSpeed);
        }
    }

    // ジャンプ処理    
    private void Jump()
    {
        Debug.Log($"Jump実行  isGrounded:{isGrounded}  jumpCount:{jumpCount}");
        // 攻撃入力があるフレームはジャンプしない
        if (inputReader.AttackPressed)
        {
            return;
        }

        Debug.Log("ジャンプ入力したよ");
        // Y方向速度リセット
        // 落下中でも一定ジャンプ力になる
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);

        // ジャンプ力を加える
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpPower);

        // ジャンプ回数加算
        jumpCount++;
    }
    // 接地判定
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
            Debug.Log("Grounded");
            ResetJumpCount();
            Debug.Log(jumpCount);//リセット処理入ったかの確認
        }
    }

    // ジャンプ回数リセット
    private void ResetJumpCount()
    {
        // ジャンプ回数リセット
        jumpCount = 0;
        // 空中ジャンプ攻撃使用フラグリセット
        hasUsedJumpAttack = false;
        // 着地時にジャンプ攻撃フラグを強制リセット（AnimationEventが呼ばれなかった場合のフォールバック）
        isJumpAttacking = false;
        isAttacking = false;
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
        // GroundCheck位置に円を描く
        if (groundCheck != null)
        {
            // GroundCheck黄色
            Gizmos.color = Color.yellow;

            // GroundCheck位置から、向きに応じた距離だけ先の位置を計算する
            Vector2 checkPos = (Vector2)groundCheck.position + Vector2.right * (isFacingRight ? 1 : -1) * attackMoveGroundCheckDistance;
            // GroundCheck位置に円を描く
            Gizmos.DrawWireSphere(checkPos, groundCheckRadius);
        }
        // ジャンプ攻撃のGizmo表示
        Gizmos.color = Color.cyan;
        // 向きによって攻撃位置反転
        Vector2 forwardPos = (Vector2)transform.position + new Vector2(jumpAttackForwardOffset.x * (isFacingRight ? 1 : -1), jumpAttackForwardOffset.y);
        // 下方向は向き関係なく一定
        Vector2 downPos = (Vector2)transform.position + new Vector2(jumpAttackDownOffset.x * (isFacingRight ? 1 : -1), jumpAttackDownOffset.y);
        // 攻撃範囲表示
        Gizmos.DrawWireSphere(forwardPos, jumpAttackRadius);
        Gizmos.DrawWireSphere(downPos, jumpAttackRadius);
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

        // 現在のコンボ用ノックバック
        float currentKnockback = attack1Knockback;

        // 現在のコンボ用のノックバックと浮かせ力
        switch (comboStep)
        {
            case 1:
                currentKnockback = attack1Knockback;
                currentLaunchPower = attack1LaunchPower;
                break;

            case 2:
                currentKnockback = attack2Knockback;
                currentLaunchPower = attack2LaunchPower;
                break;

            case 3:
                currentKnockback = attack3Knockback;
                currentLaunchPower = attack3LaunchPower;
                break;
        }

        // Enemy全処理
        foreach (Collider2D enemy in hitEnemies)
        {
            // EnemyHealth取得
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();

            // EnemyHealthが存在するなら
            if (enemyHealth != null)
            {
                AttackType attackType = GetCurrentAttackType();
                enemyHealth.TakeDamage(attackDM, transform, currentKnockback, currentLaunchPower, attackType);
                Debug.Log(comboStep + "段目の攻撃が敵にヒット！ダメージ：" + attackDM);
            }
        }
        // コンボ段階1の攻撃で、攻撃範囲内に敵がいるなら、コンボ追尾対象を更新する
        if (comboStep == 1)
        {
            UpdateComboTarget(hitEnemies);
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

    // 空中ジャンプ攻撃処理
    public void JumpAttack()
    {
        // 向きによって攻撃位置反転
        Vector2 forwardPos = (Vector2)transform.position + new Vector2(jumpAttackForwardOffset.x * (isFacingRight ? 1 : -1), jumpAttackForwardOffset.y);

        // 下方向は向き関係なく一定
        Vector2 downPos = (Vector2)transform.position + new Vector2(jumpAttackDownOffset.x * (isFacingRight ? 1 : -1), jumpAttackDownOffset.y);

        // 前方と下方向の攻撃範囲内のEnemy取得
        Collider2D[] forwardHits = Physics2D.OverlapCircleAll(forwardPos, jumpAttackRadius, enemyLayer);
        Collider2D[] downHits = Physics2D.OverlapCircleAll(downPos, jumpAttackRadius, enemyLayer);
        // ダメージとノックバックを与える処理
        DamageEnemies(forwardHits);
        DamageEnemies(downHits);
    }

    /// <summary>
    /// 攻撃入力時の処理
    /// ・コンボ段階を進める
    /// ・現在の攻撃範囲を設定する
    /// ・Animatorへ現在コンボを送る
    /// ・攻撃状態を開始する
    /// </summary>
    private void HandleAttackInput()
    {
        // 次コンボ受付を一旦OFF
        // AnimationEventで再度ONにする=Eventで呼ばれるまでボタン連打でのコンボ暴発防止策
        canNextCombo = false;

        // コンボ段階を進める
        comboStep++;

        // 3段目を超えたら1へ戻す
        if (comboStep > 3)
        {
            comboStep = 1;
        }
        Debug.Log($"[COMBO] comboStep更新 frame:{Time.frameCount} 新comboStep:{comboStep}");
        // 現在のコンボ段階に応じて
        // 攻撃範囲設定を切り替える
        switch (comboStep)
        {
            // =========================
            // Attack1の時の情報
            // =========================
            case 1:

                // Attack1用の攻撃位置を設定
                currentAttackOffset = attack1Offset;

                // Attack1用の攻撃範囲を設定
                currentAttackRadius = attack1Radius;

                // 確認用ログ
                Debug.Log(currentAttackOffset);
                Debug.Log(currentAttackRadius);

                break;

            // =========================
            // Attack2の時の情報
            // =========================
            case 2:

                // Attack2用の攻撃位置を設定
                currentAttackOffset = attack2Offset;

                // Attack2用の攻撃範囲を設定
                currentAttackRadius = attack2Radius;

                // 確認用ログ
                Debug.Log(currentAttackOffset);
                Debug.Log(currentAttackRadius);

                break;

            // =========================
            // Attack3の時の情報
            // =========================
            case 3:

                // Attack3用の攻撃位置を設定
                currentAttackOffset = attack3Offset;

                // Attack3用の攻撃範囲を設定
                currentAttackRadius = attack3Radius;

                // 確認用ログ
                Debug.Log(currentAttackOffset);
                Debug.Log(currentAttackRadius);

                break;
        }

        // Animatorへ現在のコンボ段階を送る
        // これによってAttack1～3を切り替える
        animator.SetInteger("ComboStep", comboStep);

        // Attack開始Trigger
        // Attack1開始時のみTriggerを送る
        // Attack2以降はAnimator側の遷移で継続する
        if (comboStep == 1)
        {
            animator.SetTrigger("Attack");
        }

        // 攻撃中状態ON
        // 移動停止や向き固定に使用
        isAttacking = true;
    }

    // 空中ジャンプ攻撃処理
    private void HandleJumpAttack()
    {
        // HandleJumpAttack()のDebug.Logも変更
        Debug.Log($"ジャンプ攻撃 frame:{Time.frameCount}");

        // アイテム使用中はJumpAttack不可
        // 仕様：Item中 → JumpAttack ×
        // 先にItem入力があった場合はそちらを優先するため弾く
        if (isShooting)
        {
            return;
        }

        // 使用済みなら何もしない
        if (hasUsedJumpAttack)
        {
            return;
        }

        // 地上コンボを終了
        canNextCombo = false;
        comboStep = 0;
        animator.SetInteger("ComboStep", 0);

        // ジャンプ攻撃状態ON
        isJumpAttacking = true;
        isAttacking = true;
        // ジャンプ攻撃使用済みにする
        hasUsedJumpAttack = true;

        // ジャンプ攻撃Trigger
        animator.SetTrigger("JumpAttack");
        // 確認用ログ
        Debug.Log("ジャンプ攻撃");

        // 落下停止
        StartCoroutine(JumpAttackStopCoroutine());
    }

    // 次コンボ入力を許可する
    // Animation Event から呼ばれる
    public void EnableNextCombo()
    {
        canNextCombo = true;
        Debug.Log($"[ANIM EVENT] EnableNextCombo発火 frame:{Time.frameCount} comboStep:{comboStep}");

        Debug.Log("次コンボ受付開始");
    }
    // 攻撃終了処理
    // Animation Event から呼ばれる
    public void EndAttack()
    {
        Debug.Log("EndAttack呼ばれた");

        if (animator.IsInTransition(0))
        {
            Debug.Log("遷移中なので終了スキップ");
            return;
        }

        float lockTime = 0f;

        switch (comboStep)
        {
            case 1:
                lockTime = attack1EndLock;
                break;
            case 2:
                lockTime = attack2EndLock;
                break;
            case 3:
                lockTime = attack3EndLock;
                break;
        }

        StartCoroutine(EndAttackRoutine(lockTime));
    }

    // Enemyとの接触終了
    public void SetBlocked(bool blocked)
    {
        // 重量1のEnemyと接触終了したら、移動停止解除
        isBlocked = blocked;
    }

    // シーンごとの初期向きを設定する
    private void InitFacingDirection()
    {
        // 最初の向き設定
        isFacingRight = startFacingRight;
        // 現在のScale取得
        Vector3 scale = transform.localScale;

        // 右向きスタート
        if (startFacingRight)
        {
            // ScaleのXを正にする
            scale.x = Mathf.Abs(scale.x);
        }
        // 左向きスタート
        else
        {
            // ScaleのXを負にする
            scale.x = -Mathf.Abs(scale.x);
        }
        // Scale適用
        transform.localScale = scale;
    }

    // 攻撃中の移動を開始する
    private void StartAttackMove(float distance, float duration)
    {
        StartCoroutine(AttackMoveCoroutine(distance, duration));
    }

    // 攻撃中の移動をコルーチンで処理する
    private System.Collections.IEnumerator AttackMoveCoroutine(float distance, float duration)
    {
        // 移動速度を計算する
        float moved = 0f;
        // 向きに応じた移動方向を設定する
        float direction = isFacingRight ? 1f : -1f;
        // 攻撃中の移動速度を設定する
        while (moved < distance)
        {
            // Groundがないなら移動終了
            if (!HasGroundAhead(direction))
            {
                yield break;
            }
            // 1フレームで移動する距離を計算する
            float move = (distance / duration) * Time.deltaTime;
            // Playerを移動させる
            transform.position += new Vector3(direction * move, 0, 0);
            // 移動距離を加算する
            moved += move;
            // 次のフレームまで待つ
            yield return null;
        }
    }

    // 各攻撃開始時に呼ばれるAnimation Event用のメソッド
    public void Attack1MoveStart()
    {
        StartAttackMove(
            attack1MoveDistance,
            0.19f);
    }
    // Attack2とAttack3は距離短め、時間も短めにして、素早く動いて攻撃する感じにする
    public void Attack2MoveStart()
    {
        StartAttackMove(
            attack2MoveDistance,
            0.10f);
    }
    // Attack2とAttack3は距離短め、時間も短めにして、素早く動いて攻撃する感じにする
    public void Attack3MoveStart()
    {
        StartAttackMove(
            attack3MoveDistance,
            0.10f);
    }

    // 攻撃中の移動開始前に、Groundがあるか確認する
    private bool HasGroundAhead(float direction)
    {
        // GroundCheck位置から、向きに応じた距離だけ先の位置を計算する
        Vector2 checkPosition = (Vector2)groundCheck.position + Vector2.right * direction * attackMoveGroundCheckDistance;

        // その位置にGroundLayerがあるか確認する
        return Physics2D.OverlapCircle(checkPosition, groundCheckRadius, groundLayer);
    }

    // コンボ追尾対象を更新する
    private void UpdateComboTarget(Collider2D[] hitEnemies)
    {
        // 攻撃範囲内に敵がいないなら終了
        if (hitEnemies.Length == 0)
        {
            return;
        }

        // 最も近い敵を探す
        float nearestDistance = float.MaxValue;
        // 最も近い敵のTransform
        Transform nearestEnemy = null;

        // 攻撃範囲内の敵全てを確認する
        foreach (Collider2D enemy in hitEnemies)
        {
            // 敵までの距離を計算してdistanceに代入
            float distance = Mathf.Abs(enemy.transform.position.x - transform.position.x);

            // 最も近い敵を更新する
            if (distance < nearestDistance)
            {
                // 最も近い敵の距離を更新する
                nearestDistance = distance;
                // 最も近い敵のTransformを更新する
                nearestEnemy = enemy.transform;
            }
        }
        // 追尾対象を最も近い敵にする
        comboTarget = nearestEnemy;
    }
    // コンボ追尾対象に向き直る
    public void FaceComboTarget()
    {
        // 追尾対象がいないなら終了
        if (comboTarget == null)
        {
            return;
        }
        // 追尾対象の位置に向き直る
        FaceEnemy(comboTarget.position);
    }

    // 攻撃終了処理をコルーチンで行う
    private IEnumerator EndAttackRoutine(float lockTime)
    {
        Debug.Log($"[END] EndAttackRoutine開始 frame:{Time.frameCount} comboStep:{comboStep} lockTime:{lockTime}");
        // ここで即解除しない
        yield return new WaitForSeconds(lockTime);

        isAttacking = false;
        canNextCombo = false;

        comboStep = 0;
        animator.SetInteger("ComboStep", 0);

        comboTarget = null;
    }
    // ジャンプ攻撃終了処理
    public void EndJumpAttack()
    {
        isAttacking = false;
        isJumpAttacking = false;
    }

    // ジャンプ攻撃で敵にダメージを与える処理
    private void DamageEnemies(Collider2D[] hitEnemies)
    {
        // 攻撃範囲内のEnemy全てに処理
        foreach (Collider2D enemy in hitEnemies)
        {
            // EnemyHealth取得
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
            // EnemyHealthが存在しないなら次の敵へ
            if (enemyHealth == null) continue;
            // ダメージとノックバックを与える
            enemyHealth.TakeDamage(jumpAttackDamage, transform, jumpAttackKnockback, 0f, AttackType.JumpAttack);

            EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
            // EnemyAIが存在するならスタンを与える
            if (enemyAI != null)
            {
                enemyAI.ApplyStun(jumpAttackStunRate);
            }
        }
    }

    // ジャンプ攻撃中の移動停止処理
    private IEnumerator JumpAttackStopCoroutine()
    {
        // 現在の速度停止
        rb.linearVelocity = Vector2.zero;

        // 重力無効
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;

        yield return new WaitForSeconds(jumpAttackStopTime);

        // 重力復帰
        rb.gravityScale = originalGravity;
    }

    /// <summary>
    /// Projectileを発射する処理
    /// </summary>
    private void ShootProjectile()
    {
        if (currentItem == null) return;
        if (currentItem.ItemType != ItemType.Projectile) return;

        // GetShootDirection() ではなくキャッシュを使う
        Vector2 direction = cachedShootDirection;

        int dmg = currentItem.Damage;
        float knockback = currentItem.KnockbackPower;
        float launch = currentItem.LaunchPower;

        Vector3 spawnPos = transform.position + (Vector3)(direction * 0.5f);
        Projectile proj = Instantiate(
            currentItem.ProjectilePrefab.GetComponent<Projectile>(),
            spawnPos,
            Quaternion.identity
        );

        proj.Initialize(direction, dmg, knockback, launch, gameObject);
        ConsumeItemUse();
    }

    // 8方向対応の射撃方向決定処理
    private Vector2 GetShootDirection()
    {
        // 方向ベクトル
        Vector2 dir = Vector2.zero;
        // 入力取得
        Vector2 input = inputReader.MoveInput;

        // 入力がなければ向きで決定
        if (input == Vector2.zero)
        {
            // 右向きなら右、左向きなら左
            return isFacingRight ? Vector2.right : Vector2.left;
        }

        // 8方向そのまま正規化
        dir = new Vector2(input.x, input.y);
        // 斜めも含めて正規化して返す
        return dir.normalized;
    }

    /// <summary>
    /// アイテム装備
    /// InventoryManager側の装備状態を更新する
    /// </summary>
    public void EquipItem(ItemData item)
    {
        // 未指定なら終了
        if (item == null)
        {
            return;
        }
        // InventoryManagerへ装備を依頼
        inventoryManager?.EquipItem(item);
    }

    /// <summary>
    /// アイテム解除
    /// InventoryManager側の装備状態を更新する
    /// </summary>
    public void UnequipItem()
    {
        // InventoryManagerへ装備解除を依頼
        inventoryManager?.UnequipItem();
    }

    /// <summary>
    /// アイテム使用回数を消費する
    /// 所持数を1消費し、0になればInventoryManager側で自動的に装備解除される
    /// </summary>
    private void ConsumeItemUse()
    {
        if (inventoryManager == null)
        {
            return;
        }

        // 装備中アイテムを使用（内部で所持数を1消費する）
        ItemData usedItem = inventoryManager.UseEquippedItem();

        if (usedItem != null)
        {
            Debug.Log("残り所持数：" + inventoryManager.GetItemCount(usedItem));
        }
    }

    /// <summary>
    /// 発射可能か判定
    /// </summary>
    /// <summary>
    /// アイテム使用可能か判定
    /// </summary>
    private bool CanShoot()
    {
        // CanShoot()のDebug.Logを以下に変更
        Debug.Log($"CanShoot確認 frame:{Time.frameCount} - isAttacking:{isAttacking} / isShooting:{isShooting} / isJumpAttacking:{isJumpAttacking} / IsInTransition:{animator.IsInTransition(0)}");
        // 未装備
        if (currentItem == null)
        {
            return false;
        }

        // 攻撃中
        // isAttackingフラグが立っている間はItem使用不可
        if (isAttacking)
        {
            return false;
        }

        // アイテム使用中
        // すでにアイテムアニメが再生中ならItem使用不可
        if (isShooting)
        {
            return false;
        }

        // ジャンプ攻撃中
        // isJumpAttackingフラグが立っている間はItem使用不可
        if (isJumpAttacking)
        {
            return false;
        }

        // ノックバック中
        if (playerHealth.IsKnockback)
        {
            return false;
        }

        // ガード中
        if (isGuarding)
        {
            return false;
        }

        // Animator遷移中
        // アニメが切り替わる途中フレームでItem Triggerをセットすると
        // AnimatorがTriggerを消費できずに残留し、isShooting=trueのまま
        // EndShoot()が呼ばれない行動不能バグの原因になるため弾く
        if (animator.IsInTransition(0))
        {
            return false;
        }

        // クールダウン中
        if (Time.time < nextShootTime)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 射撃入力処理
    /// </summary>
    private void HandleShootInput()
    {
        if (currentItem == null) return;

        if (currentItem.CanAutoFire)
        {
            if (inputReader.ShootHeld && CanShoot())
            {
                StartShooting();
            }
        }
        else
        {
            if (inputReader.ShootPressed && CanShoot())
            {
                StartShooting();
            }
        }
    }

    /// <summary>
    /// アイテム使用開始の共通処理
    /// </summary>
    private void StartShooting()
    {
        // ボタンを押した瞬間の方向を保存
        cachedShootDirection = GetShootDirection();

        // 地上コンボ状態を終了
        canNextCombo = false;
        comboStep = 0;
        animator.SetInteger("ComboStep", 0);

        // 残留TriggerをリセットしてからセットすることでAnimator不整合を防ぐ
        animator.ResetTrigger("Item");
        animator.SetTrigger("Item");

        isShooting = true;
        animator.SetBool("isShooting", true);
        nextShootTime = Time.time + currentItem.Cooldown;
    }

    // 射撃中フラグ（向き固定・移動制限に使う場合は追加）
    private bool isShooting = false;

    /// <summary>
    /// 射撃アニメ終了処理
    /// AnimationEvent から呼ぶ
    /// </summary>
    public void EndShoot()
    {
        isShooting = false;

        // Triggerが残っていた場合のリセット
        animator.ResetTrigger("Item");

        // Animatorを通常状態へ戻す
        animator.SetBool("isShooting", false);
    }

    private void UpdateGuardState()
    {
        // 空中なら強制解除
        if (!isGrounded)
        {
            isGuarding = false;
            return;
        }

        // 地上なら入力状態を反映
        isGuarding = inputReader.GuardHeld;

        if (previousGuardState != isGuarding)
        {
            Debug.Log($"Guard State : {isGuarding}");
            previousGuardState = isGuarding;
        }
    }

    /// <summary>
    /// ジャンプ入力を受け付けてよいか判定
    /// </summary>
    private bool CanJump()
    {
        // Attack中ならジャンプできない
        if (isAttacking) return false;
        // Projectile使用中ならジャンプできない
        if (isShooting) return false;
        // ジャンプ攻撃中ならジャンプできない
        if (isJumpAttacking) return false;
        // ノックバック中ならジャンプできない
        if (playerHealth.IsKnockback) return false;
        // ガード中ならジャンプできない
        if (isGuarding) return false;
        // それ以外はジャンプできる
        return true;
    }

    /// <summary>
    /// 地上攻撃入力を受け付けてよいか判定
    /// </summary>
    private bool CanAttack()
    {
        // Item使用中なら地上攻撃出せない
        if (isShooting) return false;
        // 空中なら地上攻撃出せない
        if (!isGrounded) return false;
        // ジャンプ攻撃中なら地上攻撃出せない
        if (isJumpAttacking) return false;
        // ノックバック中なら地上攻撃出せない
        if (playerHealth.IsKnockback) return false;
        // ガード中なら地上攻撃出せない
        if (isGuarding) return false;
        // それ以外は地上攻撃出せる
        return true;
    }

    // comboStepからAttackTypeを判定する
    private AttackType GetCurrentAttackType()
    {
        // comboStepに応じてAttackTypeを返す
        switch (comboStep)
        {
            case 1: return AttackType.Attack1;
            case 2: return AttackType.Attack2;
            case 3: return AttackType.Attack3;
            default: return AttackType.Attack1;
        }
    }
}
