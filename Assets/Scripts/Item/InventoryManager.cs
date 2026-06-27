using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイヤーの所持アイテム管理
/// </summary>
public class InventoryManager : MonoBehaviour
{
    [Header("所持アイテム一覧")]
    [SerializeField]
    private List<InventoryItem> inventory = new();

    [Header("現在装備中のアイテム")]
    [SerializeField]
    private ItemData equippedItem;

    /// <summary>
    /// 現在装備中のアイテム
    /// </summary>
    public ItemData EquippedItem => equippedItem;

    /// <summary>
    /// 所持アイテム一覧
    /// </summary>
    public List<InventoryItem> Inventory => inventory;

    /// <summary>
    /// シーン開始時に GameData からインベントリを復元する（2シーン目以降）
    /// </summary>
    private void Awake()
    {
        if (GameData.Instance != null && GameData.Instance.HasData)
        {
            inventory = GameData.Instance.LoadInventory();
            equippedItem = GameData.Instance.EquippedItem;
        }
    }


    /// <summary>
    /// アイテム取得
    /// </summary>
    public void AddItem(ItemData itemData, int amount = 1)
    {
        // 既に所持しているか確認
        InventoryItem item = inventory.Find(i => i.itemData == itemData);
        // 既に所持している場合は個数を加算
        if (item != null)
        {
            // 所持数を加算
            item.count += amount;
        }
        else
        {
            // 所持していない場合は新規追加
            inventory.Add(new InventoryItem
            {
                // アイテムデータと個数を設定
                itemData = itemData,
                count = amount
            });
        }
    }

    /// <summary>
    /// アイテム削除
    /// </summary>
    public void RemoveItem(ItemData itemData, int amount = 1)
    {
        // 既に所持しているか確認
        InventoryItem item = inventory.Find(i => i.itemData == itemData);

        // 所持していない場合は何もしない
        if (item == null)
        {
            return;
        }
        // 所持数を減算
        item.count -= amount;
        // 所持数が0以下になった場合はリスト削除と装備解除
        if (item.count <= 0)
        {
            // 装備中のアイテムが削除対象の場合は装備解除
            if (equippedItem == itemData)
            {
                // 装備中のアイテムを解除
                UnequipItem();
            }
            // リストから削除
            inventory.Remove(item);
        }
    }

    /// <summary>
    /// 所持数取得
    /// </summary>
    public int GetItemCount(ItemData itemData)
    {
        // 既に所持しているか確認
        InventoryItem item = inventory.Find(i => i.itemData == itemData);
        // 所持していない場合は0を返す
        return item != null ? item.count : 0;
    }

    /// <summary>
    /// 所持しているか
    /// </summary>
    public bool HasItem(ItemData itemData)
    {
        // 所持数が0より大きい場合は所持していると判断
        return GetItemCount(itemData) > 0;
    }

    /// <summary>
    /// アイテム装備
    /// </summary>
    public bool EquipItem(ItemData itemData)
    {
        // 装備するアイテムを所持しているか確認
        if (!HasItem(itemData))
        {
            return false;
        }
        // 装備中のアイテムを更新
        equippedItem = itemData;
        // 装備成功
        return true;
    }
    /// <summary>
    /// 装備解除
    /// </summary>
    public void UnequipItem()
    {
        // 装備中のアイテムを解除
        equippedItem = null;
    }

    /// <summary>
    /// 装備中アイテムを使用する
    /// </summary>
    public ItemData UseEquippedItem()
    {
        // 装備中のアイテムがない場合はnullを返す
        if (equippedItem == null)
        {
            // 装備中のアイテムがない場合はnullを返す
            return null;
        }
        // 装備中のアイテムを所持していない場合はnullを返す
        if (!HasItem(equippedItem))
        {
            // 装備中のアイテムを解除
            return null;
        }
        // 装備中のアイテムを使用する
        ItemData usedItem = equippedItem;
        // 使用したアイテムを削除
        RemoveItem(equippedItem);
        // 使用したアイテムを返す
        return usedItem;
    }

    /// <summary>
    /// シーン遷移時にインベントリを GameData へ保存する
    /// </summary>
    private void OnDestroy()
    {
        if (GameData.Instance != null)
        {
            GameData.Instance.SaveInventory(inventory, equippedItem);
        }
    }

}