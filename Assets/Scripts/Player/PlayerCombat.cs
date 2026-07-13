using System.Collections;
using UnityEngine;

/// <summary>
/// Playerの攻撃処理を担当するコンポーネント。
/// 地上コンボ攻撃（Attack1〜3）・攻撃中の前進移動・コンボ追尾・ジャンプ攻撃を扱う。
/// 状態と参照は PlayerController（コーディネーター）から取得する。
/// 攻撃入力の判定は PlayerController.Update() から HandleAttackInput() を呼ぶことで行う（集中制御）。
/// Attack() / EndAttack() などは AnimationEvent から呼ばれる（同一GameObject上のため従来通り発火する）。
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    // =========================
    // Attack1設定
    // =========================
    [Header("攻撃 - Attack1")]

    [Tooltip("Attack1の攻撃位置オフセット")]
    [SerializeField]
    private Vector2 attack1Offset;

    [Tooltip("Attack1の攻撃範囲")]
    [SerializeField]
    private float attack1Radius = 1.0f;

    // =========================
    // Attack2設定
    // =========================
    [Header("攻撃 - Attack2")]

    [Tooltip("Attack2の攻撃位置オフセット")]
    [SerializeField]
    private Vector2 attack2Offset;

    [Tooltip("Attack2の攻撃範囲")]
    [SerializeField]
    private float attack2Radius = 1.2f;

    // =========================
    // Attack3設定
    // =========================
    [Header("攻撃 - Attack3")]

    [Tooltip("Attack3の攻撃位置オフセット")]
    [SerializeField]
    private Vector2 attack3Offset;

    [Tooltip("Attack3の攻撃範囲")]
    [SerializeField]
    private float attack3Radius = 1.5f;

    // =========================
    // 攻撃 - 効果
    // =========================
    [Header("攻撃 - 効果")]

    [Tooltip("Attack1浮かせ力")]
    [SerializeField]
    private float attack1LaunchPower = 1.8f;

    [Tooltip("Attack2浮かせ力")]
    [SerializeField]
    private float attack2LaunchPower = 0f;

    [Tooltip("Attack3浮かせ力")]
    [SerializeField]
    private float attack3LaunchPower = 0f;

    // =========================
    // 攻撃 - 共通
    // =========================
    [Header("攻撃 - 共通")]

    [SerializeField]
    private int attackDM = 1;

    [Tooltip("Attack1用ノックバック")]
    [SerializeField]
    private float attack1Knockback = 2f;

    [Tooltip("Attack2用ノックバック")]
    [SerializeField]
    private float attack2Knockback = 3f;

    [Tooltip("Attack3用ノックバック")]
    [SerializeField]
    private float attack3Knockback = 8f;

    [Tooltip("敵Layer")]
    [SerializeField]
    private LayerMask enemyLayer;

    // =========================
    // 攻撃 - 移動
    // =========================
    [Header("攻撃 - 移動")]

    [Tooltip("攻撃中の移動距離(Attack1は多め、Attack3は少なめ)")]
    [SerializeField] private float attack1MoveDistance = 0.55f;
    [Tooltip("攻撃中の移動距離(Attack1は多め、Attack3は少なめ)")]
    [SerializeField] private float attack2MoveDistance = 0.55f;
    [Tooltip("攻撃中の移動距離(Attack1は多め、Attack3は少なめ)")]
    [SerializeField] private float attack3MoveDistance = 0.275f;

    [Tooltip("攻撃終了後の硬直時間（調整ポイント）")]
    [SerializeField] private float attack1EndLock = 0.25f;
    [Tooltip("攻撃終了後の硬直時間（調整ポイント）")]
    [SerializeField] private float attack2EndLock = 0.12f;
    [Tooltip("攻撃終了後の硬直時間（調整ポイント）")]
    [SerializeField] private float attack3EndLock = 0.25f;

    // =========================
    // JumpAttack設定
    // =========================
    [Header("ジャンプ攻撃")]

    [Tooltip("前方判定")]
    [SerializeField]
    private Vector2 jumpAttackForwardOffset;

    [Tooltip("下方向判定")]
    [SerializeField]
    private Vector2 jumpAttackDownOffset;

    [Tooltip("判定半径")]
    [SerializeField]
    private float jumpAttackRadius = 1.0f;

    [Tooltip("ノックバック")]
    [SerializeField]
    private float jumpAttackKnockback = 10f;

    [Tooltip("ダメージ")]
    [SerializeField]
    private int jumpAttackDamage = 1;

    [Tooltip("ジャンプ攻撃中の移動停止(落下停止)")]
    [SerializeField]
    private float jumpAttackStopTime = 0.15f;

    // スタン係数 1.0 = 100% / 2.0 = 200% / 0.5 = 50%
    [Tooltip("スタン時間")]
    [SerializeField]
    private float jumpAttackStunRate = 1.0f;

    // =========================
    // SE
    // =========================
    [Header("SE")]

    [Tooltip("地上攻撃1段目のSE")]
    [SerializeField]
    private AudioClip attack1SE;

    [Tooltip("地上攻撃2段目のSE")]
    [SerializeField]
    private AudioClip attack2SE;

    [Tooltip("地上攻撃3段目のSE")]
    [SerializeField]
    private AudioClip attack3SE;

    [Tooltip("ジャンプ攻撃のSE")]
    [SerializeField]
    private AudioClip jumpAttackSE;

    // =========================
    // デバッグ/表示
    // =========================
    [Header("デバッグ/表示")]

    [Tooltip("攻撃範囲を常時表示するか")]
    [SerializeField]
    private bool alwaysShowAttackGizmo = true;

    [Tooltip("Gizmo表示時間")]
    [SerializeField]
    private float attackGizmoDuration = 0.2f;

    // =========================
    // 実行時の状態
    // =========================

    // コーディネーター参照（状態・各種参照の取得元）
    private PlayerController player;

    // 現在のコンボ段階（1〜3）
    private int comboStep = 0;

    // 次コンボへ進めるか
    private bool canNextCombo = false;

    // 現在の攻撃位置
    private Vector2 currentAttackOffset;

    // 現在の攻撃範囲
    private float currentAttackRadius;

    // 現在の浮かせ力
    private float currentLaunchPower;

    // コンボ追尾対象 Attack1の攻撃開始時に設定され、Attack1〜3の移動で追尾する
    private Transform comboTarget;

    // 攻撃中の移動がGround切れで途中終了したときに、攻撃終了まで移動させないようにするフラグ
    private bool isAttackLocked = false;

    // 空中ジャンプ攻撃使用済みか
    private bool hasUsedJumpAttack;

    // デバッグ用攻撃Gizmo表示フラグ
    private bool isAttackGizmoVisible = false;

    private void Awake()
    {
        // コーディネーター取得
        player = GetComponent<PlayerController>();
    }

    //==============================
    // 入力ディスパッチ（PlayerController.Update()から呼ばれる）
    //==============================

    /// <summary>
    /// 攻撃入力を処理する。
    /// 戻り値がtrueのとき、呼び出し元（Update）はその後の処理を打ち切る（元の早期return挙動を維持）。
    /// </summary>
    public bool HandleAttackInput()
    {
        // 攻撃入力が無ければ何もしない
        if (!player.InputReader.AttackPressed) return false;

        // 空中ならジャンプ攻撃
        if (!player.IsGrounded)
        {
            HandleJumpAttack();
            return true;
        }

        // 地上攻撃が可能か判定
        if (!CanAttack())
        {
            Debug.Log($"[INPUT] CanAttack()がfalseで弾かれた frame:{Time.frameCount}");
            return true;
        }
        // 攻撃ロック中なら攻撃できない
        if (isAttackLocked)
        {
            Debug.Log($"[INPUT] isAttackLockedで弾かれた frame:{Time.frameCount}");
            return true;
        }
        // 地上なら通常攻撃
        if (player.CurrentState != PlayerState.Attacking)
        {
            Debug.Log($"[INPUT] 新規攻撃開始 frame:{Time.frameCount}");
            AdvanceCombo();
        }
        // 攻撃中で、次コンボ受付中なら、次のコンボへ進める
        else if (canNextCombo)
        {
            Debug.Log($"[INPUT] コンボ継続 frame:{Time.frameCount}");
            AdvanceCombo();
        }
        else
        {
            Debug.Log($"[INPUT] 入力は来たが無視された(currentState={player.CurrentState}, canNextCombo=false) frame:{Time.frameCount}");
        }
        Debug.Log("攻撃開始");
        Debug.Log("現在コンボ段数：" + comboStep);
        return false;
    }

    //==============================
    // コンボ制御
    //==============================

    /// <summary>
    /// 攻撃入力時の処理
    /// ・コンボ段階を進める
    /// ・現在の攻撃範囲を設定する
    /// ・Animatorへ現在コンボを送る
    /// ・攻撃状態を開始する
    /// </summary>
    private void AdvanceCombo()
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

        // 現在のコンボ段階に応じて攻撃範囲設定を切り替える
        switch (comboStep)
        {
            case 1:
                currentAttackOffset = attack1Offset;
                currentAttackRadius = attack1Radius;
                Debug.Log(currentAttackOffset);
                Debug.Log(currentAttackRadius);
                break;

            case 2:
                currentAttackOffset = attack2Offset;
                currentAttackRadius = attack2Radius;
                Debug.Log(currentAttackOffset);
                Debug.Log(currentAttackRadius);
                break;

            case 3:
                currentAttackOffset = attack3Offset;
                currentAttackRadius = attack3Radius;
                Debug.Log(currentAttackOffset);
                Debug.Log(currentAttackRadius);
                break;
        }

        // Animatorへ現在のコンボ段階を送る（これによってAttack1〜3を切り替える）
        player.PlayerAnimator.SetInteger("ComboStep", comboStep);

        // Attack開始Trigger
        // Attack1開始時のみTriggerを送る。Attack2以降はAnimator側の遷移で継続する
        if (comboStep == 1)
        {
            player.PlayerAnimator.SetTrigger("Attack");
        }

        // 攻撃中状態ON（移動停止や向き固定に使用）
        player.CurrentState = PlayerState.Attacking;
    }

    /// <summary>
    /// 地上コンボの状態をリセットする。
    /// 射撃・ジャンプ攻撃などコンボを中断する行動の開始時にコーディネーター経由で呼ばれる。
    /// </summary>
    public void ResetComboState()
    {
        canNextCombo = false;
        comboStep = 0;
        player.PlayerAnimator.SetInteger("ComboStep", 0);
    }

    /// <summary>
    /// 着地時にジャンプ攻撃の使用済みフラグをリセットする（コーディネーターの着地処理から呼ばれる）。
    /// </summary>
    public void ResetJumpAttackState()
    {
        hasUsedJumpAttack = false;
    }

    // 次コンボ入力を許可する（Animation Event から呼ばれる）
    public void EnableNextCombo()
    {
        canNextCombo = true;
        Debug.Log($"[ANIM EVENT] EnableNextCombo発火 frame:{Time.frameCount} comboStep:{comboStep}");
        Debug.Log("次コンボ受付開始");
    }

    //==============================
    // 攻撃判定
    //==============================

    // 攻撃処理（Animation Event から呼ばれる）
    public void Attack()
    {
        Debug.Log("アタック！");
        // 攻撃Gizmo表示開始
        StartCoroutine(ShowAttackGizmo());

        // 向きによって攻撃位置反転
        Vector2 attackPosition = (Vector2)transform.position + new Vector2(currentAttackOffset.x * (player.IsFacingRight ? 1 : -1), currentAttackOffset.y);

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
    private IEnumerator ShowAttackGizmo()
    {
        // 表示ON
        isAttackGizmoVisible = true;

        // 指定時間待機
        yield return new WaitForSeconds(attackGizmoDuration);

        // 表示OFF
        isAttackGizmoVisible = false;
    }

    // 攻撃終了処理（Animation Event から呼ばれる）
    public void EndAttack()
    {
        Debug.Log("EndAttack呼ばれた");

        if (player.PlayerAnimator.IsInTransition(0))
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

    // 攻撃終了処理をコルーチンで行う
    private IEnumerator EndAttackRoutine(float lockTime)
    {
        Debug.Log($"[END] EndAttackRoutine開始 frame:{Time.frameCount} comboStep:{comboStep} lockTime:{lockTime}");
        // ここで即解除しない
        yield return new WaitForSeconds(lockTime);

        player.CurrentState = PlayerState.Idle;
        canNextCombo = false;

        comboStep = 0;
        player.PlayerAnimator.SetInteger("ComboStep", 0);

        comboTarget = null;
    }

    //==============================
    // 攻撃中の前進移動
    //==============================

    // 攻撃中の移動を開始する
    private void StartAttackMove(float distance, float duration)
    {
        StartCoroutine(AttackMoveCoroutine(distance, duration));
    }

    // 攻撃中の移動をコルーチンで処理する
    private IEnumerator AttackMoveCoroutine(float distance, float duration)
    {
        // 移動量を計算する
        float moved = 0f;
        // 向きに応じた移動方向を設定する
        float direction = player.IsFacingRight ? 1f : -1f;
        // 攻撃中の移動処理
        while (moved < distance)
        {
            // ノックバック中は前進を中断してノックバックを優先する
            if (player.Health.IsKnockback)
            {
                yield break;
            }
            // Groundがないなら移動終了（地面センシングはコーディネーターに集約）
            if (!player.HasGroundAhead(direction))
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
        StartAttackMove(attack1MoveDistance, 0.19f);
    }
    // Attack2とAttack3は距離短め、時間も短めにして、素早く動いて攻撃する感じにする
    public void Attack2MoveStart()
    {
        StartAttackMove(attack2MoveDistance, 0.10f);
    }
    public void Attack3MoveStart()
    {
        StartAttackMove(attack3MoveDistance, 0.10f);
    }

    //==============================
    // コンボ追尾
    //==============================

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
        Transform nearestEnemy = null;

        // 攻撃範囲内の敵全てを確認する
        foreach (Collider2D enemy in hitEnemies)
        {
            // 敵までの距離を計算する
            float distance = Mathf.Abs(enemy.transform.position.x - transform.position.x);

            // 最も近い敵を更新する
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestEnemy = enemy.transform;
            }
        }
        // 追尾対象を最も近い敵にする
        comboTarget = nearestEnemy;
    }

    // コンボ追尾対象に向き直る（Animation Event から呼ばれる）
    public void FaceComboTarget()
    {
        // 追尾対象がいないなら終了
        if (comboTarget == null)
        {
            return;
        }
        // 追尾対象の位置に向き直る（向き制御はコーディネーターに集約）
        player.FaceEnemy(comboTarget.position);
    }

    //==============================
    // ジャンプ攻撃
    //==============================

    // 空中ジャンプ攻撃の入力処理
    private void HandleJumpAttack()
    {
        Debug.Log($"ジャンプ攻撃 frame:{Time.frameCount}");

        // アイテム使用中はJumpAttack不可
        // 仕様：Item中 → JumpAttack ×
        // 先にItem入力があった場合はそちらを優先するため弾く
        if (player.CurrentState == PlayerState.Shooting)
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
        player.PlayerAnimator.SetInteger("ComboStep", 0);

        // ジャンプ攻撃状態ON
        player.CurrentState = PlayerState.JumpAttacking;
        // ジャンプ攻撃使用済みにする
        hasUsedJumpAttack = true;

        // ジャンプ攻撃Trigger
        player.PlayerAnimator.SetTrigger("JumpAttack");
        Debug.Log("ジャンプ攻撃");

        // 落下停止
        StartCoroutine(JumpAttackStopCoroutine());
    }

    // 空中ジャンプ攻撃の判定処理（Animation Event から呼ばれる）
    public void JumpAttack()
    {
        // 向きによって攻撃位置反転
        Vector2 forwardPos = (Vector2)transform.position + new Vector2(jumpAttackForwardOffset.x * (player.IsFacingRight ? 1 : -1), jumpAttackForwardOffset.y);

        // 下方向は向き関係なく一定
        Vector2 downPos = (Vector2)transform.position + new Vector2(jumpAttackDownOffset.x * (player.IsFacingRight ? 1 : -1), jumpAttackDownOffset.y);

        // 前方と下方向の攻撃範囲内のEnemy取得
        Collider2D[] forwardHits = Physics2D.OverlapCircleAll(forwardPos, jumpAttackRadius, enemyLayer);
        Collider2D[] downHits = Physics2D.OverlapCircleAll(downPos, jumpAttackRadius, enemyLayer);
        // ダメージとノックバックを与える処理
        DamageEnemies(forwardHits);
        DamageEnemies(downHits);
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
        player.Rb.linearVelocity = Vector2.zero;

        // 重力無効
        float originalGravity = player.Rb.gravityScale;
        player.Rb.gravityScale = 0f;

        yield return new WaitForSeconds(jumpAttackStopTime);

        // 重力復帰
        player.Rb.gravityScale = originalGravity;
    }

    // ジャンプ攻撃終了処理（Animation Event から呼ばれる）
    public void EndJumpAttack()
    {
        player.CurrentState = PlayerState.Idle;
    }

    //==============================
    // SE（Animation Event から呼ばれる）
    //==============================

    /// <summary>
    /// 攻撃SE再生（AnimationEventから呼ぶ。各Attackクリップの発生フレームに配置する）
    /// </summary>
    public void PlayAttack1SE() => SoundManager.Instance?.PlaySE(attack1SE);
    public void PlayAttack2SE() => SoundManager.Instance?.PlaySE(attack2SE);
    public void PlayAttack3SE() => SoundManager.Instance?.PlaySE(attack3SE);

    /// <summary>
    /// ジャンプ攻撃SE再生（AnimationEventから呼ぶ）
    /// </summary>
    public void PlayJumpAttackSE() => SoundManager.Instance?.PlaySE(jumpAttackSE);

    //==============================
    // 内部ユーティリティ
    //==============================

    // 地上攻撃入力を受け付けてよいか判定
    private bool CanAttack()
    {
        // Item使用中なら地上攻撃出せない
        if (player.CurrentState == PlayerState.Shooting) return false;
        // 空中なら地上攻撃出せない
        if (!player.IsGrounded) return false;
        // ジャンプ攻撃中なら地上攻撃出せない
        if (player.CurrentState == PlayerState.JumpAttacking) return false;
        // ノックバック中なら地上攻撃出せない
        if (player.Health.IsKnockback) return false;
        // ガード中なら地上攻撃出せない
        if (player.IsGuarding) return false;
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

    //==============================
    // Gizmo（攻撃範囲・ジャンプ攻撃範囲）
    //==============================

    private void OnDrawGizmos()
    {
        // 向き取得（編集モードではデフォルト右向きになる）
        PlayerController pc = GetComponent<PlayerController>();
        bool facingRight = pc == null || pc.IsFacingRight;

        // 攻撃範囲表示
        Vector2 gizmoPosition = (Vector2)transform.position + new Vector2(currentAttackOffset.x * (facingRight ? 1 : -1), currentAttackOffset.y);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(gizmoPosition, currentAttackRadius);

        // 攻撃中 or 常時表示
        if (isAttackGizmoVisible || alwaysShowAttackGizmo)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(gizmoPosition, currentAttackRadius);
        }

        // ジャンプ攻撃のGizmo表示
        Gizmos.color = Color.cyan;
        Vector2 forwardPos = (Vector2)transform.position + new Vector2(jumpAttackForwardOffset.x * (facingRight ? 1 : -1), jumpAttackForwardOffset.y);
        Vector2 downPos = (Vector2)transform.position + new Vector2(jumpAttackDownOffset.x * (facingRight ? 1 : -1), jumpAttackDownOffset.y);
        Gizmos.DrawWireSphere(forwardPos, jumpAttackRadius);
        Gizmos.DrawWireSphere(downPos, jumpAttackRadius);
    }
}
