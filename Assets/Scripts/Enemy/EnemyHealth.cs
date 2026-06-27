using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
/// <summary>
/// IDamageable対応
/// </summary>
public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("基本設定")]

    // 最大HP
    [Tooltip("最大HP")]
    [SerializeField]
    private int maxHP = 10;

    // ノックバック力
    [Tooltip("ノックバック力\n被弾時に左右へ吹き飛ばす力")]
    [SerializeField]
    private float knockbackForce = 8f;

    // 現在HP
    private int currentHP;

    // HPバー参照
    private EnemyHPBar enemyHPBar;

    // SpriteRenderer参照
    private SpriteRenderer spriteRenderer;

    // Rigidbody2D参照
    private Rigidbody2D rb;

    // 現在HP取得用
    public int CurrentHP => currentHP;

    // 最大HP取得用
    public int MaxHP => maxHP;

    // 元の色を保存
    private Color originalColor;

    // ノックバック中か
    public bool IsKnockback { get; private set; }

    // ノックバック時間管理用Coroutine
    private Coroutine knockbackCoroutine;

    // 打ち上げ中か
    public bool IsLaunched { get; private set; }

    // 死亡中か
    private bool isDead;

    // 重さレベル（0:軽い, 1:普通, 2:重い）
    [Tooltip("重さレベル\n0:軽い 1:普通 2:重い\n打ち上げ攻撃を受けたときの飛びやすさに影響")]
    [SerializeField]
    private int weightLevel = 0;

    // 重さレベルに応じたノックバック倍率
    [Header("Launch Multiplier")]

    // 軽い敵はノックバックが大きく、重い敵は小さくなるように設定
    [Tooltip("重さレベル0（軽い）のときの打ち上げ力倍率")]
    [SerializeField]
    private float weight0Multiplier = 1.0f;

    // 普通の敵はノックバックが通常通り
    [Tooltip("重さレベル1（普通）のときの打ち上げ力倍率")]
    [SerializeField]
    private float weight1Multiplier = 0.5f;
    // 重い敵はノックバックが小さくなるように設定
    [Tooltip("重さレベル2（重い）のときの打ち上げ力倍率")]
    [SerializeField]
    private float weight2Multiplier = 0.0f;

    [Header("着地判定")]

    // 着地判定用
    [Tooltip("着地判定用Transform\n足元など地面に近い位置に置く")]
    [SerializeField]
    private Transform landingCheck;

    // 着地判定半径
    [Tooltip("着地判定の半径")]
    [SerializeField]
    private float landingCheckRadius = 0.15f;

    // GroundLayer
    [Tooltip("着地判定で検知する地面用Layer")]
    [SerializeField]
    private LayerMask groundLayer;

    [Header("攻撃エフェクト")]
    [SerializeField] private GameObject attack1EffectPrefab;
    [SerializeField] private GameObject attack2EffectPrefab;
    [SerializeField] private GameObject attack3EffectPrefab;
    [SerializeField] private GameObject jumpAttackEffectPrefab;
    [SerializeField] private GameObject itemEffectPrefab;

    [Tooltip("エフェクトの表示位置補正\nEnemyの中心(transform.position)からどれだけズラして出すか")]
    [SerializeField]
    private Vector2 effectPositionOffset = Vector2.zero;

    [Tooltip("エフェクトの表示サイズ倍率\n1で元のPrefabのサイズそのまま。0.5なら半分、2なら2倍")]
    [SerializeField]
    private float effectScale = 1f;

    [Header("ドロップアイテム")]
    [Tooltip("撃破時にドロップするアイテム（未設定ならドロップしない）")]
    [SerializeField]
    private ItemData dropItem;

    [Tooltip("ドロップする個数")]
    [SerializeField]
    private int dropCount = 1;

    [Tooltip("ドロップ時に生成するPickup用Prefab（ItemPickupコンポーネント付き）")]
    [SerializeField]
    private GameObject itemPickupPrefab;

    [Header("SE")]
    [Tooltip("被ダメージ時の共通SE（攻撃種別を問わない）")]
    [SerializeField]
    private AudioClip damageSE;

    [Tooltip("ボスのDieモーション開始時のSE（isBossがONの場合のみ使用）")]
    [SerializeField]
    private AudioClip dieSE;

    [Header("ボス死亡演出")]
    [Tooltip("ボスかどうか\nONの場合、死亡時に点滅の代わりにDieアニメーションを再生し、再生後に指定Sceneへ遷移する")]
    [SerializeField]
    private bool isBoss = false;

    [Tooltip("Die再生後に移動するScene名（isBossがONの場合のみ使用）")]
    [SerializeField]
    private string nextSceneOnDeath;

    [Tooltip("Dieアニメーションの再生時間（秒）\nこの時間が経過してからSceneを遷移する")]
    [SerializeField]
    private float deathAnimationDuration = 2f;

    // Animator参照（ボス死亡演出用）
    private Animator animator;

    private void Awake()
    {
        // 初期HP設定
        currentHP = maxHP;

        // 子オブジェクトからHPバー取得
        enemyHPBar = GetComponentInChildren<EnemyHPBar>();

        // SpriteRenderer取得
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Rigidbody2D取得
        rb = GetComponent<Rigidbody2D>();

        // Animator取得（未設定なら通常Enemyとして扱う）
        animator = GetComponent<Animator>();

        // 元の色を保存
        originalColor = spriteRenderer.color;
    }

    // 着地判定
    private void Update()
    {
        CheckLanding();
    }


    // ダメージ受信

    public void TakeDamage(int damage, Transform attacker, float knockbackPower, float launchPower, AttackType attackType)
    {
        EnemyAI enemyAI = GetComponent<EnemyAI>();
        // 攻撃中なら攻撃をキャンセルしてノックバックさせる
        if (enemyAI != null)
        {
            enemyAI.CancelAttack();
        }

        /* ノックバック中なら処理しない
        if (IsKnockback)
        {
            Debug.Log("Knockback中なので無視");
            return;
        }*/

        // 死亡中なら処理しない
        if (isDead) return;

        // 被ダメージSE再生（攻撃種別を問わず共通。Player側の攻撃が増えてもここで一括対応できる）
        SoundManager.Instance?.PlaySE(damageSE);

        // 攻撃種類に応じたエフェクトを発生させる
        SpawnAttackEffect(attackType);

        // 現在HP減少
        currentHP -= damage;

        // 攻撃方向判定 Player(与ダメ側)の位置とEnemy(被ダメ側)の正負を判定
        float direction = transform.position.x - attacker.position.x;

        // ノックバック開始
        IsKnockback = true;

        // 左右方向へノックバック Playerの位置との正負方向にノックバックさせる
        rb.AddForce(new Vector2(direction > 0 ? 1 : -1, 0) * knockbackPower, ForceMode2D.Impulse);
        Debug.Log($"[KNOCKBACK] Force追加直後 velocity:{rb.linearVelocity} IsLaunched:{IsLaunched}");

        // ノックバック時間管理Coroutine開始
        if (knockbackCoroutine != null)
        {
            StopCoroutine(knockbackCoroutine);
        }
        knockbackCoroutine = StartCoroutine(KnockbackCoroutine());

        // 被ダメ点滅開始
        StartCoroutine(FlashCoroutine());

        // HPバー更新
        if (enemyHPBar != null)
        {
            enemyHPBar.UpdateHPBar();
        }

        // 重さレベルに応じたノックバック倍率を取得
        float launchMultiplier = 1f;

        switch (weightLevel)
        {
            // 軽い敵はノックバックが大きく、重い敵は小さくなるように設定
            case 0:
                launchMultiplier = weight0Multiplier;
                break;
            // 普通の敵はノックバックが通常通り
            case 1:
                launchMultiplier = weight1Multiplier;
                break;
            // 重い敵はノックバックが小さくなるように設定
            case 2:
                launchMultiplier = weight2Multiplier;
                break;
        }
        // ノックバック力に重さレベルに応じた倍率を掛ける
        float finalLaunchPower = launchPower * launchMultiplier;

        // 打ち上げ攻撃なら打ち上げ状態にする
        if (finalLaunchPower > 0f)
        {
            // 打ち上げ状態に切替
            IsLaunched = true;
            // 打ち上げ力を加える
            rb.AddForce(Vector2.up * finalLaunchPower, ForceMode2D.Impulse);
        }
        Debug.Log(gameObject.name + " Weight=" + weightLevel + " Multiplier=" + launchMultiplier + " Launch=" + finalLaunchPower);
        // ダメージログ
        Debug.Log(gameObject.name + " に " + damage + " ダメージ");
        Debug.Log("Enemyの残HP : " + currentHP);
        // 現在HP確認
        if (currentHP <= 0)
        {
            Die();
        }
    }

    // 攻撃種類に応じたエフェクトをヒット位置に出す
    private void SpawnAttackEffect(AttackType attackType)
    {
        GameObject prefab = GetEffectPrefab(attackType);
        if (prefab == null) return;

        // Enemyの中心位置にオフセットを加えた位置に生成する
        Vector3 spawnPosition = transform.position + (Vector3)effectPositionOffset;

        GameObject effect = Instantiate(prefab, spawnPosition, Quaternion.identity);

        // Prefab本来のサイズに倍率を掛けて反映する
        effect.transform.localScale *= effectScale;
    }

    // 攻撃種類に応じたPrefabを返す
    private GameObject GetEffectPrefab(AttackType attackType)
    {
        switch (attackType)
        {
            case AttackType.Attack1: return attack1EffectPrefab;
            case AttackType.Attack2: return attack2EffectPrefab;
            case AttackType.Attack3: return attack3EffectPrefab;
            case AttackType.JumpAttack: return jumpAttackEffectPrefab;
            case AttackType.Item: return itemEffectPrefab;
            default: return null;
        }
    }

    // 死亡処理
    private void Die()
    {
        Debug.Log(gameObject.name + " を撃破したぞ");

        // 死亡状態
        isDead = true;

        // アイテムドロップ
        DropItem();

        // AI停止
        EnemyAI enemyAI = GetComponent<EnemyAI>();

        if (enemyAI != null)
        {
            enemyAI.enabled = false;
        }

        // ボスは地面コライダーを兼用しているため無効化しない（無効化すると地面を貫通して落下してしまう）
        if (!isBoss)
        {
            // Collider無効化
            Collider2D col = GetComponent<Collider2D>();

            if (col != null)
            {
                col.enabled = false;
            }
        }

        // 死亡演出開始
        StartCoroutine(DeathCoroutine());
    }

    // アイテムドロップ処理
    private void DropItem()
    {
        //（設定ミス用の確認）
        Debug.Log($"[DropItem] 呼び出し dropItem:{dropItem} / itemPickupPrefab:{itemPickupPrefab}");

        // ドロップアイテムかPrefabが未設定なら何もしない
        if (dropItem == null || itemPickupPrefab == null)
        {
            //（設定ミス用の確認）
            Debug.LogWarning("[DropItem] dropItemまたはitemPickupPrefabが未設定のためスキップしました");
            return;
        }

        // 自分の位置にPickup用オブジェクトを生成
        GameObject pickupObj = Instantiate(itemPickupPrefab, transform.position, Quaternion.identity);
        // 生成ログ（設定ミス用の確認）
        Debug.Log("[DropItem] " + pickupObj.name + " を生成 位置:" + transform.position);

        // ItemPickupへドロップ内容を設定
        ItemPickup pickup = pickupObj.GetComponent<ItemPickup>();

        if (pickup != null)
        {
            pickup.Setup(dropItem, dropCount);
        }
        else
        {
            // 設定忘れ警告ログ
            Debug.LogWarning("[DropItem] " + itemPickupPrefab.name + " にItemPickupコンポーネントが見つかりません");
        }
    }

    // 被ダメ点滅処理
    private IEnumerator FlashCoroutine()
    {
        for (int i = 0; i < 3; i++)
        {
            // 赤色へ変更
            spriteRenderer.color = Color.red;

            // 少し待機
            yield return new WaitForSeconds(0.08f);

            // 白色へ戻す
            spriteRenderer.color = originalColor;

            // 少し待機
            yield return new WaitForSeconds(0.08f);

        }
        // 念のため最後に確実に戻す
        spriteRenderer.color = originalColor;
    }

    // ノックバック時間管理
    private IEnumerator KnockbackCoroutine()
    {
        // 少し待機
        yield return new WaitForSeconds(0.4f);

        // X速度が収束するまで待つ（最大1秒でタイムアウト）
        float timeout = 1f;
        float timer = 0f;
        while (Mathf.Abs(rb.linearVelocity.x) > 0.5f && timer < timeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        // ノックバック終了
        IsKnockback = false;
    }

    // 死亡演出
    private IEnumerator DeathCoroutine()
    {
        // ボスならDieアニメーションを再生して次Sceneへ
        if (isBoss)
        {
            yield return BossDeathCoroutine();
            yield break;
        }

        // 5回点滅
        for (int i = 0; i < 5; i++)
        {
            // 非表示
            spriteRenderer.enabled = false;

            // 少し待機
            yield return new WaitForSeconds(0.08f);

            // 表示
            spriteRenderer.enabled = true;

            // 少し待機
            yield return new WaitForSeconds(0.08f);
        }

        // 通常Enemyは削除
        Destroy(gameObject);
    }

    // Die SE再生（AnimationEventから呼ぶ。Dieクリップ内の任意のタイミングに配置する）
    public void PlayDieSE()
    {
        SoundManager.Instance?.PlaySE(dieSE);
    }

    // ボス死亡演出 Dieアニメーション再生後に次Sceneへ遷移する
    private IEnumerator BossDeathCoroutine()
    {
        // ノックバックの勢いを止める（地面コライダーは有効のままなので落下はしないが、横滑りは防ぐ）
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // isDieをtrueにしてAnimatorのDie状態へ遷移させる
        if (animator != null)
        {
            animator.SetBool("isDie", true);
        }

        // アニメーション再生時間分待機
        yield return new WaitForSeconds(deathAnimationDuration);

        // 次Sceneへ遷移
        SceneManager.LoadScene(nextSceneOnDeath);
    }

    // 着地判定
    private void CheckLanding()
    {
        // 打ち上げ中でないなら終了
        if (!IsLaunched)
        {
            return;
        }
        // 着地判定
        bool isGrounded =
            Physics2D.OverlapCircle(
                landingCheck.position,
                landingCheckRadius,
                groundLayer);

        // 着地したら解除
        if (isGrounded)
        {
            IsLaunched = false;
        }
    }



}