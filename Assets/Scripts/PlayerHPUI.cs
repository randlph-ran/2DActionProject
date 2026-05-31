using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// プレイヤーのHPをUIで表示するクラス
/// </summary>
public class PlayerHPUI : MonoBehaviour
{
    // HPバーのSlider
    [SerializeField]
    private Slider hpSlider;

    /// <summary>
    /// HPバー更新
    /// </summary>
    public void SetHP(int currentHP, int maxHP)
    {
        // 0〜1の割合に変換してバーに反映
        hpSlider.value = (float)currentHP / maxHP;
    }
    private void Start()
    {
        Debug.Log(hpSlider);
    }
}