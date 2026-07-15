using UnityEngine;

[CreateAssetMenu(
    fileName = "NewItemData",
    menuName = "Game/Item Data")]
public class ItemData : ScriptableObject
{
    //==============================
    // データ参照
    //==============================

    [Header("データ参照")]

    [Tooltip("StreamingAssets/item.csv と紐づけるID\n空欄の場合はアセット名がIDとして使われる")]
    [SerializeField]
    private string id;

    /// <summary>
    /// CSVと紐づけるID（空欄ならアセット名を使う）
    /// </summary>
    public string Id => string.IsNullOrEmpty(id) ? name : id;

    /// <summary>
    /// CSVから読み込んだデータ。
    /// CSVが無い/該当IDの行が無い場合は null になり、各プロパティはInspector値へフォールバックする。
    /// </summary>
    private ItemStats Stats => ItemDatabase.GetStats(Id);

    //==============================
    // 基本情報
    //==============================

    [Header("基本情報")]

    [Tooltip("アイテム名\n※item.csvに値があればそちらが優先される")]
    [SerializeField]
    private string itemName;

    [Tooltip("アイテムアイコン")]
    [SerializeField]
    private Sprite itemIcon;

    [Tooltip("アイテム説明")]
    [SerializeField]
    [TextArea]
    private string description;

    [Tooltip("取得時に再生するSE")]
    [SerializeField]
    private AudioClip pickupSE;

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
    // 効果量
    //==============================

    [Header("効果量")]

    [Tooltip("アイテムが及ぼす効果の大きさ\nProjectile:与えるダメージ / Recovery:回復量")]
    [SerializeField]
    private int value = 1;

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
    //
    // 「Stats?.xxx ?? インスペクタ値」の形になっている項目はCSV対応済み。
    // CSVに値があればCSV値、無ければ（CSV未読込・行が無い・列が空欄）Inspector値が使われる。
    // 画像/SE/Prefab などのアセット参照と itemType はCSVでは扱わず、常にInspector値を使う。
    //==============================

    public string ItemName => Stats?.itemName ?? itemName;
    public Sprite ItemIcon => itemIcon;
    public ItemType ItemType => itemType;
    public string Description => Stats?.description ?? description;
    /// <summary>
    /// 取得時に再生するSE
    /// </summary>
    public AudioClip PickupSE => pickupSE;

    /// <summary>
    /// -1で無限使用
    /// </summary>
    public int MaxUseCount => Stats?.maxUseCount ?? maxUseCount;
    /// <summary>
    /// 再使用までの待機時間
    /// </summary>
    public float Cooldown => Stats?.cooldown ?? cooldown;
    /// <summary>
    /// ボタン長押しで連射するか
    /// </summary>
    public bool CanAutoFire => Stats?.canAutoFire ?? canAutoFire;
    /// <summary>
    /// 空中で使用可能か
    /// </summary>
    public bool CanUseInAir => Stats?.canUseInAir ?? canUseInAir;
    /// <summary>
    /// 使用アニメーションのタイムアウト秒数
    /// </summary>
    public float UseTimeoutDuration => Stats?.useTimeoutDuration ?? useTimeoutDuration;

    /// <summary>
    /// アイテムが及ぼす効果の大きさ（Projectile:ダメージ / Recovery:回復量）
    /// </summary>
    public int Value => Stats?.value ?? value;
    /// <summary>
    /// ノックバック強さ
    /// </summary>
    public float KnockbackPower => Stats?.knockbackPower ?? knockbackPower;
    /// <summary>
    /// 打ち上げ強さ
    /// </summary>
    public float LaunchPower => Stats?.launchPower ?? launchPower;

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
    public float ChargeTime => Stats?.chargeTime ?? chargeTime;
    /// <summary>
    /// Crash演出Prefab
    /// </summary>
    public GameObject CrashEffectPrefab => crashEffectPrefab;
}