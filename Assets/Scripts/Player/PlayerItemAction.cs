using System.Collections;
using UnityEngine;

/// <summary>
/// Playerのアイテム使用・飛び道具発射を担当するコンポーネント。
/// 状態と参照は PlayerController（コーディネーター）から取得する。
/// 入力判定は PlayerController.Update() から HandleShootInput() を呼ぶことで行う（集中制御）。
/// ShootProjectile() / EndShoot() は AnimationEvent から呼ばれる（同一GameObject上のため従来通り発火する）。
/// </summary>
public class PlayerItemAction : MonoBehaviour
{
    [Header("SE")]

    [Tooltip("飛び道具発射ボタン入力時のSE")]
    [SerializeField]
    private AudioClip projectileSE;

    [Tooltip("回復アイテム使用時のSE（ShootProjectile()のAnimationEventと同フレームで再生）")]
    [SerializeField]
    private AudioClip recoverySE;

    // コーディネーター参照（状態・各種参照の取得元）
    private PlayerController player;

    // 次回発射可能時間
    private float nextShootTime;

    // 発射方向キャッシュ
    private Vector2 cachedShootDirection;

    // 発射済みフラグ（AnimationEventで発射処理を呼ぶため、1回だけ発射するようにする）
    private bool hasShot = false;

    // 空中発射中に停止させた重力の復元用
    private float shootOriginalGravity;

    // 空中発射時に重力を停止したか
    private bool isShootGravityFrozen;

    // 発射タイムアウト監視用コルーチン
    private Coroutine shootTimeoutCoroutine;

    // 現在装備中のアイテム（InventoryManagerが管理する装備状態をそのまま参照する）
    private ItemData CurrentItem =>
        player.InventoryManager != null ? player.InventoryManager.EquippedItem : null;

    private void Awake()
    {
        // コーディネーター取得
        player = GetComponent<PlayerController>();
    }

    /// <summary>
    /// Projectileを発射する処理
    /// AnimationEvent から呼ぶ
    /// </summary>
    private void ShootProjectile()
    {
        Debug.Log($"ShootProjectile呼び出し frame:{Time.frameCount} clip:{player.PlayerAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name}");
        // currentStateがShootingでなければ発射しない
        // アニメ遷移の巻き込みによる誤発火を防ぐ
        if (player.CurrentState != PlayerState.Shooting) return;
        if (hasShot) return; // 1回押しにつき1発のみ
        Debug.Log("ShootProjectile");

        if (CurrentItem == null) return;

        // 回復アイテムならHPを回復して消費するだけで終了（Projectile生成はしない）
        if (CurrentItem.ItemType == ItemType.Recovery)
        {
            // 回復SE再生（AnimationEventのこのタイミングと同フレームで再生）
            SoundManager.Instance?.PlaySE(recoverySE);
            player.Health.Heal(CurrentItem.Value);
            ConsumeItemUse();
            hasShot = true; // 1回押しにつき1回のみ実行されるようにする
            return;
        }

        if (CurrentItem.ItemType != ItemType.Projectile) return;

        // GetShootDirection() ではなくキャッシュを使う
        Vector2 direction = cachedShootDirection;

        int dmg = CurrentItem.Value;
        float knockback = CurrentItem.KnockbackPower;
        float launch = CurrentItem.LaunchPower;

        Vector3 spawnPos = transform.position + (Vector3)(direction * 0.5f);
        Projectile proj = Instantiate(
            CurrentItem.ProjectilePrefab.GetComponent<Projectile>(),
            spawnPos,
            Quaternion.identity
        );
        Debug.Log($"Projectile生成 pos:{spawnPos} direction:{direction} instanceID:{proj.GetInstanceID()}");
        proj.Initialize(direction, dmg, knockback, launch, gameObject);
        ConsumeItemUse();
    }

    // 8方向対応の射撃方向決定処理
    private Vector2 GetShootDirection()
    {
        // 方向ベクトル
        Vector2 dir = Vector2.zero;
        // 入力取得
        Vector2 input = player.InputReader.MoveInput;

        // 入力がなければ向きで決定
        if (input == Vector2.zero)
        {
            // 右向きなら右、左向きなら左
            return player.IsFacingRight ? Vector2.right : Vector2.left;
        }

        // 8方向そのまま正規化
        dir = new Vector2(input.x, input.y);
        // 斜めも含めて正規化して返す
        return dir.normalized;
    }

    /// <summary>
    /// アイテム使用回数を消費する
    /// 所持数を1消費し、0になればInventoryManager側で自動的に装備解除される
    /// </summary>
    private void ConsumeItemUse()
    {
        if (player.InventoryManager == null)
        {
            return;
        }

        // 装備中アイテムを使用（内部で所持数を1消費する）
        ItemData usedItem = player.InventoryManager.UseEquippedItem();

        if (usedItem != null)
        {
            Debug.Log("残り所持数：" + player.InventoryManager.GetItemCount(usedItem));
        }
    }

    /// <summary>
    /// アイテム使用可能か判定
    /// </summary>
    private bool CanShoot()
    {
        Debug.Log($"CanShoot呼び出し frame:{Time.frameCount} currentState:{player.CurrentState} isGrounded:{player.IsGrounded}");

        Debug.Log($"CanShoot確認 frame:{Time.frameCount} - currentState:{player.CurrentState} / IsInTransition:{player.PlayerAnimator.IsInTransition(0)}");
        // 未装備
        if (CurrentItem == null)
        {
            return false;
        }

        // 空中使用不可のアイテムなら、地上にいない場合は使用不可
        if (!CurrentItem.CanUseInAir && !player.IsGrounded)
        {
            return false;
        }

        // 攻撃中・アイテム使用中・ジャンプ攻撃中はItem使用不可
        if (player.CurrentState != PlayerState.Idle)
        {
            return false;
        }

        // ノックバック中
        if (player.Health.IsKnockback)
        {
            return false;
        }

        // ガード中
        if (player.IsGuarding)
        {
            return false;
        }

        // Animator遷移中
        // アニメが切り替わる途中フレームでItem Triggerをセットすると
        // AnimatorがTriggerを消費できずに残留し、isShooting=trueのまま
        // EndShoot()が呼ばれない行動不能バグの原因になるため弾く
        if (player.PlayerAnimator.IsInTransition(0))
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
    /// 射撃入力処理（PlayerController.Update() から毎フレーム呼ばれる）
    /// </summary>
    public void HandleShootInput()
    {
        if (CurrentItem == null) return;

        if (CurrentItem.CanAutoFire)
        {
            if (player.InputReader.ShootHeld && CanShoot())
            {
                Debug.Log($"StartShooting呼び出し(AutoFire) frame:{Time.frameCount}");
                StartShooting();
            }
        }
        else
        {
            if (player.InputReader.ShootPressed && CanShoot())
            {
                Debug.Log($"StartShooting呼び出し(Press) frame:{Time.frameCount}");
                StartShooting();
            }
        }
    }

    /// <summary>
    /// アイテム使用開始の共通処理
    /// </summary>
    private void StartShooting()
    {
        hasShot = false; // 発射ボタンを押すたびにリセット

        // 飛び道具なら入力時点でSE再生（命中・不命中は問わない）
        // 回復はShootProjectile()内のAnimationEventタイミングで別途再生する
        if (CurrentItem.ItemType == ItemType.Projectile)
        {
            SoundManager.Instance?.PlaySE(projectileSE);
        }

        // ボタンを押した瞬間の方向を保存
        cachedShootDirection = GetShootDirection();

        // Animator用の射撃方向 0 = 横
        int shootDirection = 0;

        // 1 = 上方向
        if (cachedShootDirection.y > 0.5f)
        {
            shootDirection = 1;
        }
        // 2 = 下方向
        else if (cachedShootDirection.y < -0.5f)
        {
            shootDirection = 2;
        }

        // Animatorへ送る
        player.PlayerAnimator.SetInteger("ShootDirection", shootDirection);

        // アイテム種別をAnimatorへ送る（0:Projectile, 1:Recovery, 2:Special）
        player.PlayerAnimator.SetInteger("ItemType", (int)CurrentItem.ItemType);

        // 地上コンボ状態を終了（コンボ管理はコーディネーター側にあるため委譲する）
        player.ResetComboState();

        // 空中発射時は重力と速度を停止し、座標を固定する
        // 着地と発射アニメ完走のタイミングが競合して
        // 地上Stateへ誤って引き込まれる不安定挙動を防ぐ
        isShootGravityFrozen = !player.IsGrounded;
        if (isShootGravityFrozen)
        {
            shootOriginalGravity = player.Rb.gravityScale;
            player.Rb.gravityScale = 0f;
            player.Rb.linearVelocity = Vector2.zero;
        }

        player.CurrentState = PlayerState.Shooting;
        player.PlayerAnimator.SetBool("isShooting", true);
        nextShootTime = Time.time + CurrentItem.Cooldown;

        // Animatorの遷移競合でAnimationEvent(ShootProjectile/EndShoot)が
        // 発火しなかった場合の保険。一定時間後も発射中のままなら強制終了する
        if (shootTimeoutCoroutine != null)
        {
            StopCoroutine(shootTimeoutCoroutine);
        }
        shootTimeoutCoroutine = StartCoroutine(ShootTimeoutCoroutine(CurrentItem.UseTimeoutDuration));
    }

    // AnimationEventが発火しなかった場合のフォールバック処理
    // タイムアウト秒数はItemDataごとに設定（アイテムによってアニメーション再生時間が異なるため）
    private IEnumerator ShootTimeoutCoroutine(float timeoutDuration)
    {
        yield return new WaitForSeconds(timeoutDuration);

        // タイムアウトしてもまだ発射中状態が続いていたら強制終了する
        if (player.CurrentState == PlayerState.Shooting)
        {
            Debug.LogWarning($"ShootTimeout発火 frame:{Time.frameCount} AnimationEventが来なかったため強制EndShoot");
            EndShoot();
        }
    }

    /// <summary>
    /// 射撃アニメ終了処理
    /// AnimationEvent から呼ぶ
    /// </summary>
    public void EndShoot()
    {
        Debug.Log($"EndShoot呼び出し frame:{Time.frameCount}");
        player.CurrentState = PlayerState.Idle;
        hasShot = false; // 念のためリセット

        // Animatorを通常状態へ戻す
        player.PlayerAnimator.SetBool("isShooting", false);

        // タイムアウト監視を停止する
        if (shootTimeoutCoroutine != null)
        {
            StopCoroutine(shootTimeoutCoroutine);
            shootTimeoutCoroutine = null;
        }

        // 空中発射で止めていた重力を復帰する
        if (isShootGravityFrozen)
        {
            player.Rb.gravityScale = shootOriginalGravity;
            isShootGravityFrozen = false;
        }
    }
}
