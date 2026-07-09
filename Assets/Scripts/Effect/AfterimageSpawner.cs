using System.Collections;
using UnityEngine;

/// <summary>
/// 残像エフェクトを生成する汎用コンポーネント。
/// SpriteRendererを持つ任意のGameObject（Player・飛び道具・スペシャル技など）に付けて使う。
/// 自分が乗っているオブジェクトの現在の見た目を複製した残像を生成する。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class AfterimageSpawner : MonoBehaviour
{
    [Header("残像エフェクト設定")]

    [Tooltip("残像1枚の表示時間（フェードアウトにかかる時間）")]
    [SerializeField]
    private float duration = 0.5f;

    [Tooltip("残像生成時の初期アルファ値")]
    [SerializeField]
    private float startAlpha = 0.5f;

    [Tooltip("残像の色味（幽体っぽい青白さなどを調整する用。アルファはstartAlphaで管理）")]
    [SerializeField]
    private Color tintColor = new Color(0.6f, 0.8f, 1f);

    [Tooltip("連続生成（トレイル）時の残像の生成間隔")]
    [SerializeField]
    private float spawnInterval = 0.05f;

    // 複製元のSpriteRenderer（自分自身）
    private SpriteRenderer sourceRenderer;

    // トレイル（連続生成）が実行中か
    private bool isTrailActive;

    private void Awake()
    {
        // 複製元となる自身のSpriteRendererを取得
        sourceRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// 残像を1枚だけ生成する
    /// </summary>
    public void SpawnOnce()
    {
        // SpriteRendererが無い、または表示するSpriteが無ければ終了
        if (sourceRenderer == null || sourceRenderer.sprite == null)
        {
            return;
        }

        // 残像用GameObjectを生成
        GameObject afterimage = new GameObject("Afterimage");

        // 位置・回転・スケールを複製元と同じにする（向き反転も含めて）
        afterimage.transform.SetPositionAndRotation(transform.position, transform.rotation);
        afterimage.transform.localScale = transform.localScale;

        // 残像用SpriteRendererを設定（複製元の見た目をコピー）
        SpriteRenderer afterimageRenderer = afterimage.AddComponent<SpriteRenderer>();
        // SpriteをコピーPlayerの見た目を複製
        afterimageRenderer.sprite = sourceRenderer.sprite;
        // 重要：Materialは共有することで、残像の色を変えても複製元の色に影響が出ないようにする
        afterimageRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
        // 色味と初期アルファ値を設定
        afterimageRenderer.color = new Color(tintColor.r, tintColor.g, tintColor.b, startAlpha);
        // SortingLayerとOrderを複製元と同じにする
        afterimageRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        afterimageRenderer.sortingOrder = sourceRenderer.sortingOrder;
        // Flip状態（向き）を複製元と同じにする
        afterimageRenderer.flipX = sourceRenderer.flipX;
        afterimageRenderer.flipY = sourceRenderer.flipY;

        // 残像自身にフェードアウト処理を持たせる（生成元と独立して消える）
        AfterimageFade fade = afterimage.AddComponent<AfterimageFade>();
        fade.Play(duration);
    }

    /// <summary>
    /// 一定間隔で残像を連続生成し続ける（トレイル）。
    /// 既に実行中の場合は多重起動しない。
    /// </summary>
    public void StartTrail()
    {
        // 既に実行中なら多重起動しない
        if (isTrailActive)
        {
            return;
        }
        StartCoroutine(TrailRoutine());
    }

    /// <summary>
    /// トレイルの連続生成を停止する
    /// </summary>
    public void StopTrail()
    {
        isTrailActive = false;
    }

    // StopTrailが呼ばれるまで一定間隔で残像を生成し続ける
    private IEnumerator TrailRoutine()
    {
        // トレイル開始フラグON
        isTrailActive = true;
        // 停止されるまでループ
        while (isTrailActive)
        {
            // 残像を1枚生成
            SpawnOnce();
            // 指定間隔待機してから次の残像生成へ
            yield return new WaitForSeconds(spawnInterval);
        }
    }
}
