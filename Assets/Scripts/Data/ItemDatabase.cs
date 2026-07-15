using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// StreamingAssets/item.csv から読み込んだItemデータの入れ物。
///
/// ・初回アクセス時に自動で読み込むため、シーンへの配置やコンポーネント追加は不要。
/// ・CSVが無い/壊れていても空のまま動き、ItemData側がInspector値へフォールバックする。
/// ・ItemData が自分のIDでここを引く（pull型）ため、ItemDataアセットを列挙する必要がない。
/// </summary>
public static class ItemDatabase
{
    // 読み込むCSVファイル名（StreamingAssets直下）
    private const string CsvFileName = "item.csv";

    // id → ItemStats。null = 未ロード
    private static Dictionary<string, ItemStats> statsById;

    /// <summary>
    /// 指定IDのデータを取得する。存在しない場合は null（呼び出し側はInspector値へフォールバックする）。
    /// </summary>
    public static ItemStats GetStats(string id)
    {
        EnsureLoaded();

        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        return statsById.TryGetValue(id, out ItemStats stats) ? stats : null;
    }

    /// <summary>
    /// CSVを読み込み直す（データを編集して確認したいときに使う）。
    /// </summary>
    public static void Reload()
    {
        statsById = null;
        EnsureLoaded();
    }

    //==============================
    // 内部処理
    //==============================

    // まだ読み込んでいなければCSVを読み込む
    private static void EnsureLoaded()
    {
        if (statsById != null)
        {
            return;
        }

        // 読み込みに失敗しても空の辞書として成立させる（＝全項目がInspector値へフォールバックする）
        statsById = new Dictionary<string, ItemStats>();

        List<CsvRow> rows = CsvLoader.Load(CsvFileName);

        foreach (CsvRow row in rows)
        {
            string id = row.GetStringOrNull("id");

            // idが無い行は紐づけられないので飛ばす
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"[ItemDatabase] {CsvFileName}: id が空の行があります。この行は無視します");
                continue;
            }

            // id重複は先勝ちにして警告する
            if (statsById.ContainsKey(id))
            {
                Debug.LogWarning($"[ItemDatabase] {CsvFileName}: id '{id}' が重複しています。最初の行を使用します");
                continue;
            }

            statsById[id] = CreateStats(row);
        }

        Debug.Log($"[ItemDatabase] {CsvFileName} から {statsById.Count} 件のItemデータを読み込みました");
    }

    // 1行分をItemStatsへ変換する。空欄の列は null のままになり、ItemData側でInspector値が使われる
    private static ItemStats CreateStats(CsvRow row)
    {
        return new ItemStats
        {
            itemName = row.GetStringOrNull("itemName"),
            description = row.GetStringOrNull("description"),

            maxUseCount = row.GetIntOrNull("maxUseCount"),
            cooldown = row.GetFloatOrNull("cooldown"),
            canAutoFire = row.GetBoolOrNull("canAutoFire"),
            canUseInAir = row.GetBoolOrNull("canUseInAir"),
            useTimeoutDuration = row.GetFloatOrNull("useTimeoutDuration"),

            value = row.GetIntOrNull("value"),
            knockbackPower = row.GetFloatOrNull("knockbackPower"),
            launchPower = row.GetFloatOrNull("launchPower"),

            chargeTime = row.GetFloatOrNull("chargeTime"),
        };
    }

    // Play開始時に読み込み状態をリセットする。
    // これがないと、エディタで一度読み込んだ内容が次のPlayまで残り、CSVを編集しても反映されない場合がある
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        statsById = null;
    }
}
