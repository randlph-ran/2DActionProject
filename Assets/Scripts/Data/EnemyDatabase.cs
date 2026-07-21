using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// StreamingAssets/enemy.csv から読み込んだEnemyデータの入れ物。
/// ItemDatabaseと同じ仕組み（初回アクセス時に自動ロード、id→Statsの辞書、pull型）。
/// </summary>
public static class EnemyDatabase
{
    // 読み込むCSVファイル名（StreamingAssets直下）
    private const string CsvFileName = "enemy.csv";

    // id → EnemyStats。null = 未ロード
    private static Dictionary<string, EnemyStats> statsById;

    /// <summary>
    /// 指定IDのデータを取得する。存在しない場合は null（呼び出し側はInspector値へフォールバックする）。
    /// </summary>
    public static EnemyStats GetStats(string id)
    {
        EnsureLoaded();

        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        return statsById.TryGetValue(id, out EnemyStats stats) ? stats : null;
    }

    /// <summary>CSVを読み込み直す（データ編集の確認用）。</summary>
    public static void Reload()
    {
        statsById = null;
        EnsureLoaded();
    }

    //==============================
    // 内部処理
    //==============================

    private static void EnsureLoaded()
    {
        if (statsById != null)
        {
            return;
        }

        // 読み込み失敗でも空辞書として成立させる（＝全項目がInspector値へフォールバック）
        statsById = new Dictionary<string, EnemyStats>();

        List<CsvRow> rows = CsvLoader.Load(CsvFileName);

        foreach (CsvRow row in rows)
        {
            string id = row.GetStringOrNull("id");

            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"[EnemyDatabase] {CsvFileName}: id が空の行があります。この行は無視します");
                continue;
            }

            if (statsById.ContainsKey(id))
            {
                Debug.LogWarning($"[EnemyDatabase] {CsvFileName}: id '{id}' が重複しています。最初の行を使用します");
                continue;
            }

            statsById[id] = CreateStats(row);
        }

        Debug.Log($"[EnemyDatabase] {CsvFileName} から {statsById.Count} 件のEnemyデータを読み込みました");
    }

    // 1行分をEnemyStatsへ変換する。空欄の列はnullのままになる
    private static EnemyStats CreateStats(CsvRow row)
    {
        return new EnemyStats
        {
            // EnemyAI
            enemyName = row.GetStringOrNull("enemyName"),
            moveSpeed = row.GetFloatOrNull("moveSpeed"),
            chaseDistance = row.GetFloatOrNull("chaseDistance"),
            chaseHeight = row.GetFloatOrNull("chaseHeight"),
            meleeAttackDistance = row.GetFloatOrNull("meleeAttackDistance"),
            canShoot = row.GetBoolOrNull("canShoot"),
            rangedAttackDistance = row.GetFloatOrNull("rangedAttackDistance"),
            rangedAttackCooldown = row.GetFloatOrNull("rangedAttackCooldown"),
            attackCooldown = row.GetFloatOrNull("attackCooldown"),
            attackRadius = row.GetFloatOrNull("attackRadius"),
            attackDamage = row.GetIntOrNull("attackDamage"),
            knockbackForce = row.GetFloatOrNull("knockbackForce"),
            checkDistance = row.GetFloatOrNull("checkDistance"),
            retreatDuration = row.GetFloatOrNull("retreatDuration"),
            retreatSpeed = row.GetFloatOrNull("retreatSpeed"),
            stunLevel = row.GetIntOrNull("stunLevel"),

            // EnemyHealth
            maxHP = row.GetIntOrNull("maxHP"),
            weightLevel = row.GetIntOrNull("weightLevel"),
            weight0Multiplier = row.GetFloatOrNull("weight0Multiplier"),
            weight1Multiplier = row.GetFloatOrNull("weight1Multiplier"),
            weight2Multiplier = row.GetFloatOrNull("weight2Multiplier"),
            landingCheckRadius = row.GetFloatOrNull("landingCheckRadius"),
            maxLaunchTime = row.GetFloatOrNull("maxLaunchTime"),
            effectScale = row.GetFloatOrNull("effectScale"),
            dropCount = row.GetIntOrNull("dropCount"),
            dropItemId = row.GetStringOrNull("dropItemId"),
            isBoss = row.GetBoolOrNull("isBoss"),
            nextSceneOnDeath = row.GetStringOrNull("nextSceneOnDeath"),
            deathAnimationDuration = row.GetFloatOrNull("deathAnimationDuration"),
        };
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        statsById = null;
    }
}
