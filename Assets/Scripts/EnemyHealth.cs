using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    // 最大HP
    [SerializeField]
    private int maxHP = 10;

    // 現在HP
    private int currentHP;

    // HPバー参照
    private EnemyHPBar enemyHPBar;

    // SpriteRenderer参照
    private SpriteRenderer spriteRenderer;

    // 現在HP取得用
    public int CurrentHP => currentHP;

    // 最大HP取得用
    public int MaxHP => maxHP;


    private void Awake()
    {
        // 初期HP設定
        currentHP = maxHP;

        // 子オブジェクトからHPバー取得
        enemyHPBar = GetComponentInChildren<EnemyHPBar>();

        // SpriteRenderer取得
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // ダメージ受信

    public void TakeDamage(int damage)
    {
        // 現在HP減少
        currentHP -= damage;

        // 被ダメ点滅開始
        StartCoroutine(FlashCoroutine());

        // HPバー更新
        if (enemyHPBar != null)
        {
            enemyHPBar.UpdateHPBar();
        }

        // ダメージログ
        Debug.Log(gameObject.name + " に " + damage + " ダメージ");
        Debug.Log("Enemyの残HP : " + currentHP);
        // 現在HP確認
        if (currentHP <= 0)
        {
            Die();
        }
    }

    // 死亡処理
    private void Die()
    {
        Debug.Log(gameObject.name + " を撃破したぞ");

        // GameObject削除
        Destroy(gameObject);
    }

    // 被ダメ点滅処理
    private IEnumerator FlashCoroutine()
    {
        for (int i = 0; i < 3; i++)
        {
            // 赤色へ変更
            spriteRenderer.color = Color.white;

            // 少し待機
            yield return new WaitForSeconds(0.08f);

            // 白色へ戻す
            spriteRenderer.color = Color.red;

            // 少し待機
            yield return new WaitForSeconds(0.08f);
        }
    }
}