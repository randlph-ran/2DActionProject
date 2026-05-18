using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Rigidbody2Dを保存する変数
    private Rigidbody2D rb;

    // 移動入力値
    private float moveInput;

    // 移動速度
    [SerializeField]
    private float moveSpeed = 5f;

    // プレイヤーが右向きかどうか
    private bool isFacingRight = true;

    // ゲーム開始時に最初に呼ばれる
    private void Awake()
    {
        // Rigidbody2D取得
        rb = GetComponent<Rigidbody2D>();
    }

    // 毎フレーム実行
    // 入力取得を行う
    private void Update()
    {
        // 左右入力取得
        moveInput = Input.GetAxisRaw("Horizontal");

        // 向き変更処理
        Flip();
    }

    // 物理演算用
    private void FixedUpdate()
    {
        // 現在のY速度を保持しながらX方向へ移動
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    // キャラクターの向きを変更する
    private void Flip()
    {
        // 左入力時、右向きなら反転
        if (moveInput < 0 && isFacingRight)
        {
            Turn();
        }

        // 右入力時、左向きなら反転
        else if (moveInput > 0 && !isFacingRight)
        {
            Turn();
        }
    }

    // 実際にSpriteを反転する
    private void Turn()
    {
        // 向き状態反転
        isFacingRight = !isFacingRight;

        // 現在Scale取得
        Vector3 scale = transform.localScale;

        // X方向反転
        scale.x *= -1;

        // Scale適用
        transform.localScale = scale;
    }
}
