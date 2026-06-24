using UnityEngine;

[CreateAssetMenu(
    fileName = "NewItemData",
    menuName = "Game/Item Data")]
public class ItemData : ScriptableObject
{
    //==============================
    // 基本情報
    //==============================

    [Header("基本情報")]

    [Tooltip("アイテム名")]
    [SerializeField]
    private string itemName;

    [Tooltip("アイテムアイコン")]
    [SerializeField]
    private Sprite itemIcon;

    [Tooltip("アイテム説明")]
    [SerializeField]
    [TextArea]
    private string description;

    //==============================
    // アイテム種別
    //==============================

    [Header("アイテム種別")]

    [Tooltip("アイテムの種類")]
    [SerializeField]
    private ItemType itemType;

    //==============================
    // 使用制御
    //==============================

    [Header("使用制御")]

    [Tooltip("-1で無限使用")]
    [SerializeField]
    private int maxUseCount = -1;

    [Tooltip("再使用までの待機時間")]
    [SerializeField]
    private float cooldown = 0.5f;

    [Tooltip("ボタン長押しで連射するか")]
    [SerializeField]
    private bool canAutoFire;

    [Tooltip("空中で使用可能か\nOFFの場合、空中では使用できず地上限定になる")]
    [SerializeField]
    private bool canUseInAir = true;

    [Tooltip("使用アニメーションのタイムアウト秒数\nAnimationEventが発火しなかった場合の保険。実際のクリップ再生時間より少し長めに設定する")]
    [SerializeField]
    private float useTimeoutDuration = 0.5f;

    //==============================
    // 攻撃設定
    //==============================

    [Header("攻撃設定")]

    [Tooltip("与えるダメージ")]
    [SerializeField]
    private int damage = 1;

    [Tooltip("ノックバック強さ")]
    [SerializeField]
    private float knockbackPower = 3f;

    [Tooltip("打ち上げ強さ")]
    [SerializeField]
    private float launchPower = 0f;

    //==============================
    // Projectile用
    //==============================

    [Header("Projectile設定")]

    [Tooltip("発射するProjectile")]
    [SerializeField]
    private GameObject projectilePrefab;

    //==============================
    // Recovery用
    //==============================

    [Header("Recovery設定")]

    [Tooltip("回復量")]
    [SerializeField]
    private int healAmount = 30;

    //==============================
    // Crash用
    //==============================

    [Header("Crash設定")]

    [Tooltip("発動前のチャージ時間")]
    [SerializeField]
    private float chargeTime = 1f;

    [Tooltip("Crash演出Prefab")]
    [SerializeField]
    private GameObject crashEffectPrefab;

    //==============================
    // 公開プロパティ
    //==============================

    public string ItemName => itemName;
    public Sprite ItemIcon => itemIcon;
    public ItemType ItemType => itemType;
    public string Description => description;

    /// <summary>
    /// 回復量
    /// </summary>
    public int HealAmount => healAmount;

    /// <summary>
    /// -1で無限使用
    /// </summary>
    public int MaxUseCount => maxUseCount;
    /// <summary>
    /// 再使用までの待機時間
    /// </summary>
    public float Cooldown => cooldown;
    /// <summary>
    /// ボタン長押しで連射するか
    /// </summary>
    public bool CanAutoFire => canAutoFire;
    /// <summary>
    /// 空中で使用可能か
    /// </summary>
    public bool CanUseInAir => canUseInAir;
    /// <summary>
    /// 使用アニメーションのタイムアウト秒数
    /// </summary>
    public float UseTimeoutDuration => useTimeoutDuration;

    /// <summary>
    /// 与えるダメージ
    /// </summary>
    public int Damage => damage;
    /// <summary>
    /// ノックバック強さ
    /// </summary>
    public float KnockbackPower => knockbackPower;
    /// <summary>
    /// 打ち上げ強さ
    /// </summary>
    public float LaunchPower => launchPower;

    /// <summary>
    /// 
    /// 発射するProjectile
    /// -------------------------------
    /// Projectileは、Itemを使用したときに生成されるオブジェクトで、攻撃の当たり判定や移動などを担当します。
    /// 例えば、銃の弾や魔法の弾などがProjectileに該当します。
    /// --------------------------------
    /// ProjectilePrefabは、ItemDataに設定されたProjectileのPrefabを指し、Itemを使用したときにこのPrefabが生成。
    /// これにより、Itemを使用するたびに同じProjectileが生成され、攻撃の挙動や見た目を統一することができる。
    /// 例えば、銃の弾のPrefabをProjectilePrefabに設定すれば、Itemを使用するたびにその銃の弾が生成され、攻撃の当たり判定や移動などが行われる。
    /// つまり、ProjectilePrefabは、Itemを使用したときに生成される攻撃のオブジェクトのテンプレートとなる。
    /// </summary>
    public GameObject ProjectilePrefab => projectilePrefab;

    /// <summary>
    /// 発動前のチャージ時間
    /// </summary>
    public float ChargeTime => chargeTime;
    /// <summary>
    /// Crash演出Prefab
    /// </summary>
    public GameObject CrashEffectPrefab => crashEffectPrefab;
}