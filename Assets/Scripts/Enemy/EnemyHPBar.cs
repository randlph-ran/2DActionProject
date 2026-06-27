using UnityEngine;
using UnityEngine.UI;

public class EnemyHPBar : MonoBehaviour
{
    // HPゲージ本体(Image)
    [Tooltip("HPゲージ本体(Image)")]
    [SerializeField] private Image hpFillImage;

    // EnemyHealth参照
    private EnemyHealth enemyHealth;

    // Startはゲーム開始時に1回呼ばれる
    private void Start()
    {
        // 親オブジェクトからEnemyHealth取得
        enemyHealth = GetComponentInParent<EnemyHealth>();

        // 最初にHPバー更新
        UpdateHPBar();
    }

    // HPバー更新処理
    public void UpdateHPBar()
    {
        // 現在HP取得
        float currentHP = enemyHealth.CurrentHP;

        // 最大HP取得
        float maxHP = enemyHealth.MaxHP;

        // HP割合計算
        float hpPercent = currentHP / maxHP;

        // ログ確認
        Debug.Log("HP割合 : " + hpPercent);

        // fillAmount反映
        hpFillImage.fillAmount = hpPercent;
    }
}