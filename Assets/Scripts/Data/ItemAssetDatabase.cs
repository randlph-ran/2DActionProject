using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ItemDataアセットを「id → ItemData」で引くための索引（名簿）。
///
/// Resources/Item/ 配下のItemDataを Resources.LoadAll で自動収集して辞書を作る。
/// アイテムを追加してもResources/Item/に置くだけで自動で拾われるため、手動登録は不要。
///
/// item.csv（数値データ）を読む ItemDatabase とは役割が別：
/// ・ItemDatabase       … idごとの「数値パラメータ」を持つ
/// ・ItemAssetDatabase  … idから「ItemDataアセット本体」を引く（ドロップ指定などで使う）
/// </summary>
public static class ItemAssetDatabase
{
    // Resources以下の収集対象フォルダ（Assets/Resources/Item/）
    private const string ResourceFolder = "Item";

    // id → ItemData。null = 未ロード
    private static Dictionary<string, ItemData> itemsById;

    /// <summary>
    /// 指定IDのItemDataを取得する。見つからなければ null。
    /// </summary>
    public static ItemData GetItem(string id)
    {
        EnsureLoaded();

        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        return itemsById.TryGetValue(id, out ItemData item) ? item : null;
    }

    //==============================
    // 内部処理
    //==============================

    private static void EnsureLoaded()
    {
        if (itemsById != null)
        {
            return;
        }

        itemsById = new Dictionary<string, ItemData>();

        // Resources/Item/ 配下の全ItemDataを読み込む
        ItemData[] all = Resources.LoadAll<ItemData>(ResourceFolder);

        foreach (ItemData item in all)
        {
            string id = item.Id;

            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            if (itemsById.ContainsKey(id))
            {
                Debug.LogWarning($"[ItemAssetDatabase] id '{id}' のItemDataが複数あります。最初の1つを使用します");
                continue;
            }

            itemsById[id] = item;
        }

        Debug.Log($"[ItemAssetDatabase] Resources/{ResourceFolder}/ から {itemsById.Count} 件のItemDataを収集しました");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        itemsById = null;
    }
}
