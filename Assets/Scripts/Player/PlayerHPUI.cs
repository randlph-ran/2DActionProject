using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// プレイヤーのHPをUIで表示するクラス
/// </summary>
public class PlayerHPUI : MonoBehaviour
{
    // HPバーのSlider
    [Tooltip("HPバーのSlider")]
    [SerializeField]
    private Slider hpSlider;

    /// <summary>
    /// HPバー更新
    /// </summary>
    public void SetHP(int currentHP, int maxHP)
    {
        // HPバーの最大値と現在値を設定
        hpSlider.maxValue = maxHP;
        hpSlider.value = currentHP;
    }
    private void Start()
    {
        Debug.Log(hpSlider);
    }
}