using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    // 移動速度設定
    [SerializeField]
    private float moveSpeed = 2f;

    // 右向きかどうか
    private bool isFacingRight = true;

    // Rigidbody2D
    private Rigidbody2D rb;

    // Player位置
    private Transform playerTransform;

    // 追尾開始距離　指定距離以内なら追尾
    [SerializeField]
    private float chaseDistance = 5f;

    // 初期化
    private void Awake()
    {
        // Rigidbody2D取得
        rb = GetComponent<Rigidbody2D>();

        //Scene内のTagがPlayerのものを探して入れる
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }

    // 物理更新
    private void FixedUpdate()
    {
        // Playerとの距離 Enemyの位置とPlayerの位置を測って入れる
        float distance = Vector2.Distance(transform.position, playerTransform.position);

        // 移動方向
        float moveDirection;

        // Playerが近いなら追尾
        if (distance <= chaseDistance)
        {
            // Playerが右にいるか
            moveDirection = playerTransform.position.x > transform.position.x ? 1f : -1f;

            // Playerに合わせて向きを更新
            isFacingRight = moveDirection > 0;
        }
        else
        {
            // 通常巡回　向きに合わせて巡回
            moveDirection = isFacingRight ? 1f : -1f;
        }

        // 移動
        rb.linearVelocity = new Vector2(moveDirection * moveSpeed, rb.linearVelocity.y);
    }

    // OnCollisionEnter2D:
    // 衝突時処理
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Groundと衝突したら反転
        if (collision.gameObject.CompareTag("Wall"))
        {
            Flip();
        }
    }

    // Flip:
    // 向き変更
    private void Flip()
    {
        // 向き反転
        isFacingRight = !isFacingRight;

        // Scale取得
        Vector3 scale = transform.localScale;

        // X反転
        scale.x *= -1;

        // Scale反映
        transform.localScale = scale;
    }
}