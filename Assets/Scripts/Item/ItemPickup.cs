using UnityEngine;

/// <summary>
/// フィールド上に出現するアイテム取得オブジェクト
/// Enemyドロップ時に生成し、Playerが触れると所持アイテムへ追加する
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ItemPickup : MonoBehaviour
{
    // 取得対象のアイテムデータ
    private ItemData itemData;

    // 取得個数
    private int count;

    [Tooltip("アイコン表示用（未設定なら見た目反映はスキップ）")]
    [SerializeField]
    private SpriteRenderer iconRenderer;

    /// <summary>
    /// ドロップ元から呼び出し、内容を設定する
    /// </summary>
    public void Setup(ItemData data, int amount)
    {
        // アイテムデータと個数を保持
        itemData = data;
        count = amount;

        // アイコン表示があれば見た目を反映
        if (iconRenderer != null && itemData != null && itemData.ItemIcon != null)
        {
            iconRenderer.sprite = itemData.ItemIcon;
        }
    }

    // Player接触判定
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Player以外は無視
        if (!other.CompareTag("Player"))
        {
            return;
        }

        // 接触したオブジェクトからInventoryManagerを取得
        InventoryManager inventory = other.GetComponent<InventoryManager>();

        // 同オブジェクトにない場合は親オブジェクトも探す
        if (inventory == null)
        {
            inventory = other.GetComponentInParent<InventoryManager>();
        }

        // InventoryManagerが見つからない、またはアイテム未設定なら何もしない
        if (inventory == null || itemData == null)
        {
            return;
        }

        // 取得SE再生
        SoundManager.Instance?.PlaySE(itemData.PickupSE);

        // 所持アイテムへ追加
        inventory.AddItem(itemData, count);

        // 取得済みなので削除
        Destroy(gameObject);
    }
}
