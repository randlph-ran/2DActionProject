using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// シーンをまたいで引き継ぐゲームデータを管理する。
/// Scene1 に配置し、DontDestroyOnLoad で持続させる。
/// </summary>
public class GameData : MonoBehaviour
{
    public static GameData Instance { get; private set; }

    //==============================
    // 内部状態
    //==============================

    // データが保存済みかどうか（初回 Scene では false）
    private bool isInitialized;

    /// <summary>保存済みデータが存在するか</summary>
    public bool HasData => isInitialized;

    //==============================
    // HP
    //==============================

    public int CurrentHP { get; private set; }
    public int MaxHP     { get; private set; }

    //==============================
    // インベントリ
    //==============================

    private List<InventoryItem> savedInventory = new();
    public ItemData EquippedItem { get; private set; }

    //==============================
    // Unity イベント
    //==============================

    private void Awake()
    {
        // 既に存在する場合は自身を破棄
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    //==============================
    // 保存
    //==============================

    /// <summary>HP を保存する</summary>
    public void SaveHP(int current, int max)
    {
        CurrentHP      = current;
        MaxHP          = max;
        isInitialized  = true;
    }

    /// <summary>インベントリを保存する</summary>
    public void SaveInventory(List<InventoryItem> items, ItemData equipped)
    {
        // 参照共有を避けるためディープコピー
        savedInventory = items.ConvertAll(
            i => new InventoryItem { itemData = i.itemData, count = i.count }
        );
        EquippedItem  = equipped;
        isInitialized = true;
    }

    //==============================
    // 読み込み
    //==============================

    /// <summary>保存済みインベントリのコピーを返す</summary>
    public List<InventoryItem> LoadInventory()
    {
        return savedInventory.ConvertAll(
            i => new InventoryItem { itemData = i.itemData, count = i.count }
        );
    }

    //==============================
    // リセット
    //==============================

    /// <summary>
    /// 保存済みのHP・インベントリを初期状態に戻す。
    /// Titleへ戻った時（クリア後・ゲームオーバー後など）に呼び、
    /// 次回のNewGame開始時に前回までのデータを引き継がないようにする。
    /// </summary>
    public void ResetData()
    {
        CurrentHP     = 0;
        MaxHP         = 0;
        savedInventory.Clear();
        EquippedItem  = null;
        isInitialized = false;
    }
}
