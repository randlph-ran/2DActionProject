using System.Collections;
using UnityEngine;

/// <summary>
/// Playerのコーディネーター。
/// 状態（行動状態・接地・向き）と各種参照の唯一の持ち主となり、
/// 分割した各コンポーネント（PlayerCombat / PlayerItemAction / AfterimageSpawner）へ
/// それらを提供する。移動・ジャンプ・接地判定・向き・ガードはこのクラスが直接担当する。
/// Update() が各系の処理を順に呼ぶ司令塔（集中制御）。
/// </summary>
public class PlayerController : MonoBehaviour
{
    // PlayerInputReader
    private PlayerInputReader inputReader;

    // Animator
    private Animator animator;

    // 攻撃エフェクトPrefab（未使用：将来整理予定）
    [SerializeField] private GameObject slashEffectPrefab;

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

    // 現在の行動状態（PlayerStateは独立ファイルPlayerState.csに定義）
    private PlayerState currentState = PlayerState.Idle;

    // =========================
    // コーディネーター公開インターフェース
    // 分割した各コンポーネント（PlayerCombat / PlayerItemActionなど）が
    // 状態と参照をここから取得する。状態と参照の唯一の持ち主はこのPlayerController。
    // =========================

    /// <summary>現在の行動状態（各コンポーネントから読み書きされる）</summary>
    public PlayerState CurrentState { get => currentState; set => currentState = value; }

    /// <summary>接地しているか</summary>
    public bool IsGrounded => isGrounded;

    /// <summary>右向きか</summary>
    public bool IsFacingRight => isFacingRight;

    /// <summary>Rigidbody2D参照</summary>
    public Rigidbody2D Rb => rb;

    /// <summary>Animator参照</summary>
    public Animator PlayerAnimator => animator;

    /// <summary>PlayerInputReader参照</summary>
    public PlayerInputReader InputReader => inputReader;

    /// <summary>InventoryManager参照</summary>
    public InventoryManager InventoryManager => inventoryManager;

    /// <summary>PlayerHealth参照</summary>
    public PlayerHealth Health => playerHealth;

    /// <summary>現在ガード中か</summary>
    public bool IsGuarding => isGuarding;

    /// <summary>
    /// 地上コンボの状態をリセットする。
    /// 射撃・ジャンプ攻撃などコンボを中断する行動の開始時に呼ぶ。
    /// 実体はPlayerCombat側にあるため委譲する。
    /// </summary>
    public void ResetComboState() => combat?.ResetComboState();

    // ジャンプ力
    [Header("ジャンプ")]
    [Tooltip("ジャンプ力")]
    [SerializeField]
    private float jumpPower = 12f;

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

    //==============================
    // SE
    //==============================

    [Header("SE")]

    [Tooltip("足音ループ再生用AudioSource（Loop=ON, Play On Awake=OFF）")]
    [SerializeField]
    private AudioSource footstepSource;

    [Tooltip("ジャンプ時のSE（2段ジャンプも共通）")]
    [SerializeField]
    private AudioClip jumpSE;

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

    /// <summary>
    /// 現在ガード中か
    /// </summary>
    [SerializeField]
    [Tooltip("現在ガード状態かどうか（デバッグ確認用）")]
    private bool isGuarding;

    private bool previousGuardState;

    // =========================
    // 分割コンポーネント参照
    // =========================

    // ジャンプ軌跡などの残像生成を担当する汎用コンポーネント。
    // Player以外（飛び道具など）でも同じコンポーネントを使い回せる。
    private AfterimageSpawner afterimageSpawner;

    // アイテム使用・射撃処理を担当するコンポーネント
    private PlayerItemAction itemAction;

    // 攻撃処理を担当するコンポーネント
    private PlayerCombat combat;

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

        // AfterimageSpawner取得（残像エフェクト用）
        afterimageSpawner = GetComponent<AfterimageSpawner>();

        // PlayerItemAction取得（アイテム使用・射撃用）
        itemAction = GetComponent<PlayerItemAction>();

        // PlayerCombat取得（攻撃用）
        combat = GetComponent<PlayerCombat>();
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

        // 足音制御：Idle状態（実際に自由に動けている状態）かつ接地中かつ移動入力があるときだけ再生
        // currentStateを見ることで、攻撃中などで横移動が止まっているのに足音だけ鳴り続ける矛盾を防ぐ
        bool shouldPlayFootstep = currentState == PlayerState.Idle && isGrounded && isRunning;

        if (footstepSource != null)
        {
            if (shouldPlayFootstep && !footstepSource.isPlaying)
            {
                footstepSource.Play();
            }
            else if (!shouldPlayFootstep && footstepSource.isPlaying)
            {
                footstepSource.Stop();
            }
        }

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
        if (currentState == PlayerState.Idle)
        {
            Flip();
        }

        // 攻撃入力処理（PlayerCombatへ委譲）
        // trueが返ったら以降の入力処理を打ち切る（元の早期return挙動を維持）
        if (combat != null && combat.HandleAttackInput())
        {
            return;
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

        // アイテム入力処理
        // Attack/JumpAttack入力判定の後に呼ぶことで、
        // 同一フレームにAttackとItemが同時入力されても
        // Attack側が先に currentState=JumpAttacking をセットするので
        // CanShoot()のcurrentStateチェックが正しく機能するようになる
        itemAction?.HandleShootInput();
    }

    // 物理演算用
    private void FixedUpdate()
    {
        // ゲーム開始前は移動停止
        if (!GameManager.IsGameStarted)
        {
            // 完全に停止させるために速度もゼロにする
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // ノックバック中は移動停止
        if (playerHealth.IsKnockback)
        {
            return;
        }

        // 攻撃中、アイテム中は横移動停止
        if (currentState == PlayerState.Attacking || currentState == PlayerState.JumpAttacking || currentState == PlayerState.Shooting)
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


        // 落下速度制限　落下距離が長くなると速くなりすぎるのを防止するため
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

        // ジャンプSE再生（2段ジャンプも同じSEで共通）
        SoundManager.Instance?.PlaySE(jumpSE);

        // Y方向速度リセット
        // 落下中でも一定ジャンプ力になる
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);

        // ジャンプ力を加える
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpPower);

        // ジャンプ回数加算
        jumpCount++;

        // ジャンプ軌跡用の残像トレイルを開始する（着地時にStopTrailで停止する）
        // 多重起動の防止はAfterimageSpawner.StartTrail()側で行うため、ここでは呼ぶだけでよい
        afterimageSpawner?.StartTrail();
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
        // 空中ジャンプ攻撃使用フラグリセット（実体はPlayerCombat側）
        combat?.ResetJumpAttackState();
        // 着地したので軌跡エフェクトの生成を停止する
        afterimageSpawner?.StopTrail();
        // 着地時にジャンプ攻撃状態を強制リセット（AnimationEventが呼ばれなかった場合のフォールバック）
        if (currentState == PlayerState.Attacking || currentState == PlayerState.JumpAttacking)
        {
            currentState = PlayerState.Idle;
        }
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

    // 攻撃中の移動開始前に、Groundがあるか確認する（攻撃移動などから呼ばれる地面センシング）
    public bool HasGroundAhead(float direction)
    {
        // GroundCheck位置から、向きに応じた距離だけ先の位置を計算する
        Vector2 checkPosition = (Vector2)groundCheck.position + Vector2.right * direction * attackMoveGroundCheckDistance;

        // その位置にGroundLayerがあるか確認する
        return Physics2D.OverlapCircle(checkPosition, groundCheckRadius, groundLayer);
    }

    // Scene上でGroundCheck確認用
    private void OnDrawGizmos()
    {
        // GroundCheck未設定なら終了
        if (groundCheck == null)
        {
            return;
        }

        // GroundCheck黄色
        Gizmos.color = Color.yellow;

        // GroundCheck位置から、向きに応じた距離だけ先の位置を計算する
        Vector2 checkPos = (Vector2)groundCheck.position + Vector2.right * (isFacingRight ? 1 : -1) * attackMoveGroundCheckDistance;
        // GroundCheck位置に円を描く
        Gizmos.DrawWireSphere(checkPos, groundCheckRadius);
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

    private void UpdateGuardState()
    {
        // Idle以外（攻撃中・ジャンプ攻撃中・アイテム使用中）はガード不可
        if (currentState != PlayerState.Idle)
        {
            isGuarding = false;
            return;
        }

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
        // Attack中・Projectile使用中・ジャンプ攻撃中ならジャンプできない
        if (currentState != PlayerState.Idle) return false;
        // ノックバック中ならジャンプできない
        if (playerHealth.IsKnockback) return false;
        // ガード中ならジャンプできない
        if (isGuarding) return false;
        // それ以外はジャンプできる
        return true;
    }
}
