using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeController : MonoBehaviour
{
    // フェード対象のImage
    [SerializeField]
    private Image fadeImage;

    // フェード時間（秒）
    [SerializeField]
    private float fadeDuration = 2f;

    // スタートテキストのCanvasGroup
    [SerializeField]
    private CanvasGroup startTextCanvasGroup;

    // スタートテキストの表示時間（秒）
    [SerializeField]
    private float startTextDuration = 1.5f;

    private void Start()
    {
        // シーン開始時にフェードイン開始
        StartCoroutine(FadeIn());
        // フェードインと同時にスタートテキストを表示
        StartCoroutine(ShowStartText());
    }

    private IEnumerator FadeIn()
    {
        // 現在の色を取得
        Color color = fadeImage.color;

        // タイマー初期化
        float timer = 0f;
        // フェード時間までループ
        while (timer < fadeDuration)
        {
            // 経過時間を加算
            timer += Time.deltaTime;

            // α値を1→0へ補間
            color.a = Mathf.Lerp(1f, 0f, timer / fadeDuration);

            // Imageへ反映
            fadeImage.color = color;
            // 次のフレームまで待機
            yield return null;
        }

        // 誤差対策で完全透明にする
        color.a = 0f;
        fadeImage.color = color;
    }

    // スタートテキストを表示するコルーチン    
    private IEnumerator ShowStartText()
    {
        // スタートテキストを表示
        startTextCanvasGroup.alpha = 1f;

        // 指定時間待機
        yield return new WaitForSeconds(startTextDuration);

        // スタートテキストを非表示
        startTextCanvasGroup.alpha = 0f;

        // ★ここが重要：ゲーム開始タイミング
        GameManager.StartGame();
    }

}