using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    // 最大HP
    [SerializeField]
    private int maxHP = 5;

    // 現在HP
    private int currentHP;

    // ゲーム開始時に呼ばれる
    private void Start()
    {
        // HP初期化
        currentHP = maxHP;

        Debug.Log("Player HPを最大HPで初期化しました: " + currentHP);
    }

    // ダメージを受ける処理
    public void TakeDamage(int damage)
    {
        // HPをダメージで減少させる
        currentHP -= damage;

        Debug.Log("Playerは" + damage + " のダメージを受けた");
        Debug.Log("現在HP: " + currentHP);

        // HP0以下なら死亡
        if (currentHP <= 0)
        {
            Die();
        }
    }

    // Die:
    // HP0時処理
    private void Die()
    {
        Debug.Log("Playerは死んでしまった");

        // 仮で非表示
        gameObject.SetActive(false);
    }
}