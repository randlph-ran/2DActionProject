using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // PlayerInputReader
    private PlayerInputReader inputReader;

    // Animator
    private Animator animator;

    // 現在のコンボ段階（1〜3）
    private int comboStep = 0;

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

    // 攻撃中の移動用GroundCheck距離
    [SerializeField]
    private float attackMoveGroundCheckDistance = 0.6f;

    // Ground判定半径
    [SerializeField]
    private float groundCheckRadius = 0.1f;

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

    // Attack1浮かせ力
    [SerializeField]
    private float attack1LaunchPower = 1.8f;

    // Attack2浮かせ力
    [SerializeField]
    private float attack2LaunchPower = 0f;

    // Attack3浮かせ力
    [SerializeField]
    private float attack3LaunchPower = 0f;

    // 現在の浮かせ力
    private float currentLaunchPower;


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

    // Attack1用ノックバック
    [SerializeField]
    private float attack1Knockback = 2f;

    // Attack2用ノックバック
    [SerializeField]
    private float attack2Knockback = 3f;

    // Attack3用ノックバック
    [SerializeField]
    private float attack3Knockback = 8f;

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

    //Weight負け中移動停止フラグ
    private bool isBlocked;

    // 落下速度上限
    [SerializeField]
    private float maxFallSpeed = 12f;

    // 最初の向き右フラグ
    [SerializeField]
    private bool startFacingRight = true;

    // 攻撃中の移動距離(Attack1は多め、Attack3は少なめ)
    [SerializeField] private float attack1MoveDistance = 0.55f;
    [SerializeField] private float attack2MoveDistance = 0.55f;
    [SerializeField] private float attack3MoveDistance = 0.275f;

    // 攻撃中の移動速度
    private float attackMoveSpeed;

    // コンボ追尾対象 Attack1の攻撃開始時に設定され、Attack1～3の移動で追尾する
    private Transform comboTarget;

    // 攻撃中の移動がGround切れで途中終了したときに、攻撃終了まで移動させないようにするフラグ
    private bool isAttackLocked = false;
    // 攻撃終了後の硬直時間（調整ポイント）
    [SerializeField] private float attack1EndLock = 0.25f;
    [SerializeField] private float attack2EndLock = 0.12f;
    [SerializeField] private float attack3EndLock = 0.12f;

    private void Start()
    {
        // Rigidbody2D取得
        rb = GetComponent<Rigidbody2D>();
        // PlayerHealth取得
        playerHealth = GetComponent<PlayerHealth>();
        // Animator取得
        animator = GetComponent<Animator>();

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
    }

    // 毎フレーム実行
    // 入力取得を行う
    private void Update()
    {
        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            Debug.Log("IsGameStarted = " + GameManager.IsGameStarted);
        }

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

        // 左右入力 いったん旧入力システムで
        moveInput = inputReader.MoveInput.x;

        // 横入力があるか
        bool isRunning = Mathf.Abs(moveInput) > 0.1f;

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

        // 接地判定
        CheckGround();

        // 攻撃中は向き固定
        if (!isAttacking)
        {
            Flip();
        }

        // ジャンプ入力
        if (inputReader.JumpPressed)
        {
            Debug.Log(jumpCount + "回目");
            // 最大回数未満ならジャンプ可能
            if (jumpCount < maxJumpCount)
            {
                Jump();
            }
        }

        // 攻撃入力
        if (inputReader.AttackPressed)
        {
            // 攻撃ロック中はAttackできない
            if (isAttackLocked)
            {
                return;
            }

            if (!isAttacking)
            {
                HandleAttackInput();
            }
            else if (canNextCombo)
            {
                HandleAttackInput();
            }
            Debug.Log("攻撃開始");
            Debug.Log("現在コンボ段数：" + comboStep);
        }

        // 攻撃中の移動処理
        float direction = isFacingRight ? 1f : -1f;
        // 攻撃中の移動速度を設定
        rb.linearVelocity = new Vector2(direction * attackMoveSpeed, rb.linearVelocity.y);
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

        // 攻撃中は横移動停止
        if (isAttacking)
        {
            // 横移動停止
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

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
                // ダメージを与える
                enemyHealth.TakeDamage(attackDM, transform, currentKnockback, currentLaunchPower);
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
    // 次コンボ入力を許可する
    // Animation Event から呼ばれる
    public void EnableNextCombo()
    {
        canNextCombo = true;

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

    // Enemyと接触中
    private void OnCollisionStay2D(Collision2D collision)
    {
        // Enemyタグ以外なら終了
        if (!collision.gameObject.CompareTag("Enemy"))
        {
            return;
        }

        // EnemyAI取得
        EnemyAI enemyAI =
            collision.gameObject.GetComponent<EnemyAI>();

        // EnemyAI無ければ終了
        if (enemyAI == null)
        {
            return;
        }

        // Enemyの重さ取得
        int enemyWeight = enemyAI.WeightLevel;

        // =========================
        // 重量2 = Boss級
        // Playerが押し負ける
        // =========================

        if (enemyWeight >= 2)
        {
            // Player移動停止
            rb.linearVelocity =
                new Vector2(
                    0,
                    rb.linearVelocity.y
                );
        }

        // =========================
        // 重量1 = 同格
        // 互いに押せない
        // =========================

        else if (enemyWeight == 1)
        {
            // Player横移動停止
            rb.linearVelocity =
                new Vector2(
                    0,
                    rb.linearVelocity.y
                );
        }
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

    // 攻撃終了後の硬直処理をコルーチンで行う
    private IEnumerator AttackLockCoroutine()
    {
        isAttackLocked = true;

        // ★ここが硬直時間（調整ポイント）
        yield return new WaitForSeconds(0.4f);

        isAttackLocked = false;
    }
    private IEnumerator EndAttackRoutine(float lockTime)
    {
        // ここで即解除しない
        yield return new WaitForSeconds(lockTime);

        isAttacking = false;
        canNextCombo = false;

        comboStep = 0;
        animator.SetInteger("ComboStep", 0);

        comboTarget = null;
    }
}
