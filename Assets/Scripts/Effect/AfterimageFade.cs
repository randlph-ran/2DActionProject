using System.Collections;
using UnityEngine;

/// <summary>
/// 残像1枚を一定時間かけてフェードアウトさせ、完了後に自身を破棄する。
/// 生成元(AfterimageSpawner)とは独立して動くため、
/// 生成元が途中で破棄されても（飛び道具の消滅など）残像は正しく消える。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class AfterimageFade : MonoBehaviour
{
    /// <summary>
    /// フェードアウトを開始する
    /// </summary>
    /// <param name="duration">消えるまでにかかる時間</param>
    public void Play(float duration)
    {
        StartCoroutine(FadeRoutine(duration));
    }

    // 一定時間かけてアルファを0まで下げ、消えたら自身を破棄する
    private IEnumerator FadeRoutine(float duration)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        // フェードアウト時間の経過を追跡する変数
        float elapsed = 0f;
        // 残像の初期色を取得
        Color startColor = sr.color;

        // フェードアウト処理
        while (elapsed < duration)
        {
            // 経過時間を加算
            elapsed += Time.deltaTime;
            // アルファ値を線形補間で減少させる
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / duration);
            // 残像の色を更新
            sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        // フェードアウト完了後、残像GameObjectを破棄
        Destroy(gameObject);
    }
}
