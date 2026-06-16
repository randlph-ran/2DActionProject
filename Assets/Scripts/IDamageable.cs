using UnityEngine;

/// <summary>
/// ダメージを受けるオブジェクト共通インターフェース
/// Projectileはこれだけを見て処理する
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// ダメージ処理
    /// </summary>
    /// <param name="damage">与ダメージ</param>
    /// <param name="attacker">発射者</param>
    /// <param name="knockbackPower">ノックバック力</param>
    /// <param name="launchPower">打ち上げ力</param>
    /// <param name="attackType">攻撃種類（エフェクト切り替え用）</param>
    void TakeDamage(int damage, Transform attacker, float knockbackPower, float launchPower, AttackType attackType);
}