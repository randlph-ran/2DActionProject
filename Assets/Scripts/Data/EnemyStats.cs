using UnityEngine;

/// <summary>
/// CSVから読み込んだEnemy1体分のデータ（実行時のみメモリ上に存在する）。
///
/// 各項目はnull許容で、null = 「CSVで指定されていない」を意味する。
/// EnemyAI / EnemyHealth はAwakeで、値が入っている項目だけ自分のフィールドへ上書きする
/// （nullの項目はInspector値のまま＝フォールバック）。
///
/// ※Enemyはプレハブ上のMonoBehaviourのため、実行時にフィールドを書き換えても
///   アセットには永続化されない（Play終了で元に戻る）。よってItemDataのような
///   「非シリアライズの第2の箱」は不要で、直接フィールドを上書きしてよい。
/// </summary>
public class EnemyStats
{
    // --- EnemyAI ---
    public string enemyName;
    public float? moveSpeed;
    public float? chaseDistance;
    public float? chaseHeight;
    public float? meleeAttackDistance;
    public bool? canShoot;
    public float? rangedAttackDistance;
    public float? rangedAttackCooldown;
    public float? attackCooldown;
    public float? attackRadius;
    public int? attackDamage;
    public float? knockbackForce;
    public float? checkDistance;
    public float? retreatDuration;
    public float? retreatSpeed;
    public int? stunLevel;

    // --- EnemyHealth ---
    public int? maxHP;
    public int? weightLevel;
    public float? weight0Multiplier;
    public float? weight1Multiplier;
    public float? weight2Multiplier;
    public float? landingCheckRadius;
    public float? maxLaunchTime;
    public float? effectScale;
    public int? dropCount;
    // ドロップするItemDataのID（ItemAssetDatabaseで実際のItemDataへ解決する）
    public string dropItemId;
    public bool? isBoss;
    public string nextSceneOnDeath;
    public float? deathAnimationDuration;
}
