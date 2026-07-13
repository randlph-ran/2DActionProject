using System.Collections;
using UnityEngine;

/// <summary>
/// Playerのコーディネーター。
/// 状態（行動状態・接地・向き）と各種参照の唯一の持ち主となり、
/// 分割した各コンポーネント（PlayerCombat / PlayerItemAction / AfterimageSpawner）へ
/// それらを提供する。移動・ジャンプ・接地判定・向き・ガードはこのクラス自身が担当する。
/// Update() が各系の処理を順に呼ぶ司令塔（集中制御）。
/// </summary>
public class PlayerController : MonoBehaviour
{
    //==============================
    // 参照（Awakeで取得）
    //==============================

    // PlayerInputReader
    private PlayerInputReader inputReader;

    // Animator
    private Animator animator;

    // Rigidbody2D
    private Rigidbody2D rb;

    // PlayerHealth
    private PlayerHealth playerHealth;

    // InventoryManager
    private InventoryManager inventoryManager;

    // 残像生成（ジャンプ軌跡など。Player以外でも使い回せる汎用コンポーネント）
    private AfterimageSpawner afterimageSpawner;

    // アイテム使用・射撃処理
    private PlayerItemAction itemAction;

    // 攻撃処理
    private PlayerCombat combat;

    //==============================
    // 調整値（Inspector）
    //==============================

    [Header("移動")]
    [Tooltip("移動速度")]
    [SerializeField]
    private float moveSpeed = 5f;

    [Header("ジャンプ")]
    [Tooltip("ジャンプ力")]
    [SerializeField]
    private float jumpPower = 12f;

    [Tooltip("最大ジャンプ回数")]
    [SerializeField]
    private int maxJumpCount = 2;

    [Header("接地判定")]
    [Tooltip("GroundCheck位置")]
    [SerializeField]
    private Transform groundCheck;

    [Tooltip("攻撃中の移動用GroundCheck距離")]
    [SerializeField]
    private float attackMoveGroundCheckDistance = 0.6f;

    [Tooltip("Ground判定半径")]
    [SerializeField]
    private float groundCheckRadius = 0.1f;

    [Tooltip("GroundLayer")]
    [SerializeField]
    private LayerMask groundLayer;

    [Header("SE")]
    [Tooltip("足音ループ再生用AudioSource（Loop=ON, Play On Awake=OFF）")]
    [SerializeField]
    private AudioSource footstepSource;

    [Tooltip("ジャンプ時のSE（2段ジャンプも共通）")]
    [SerializeField]
    private AudioClip jumpSE;

    [Header("落下/初期設定")]
    [Tooltip("落下速度上限")]
    [SerializeField]
    private float maxFallSpeed = 10f;

    [Tooltip("最初の向き右フラグ")]
    [SerializeField]
    private bool startFacingRight = true;

    [Header("ノックバック")]
    [Tooltip("ノックバックStateのセーフティ解除時間\nAnimation Event(EndKnockback)が来なくてもフリーズしないよう、この時間で強制的にIdleへ戻す。\nノックバックアニメの長さより少し長めに設定する")]
    [SerializeField]
    private float knockbackSafetyTime = 1.0f;

    [Header("デバッグ")]
    [Tooltip("現在ガード状態かどうか（デバッグ確認用）")]
    [SerializeField]
    private bool isGuarding;

    //==============================
    // 実行時の状態
    //==============================

    // 現在の行動状態（PlayerStateは独立ファイルPlayerState.csに定義）
    private PlayerState currentState = PlayerState.Idle;

    // 左右入力
    private float moveInput;

    // 右向き判定
    private bool isFacingRight = true;

    // 接地判定
    private bool isGrounded;

    // 現在のジャンプ回数
    private int jumpCount = 0;

    // Weight負け中移動停止フラグ
    private bool isBlocked;

    // 直前フレームのガード状態（ログ用）
    private bool previousGuardState;

    // ノックバックStateのセーフティ解除監視コルーチン
    private Coroutine knockbackTimeoutCoroutine;

    //==============================
    // コーディネーター公開インターフェース
    // 分割した各コンポーネント（PlayerCombat / PlayerItemActionなど）が
    // 状態と参照をここから取得する。状態と参照の唯一の持ち主はこのPlayerController。
    //==============================

    /// <summary>現在の行動状態（各コンポーネントから読み書きされる）</summary>
    public PlayerState CurrentState { get => currentState; set => currentState = value; }

    /// <summary>接地しているか</summary>
    public bool IsGrounded => isGrounded;

    /// <summary>右向きか</summary>
    public bool IsFacingRight => isFacingRight;

    /// <summary>現在ガード中か</summary>
    public bool IsGuarding => isGuarding;

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

    /// <summary>
    /// 地上コンボの状態をリセットする。
    /// 射撃・ジャンプ攻撃などコンボを中断する行動の開始時に呼ぶ。
    /// 実体はPlayerCombat側にあるため委譲する。
    /// </summary>
    public void ResetComboState() => combat?.ResetComboState();

    /// <summary>
    /// ノックバック状態へ入る（PlayerHealthの被弾処理から呼ばれる）。
    /// 行動状態をKnockbackにし、専用アニメを再生する。
    /// この状態の間は移動・ジャンプ・攻撃などPlayerの行動が全て無効になる
    /// （各ゲートが Health.IsKnockback = CurrentState==Knockback を見ているため）。
    /// </summary>
    public void EnterKnockback()
    {
        currentState = PlayerState.Knockback;

        // 進行中の攻撃コンボを中断する
        combat?.ResetComboState();

        // ノックバックアニメ再生
        animator.SetTrigger("Knockback");

        // セーフティ：Animation Event(EndKnockback)が来なくてもフリーズしないよう監視を開始する
        if (knockbackTimeoutCoroutine != null)
        {
            StopCoroutine(knockbackTimeoutCoroutine);
        }
        knockbackTimeoutCoroutine = StartCoroutine(KnockbackSafetyTimeout());
    }

    /// <summary>
    /// ノックバック状態を終了してIdleへ戻す。
    /// ノックバックアニメ末尾の Animation Event から呼ばれる。
    /// </summary>
    public void EndKnockback()
    {
        // 既にKnockback以外へ遷移していれば何もしない（多重呼び出し対策）
        if (currentState == PlayerState.Knockback)
        {
            currentState = PlayerState.Idle;
        }

        // セーフティ監視を停止
        if (knockbackTimeoutCoroutine != null)
        {
            StopCoroutine(knockbackTimeoutCoroutine);
            knockbackTimeoutCoroutine = null;
        }
    }

    // Animation Event(EndKnockback)が発火しなかった場合のフォールバック
    private IEnumerator KnockbackSafetyTimeout()
    {
        yield return new WaitForSeconds(knockbackSafetyTime);

        // まだKnockbackのままなら強制的にIdleへ戻す
        if (currentState == PlayerState.Knockback)
        {
            Debug.LogWarning("[Knockback] Animation EventのEndKnockbackが来なかったため強制解除しました");
            currentState = PlayerState.Idle;
        }
        knockbackTimeoutCoroutine = null;
    }

    //==============================
    // Unityライフサイクル
    //==============================

    // ゲーム開始時に最初に呼ばれる
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerHealth = GetComponent<PlayerHealth>();
        animator = GetComponent<Animator>();
        inputReader = GetComponent<PlayerInputReader>();
        inventoryManager = GetComponent<InventoryManager>();

        // 分割コンポーネント（同一GameObject上）
        afterimageSpawner = GetComponent<AfterimageSpawner>();
        itemAction = GetComponent<PlayerItemAction>();
        combat = GetComponent<PlayerCombat>();
    }

    private void Start()
    {
        // 初期向き設定
        InitFacingDirection();
    }

    // 毎フレームの司令塔。各系の処理を上から順に呼ぶ
    private void Update()
    {
        // ゲーム開始前：接地判定だけ更新し、Animatorを停止状態にする
        if (!GameManager.IsGameStarted)
        {
            CheckGround();
            animator.SetBool("isGrounded", isGrounded);
            animator.SetBool("isRunning", false);
            // 開始演出時の落下アニメループ防止のため落下速度を0にする
            animator.SetFloat("verticalSpeed", 0);
            return;
        }

        // 入力・接地・ガード状態の更新
        moveInput = inputReader.MoveInput.x;
        CheckGround();
        UpdateGuardState();

        // 移動状態を判定してAnimatorと足音へ反映
        // ノックバック中は走りアニメを混入させない（専用のノックバックアニメを優先する）
        bool isRunning = Mathf.Abs(moveInput) > 0.1f && !isGuarding && currentState != PlayerState.Knockback;
        UpdateAnimatorParams(isRunning);
        UpdateFootstep(isRunning);

        // 行動不能条件（ノックバック中・ガード中は以降の入力を受け付けない）
        if (playerHealth.IsKnockback) return;
        if (isGuarding) return;

        // 攻撃中・アイテム中は向き固定。Idle時のみ入力方向へ向く
        if (currentState == PlayerState.Idle)
        {
            Flip();
        }

        // 攻撃入力（PlayerCombatへ委譲）
        // trueが返ったら以降の入力処理を打ち切る（元の早期return挙動を維持）
        if (combat != null && combat.HandleAttackInput())
        {
            return;
        }

        // ジャンプ入力
        HandleJumpInput();

        // アイテム入力（PlayerItemActionへ委譲）
        // Attack/JumpAttack入力判定の後に呼ぶことで、
        // 同一フレームにAttackとItemが同時入力されても
        // Attack側が先に currentState=JumpAttacking をセットするので
        // CanShoot()のcurrentStateチェックが正しく機能するようになる
        itemAction?.HandleShootInput();
    }

    // 物理演算用。横移動と落下速度制限を担当する
    private void FixedUpdate()
    {
        // ゲーム開始前は完全停止
        if (!GameManager.IsGameStarted)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // ノックバック中は横速度を上書きしない（ノックバックの勢いを保つため、ここでは何もしない）
        if (playerHealth.IsKnockback)
        {
            return;
        }

        // 攻撃/アイテム中・ガード中・Weight負け中は横移動を止めて終了
        if (ShouldStopHorizontalMove())
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        // 左右移動
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        // 落下速度制限（落下距離が長くなると速くなりすぎるのを防止する）
        if (rb.linearVelocity.y < -maxFallSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
        }
    }

    // 横移動を停止すべき状態か（攻撃/アイテム中・ガード中・Weight負け中）。
    // ※ノックバックは含めない（勢いを保つためFixedUpdate側で個別にreturnしている）
    private bool ShouldStopHorizontalMove()
    {
        if (currentState == PlayerState.Attacking
            || currentState == PlayerState.JumpAttacking
            || currentState == PlayerState.Shooting) return true;
        if (isGuarding) return true;
        if (isBlocked) return true;
        return false;
    }

    //==============================
    // Animator / 足音
    //==============================

    // 移動・接地・ガード・縦速度をAnimatorへ反映する
    private void UpdateAnimatorParams(bool isRunning)
    {
        animator.SetBool("isGuarding", isGuarding);
        animator.SetBool("isRunning", isRunning);
        animator.SetBool("isGrounded", isGrounded);
        animator.SetFloat("verticalSpeed", rb.linearVelocity.y);
    }

    // 足音制御：Idle状態かつ接地中かつ移動入力があるときだけ再生する。
    // currentStateを見ることで、攻撃中などで横移動が止まっているのに足音だけ鳴り続ける矛盾を防ぐ
    private void UpdateFootstep(bool isRunning)
    {
        if (footstepSource == null) return;

        bool shouldPlay = currentState == PlayerState.Idle && isGrounded && isRunning;

        if (shouldPlay && !footstepSource.isPlaying)
        {
            footstepSource.Play();
        }
        else if (!shouldPlay && footstepSource.isPlaying)
        {
            footstepSource.Stop();
        }
    }

    //==============================
    // ジャンプ
    //==============================

    // ジャンプ入力の受付
    private void HandleJumpInput()
    {
        // 攻撃中・アイテム使用中・ガード中はジャンプできない
        if (!CanJump()) return;
        // ジャンプ入力が無ければ終了
        if (!inputReader.JumpPressed) return;
        // ジャンプカウントが最大未満なら（2段）ジャンプする
        if (jumpCount < maxJumpCount)
        {
            Jump();
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

        // ジャンプSE再生（2段ジャンプも同じSEで共通）
        SoundManager.Instance?.PlaySE(jumpSE);

        // Y方向速度をリセットしてからジャンプ力を加える（落下中でも一定のジャンプ力になる）
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpPower);

        // ジャンプ回数加算
        jumpCount++;

        // ジャンプ軌跡用の残像トレイルを開始する（着地時にStopTrailで停止する）
        // 多重起動の防止はAfterimageSpawner.StartTrail()側で行うため、ここでは呼ぶだけでよい
        afterimageSpawner?.StartTrail();
    }

    // ジャンプ入力を受け付けてよいか判定
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

    //==============================
    // 接地判定
    //==============================

    // 接地判定
    private void CheckGround()
    {
        bool wasGrounded = isGrounded;

        // GroundCheck位置を中心にした円がGroundLayerに触れているかを調べる
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
        }
    }

    // 着地時の各種リセット
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

    // 前方に地面があるか確認する（攻撃移動などから呼ばれる地面センシング）
    public bool HasGroundAhead(float direction)
    {
        // GroundCheck位置から、向きに応じた距離だけ先の位置を計算する
        Vector2 checkPosition = (Vector2)groundCheck.position + Vector2.right * direction * attackMoveGroundCheckDistance;

        // その位置にGroundLayerがあるか確認する
        return Physics2D.OverlapCircle(checkPosition, groundCheckRadius, groundLayer);
    }

    //==============================
    // 向き
    //==============================

    // 入力方向に応じて向きを変更する
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

        // ScaleのXを反転して適用する
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    // 指定位置の方向へ向き直る（Enemy追尾・被弾時などから呼ばれる）
    public void FaceEnemy(Vector2 enemyPosition)
    {
        // 対象が右側にいるか
        bool enemyIsRight = enemyPosition.x > transform.position.x;

        // 右にいて左向きなら反転
        if (enemyIsRight && !isFacingRight)
        {
            Turn();
        }
        // 左にいて右向きなら反転
        else if (!enemyIsRight && isFacingRight)
        {
            Turn();
        }
    }

    // シーンごとの初期向きを設定する
    private void InitFacingDirection()
    {
        isFacingRight = startFacingRight;

        // 初期向きに合わせてScaleのX符号を決める
        Vector3 scale = transform.localScale;
        scale.x = startFacingRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    //==============================
    // ガード
    //==============================

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

    //==============================
    // その他
    //==============================

    // Weight負け（重量1のEnemyとの接触）中の移動停止フラグを設定する
    public void SetBlocked(bool blocked)
    {
        isBlocked = blocked;
    }

    // Scene上でGroundCheck位置を確認するためのGizmo
    private void OnDrawGizmos()
    {
        if (groundCheck == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;

        // GroundCheck位置から、向きに応じた距離だけ先の位置に円を描く
        Vector2 checkPos = (Vector2)groundCheck.position + Vector2.right * (isFacingRight ? 1 : -1) * attackMoveGroundCheckDistance;
        Gizmos.DrawWireSphere(checkPos, groundCheckRadius);
    }
}
