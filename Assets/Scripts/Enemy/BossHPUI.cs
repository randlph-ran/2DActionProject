using UnityEngine;
using UnityEngine.UI;

public class BossHPUI : MonoBehaviour
{
    // BossのHP管理
    [Tooltip("BossのHP管理(EnemyHealth参照)")]
    [SerializeField]
    private EnemyHealth bossHealth;

    // HPバー
    [Tooltip("HPバーのSlider")]
    [SerializeField]
    private Slider hpSlider;

    // 初期化
    private void Start()
    {
        // 最大HP設定
        hpSlider.maxValue =
            bossHealth.MaxHP;

        // 初期HP反映
        hpSlider.value =
            bossHealth.CurrentHP;
    }

    // 毎フレーム更新
    private void Update()
    {
        // 現在HP反映
        hpSlider.value = bossHealth.CurrentHP;

        // デバッグ用にHP表示
        Debug.Log("Boss HP : " + bossHealth.CurrentHP);

    }
}