using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeController : MonoBehaviour
{
    [Tooltip("フェード対象のImage")]
    [SerializeField]
    private Image fadeImage;

    [Tooltip("フェード時間（秒）")]
    [SerializeField]
    private float fadeDuration = 2f;

    [Tooltip("スタートテキストのCanvasGroup")]
    [SerializeField]
    private CanvasGroup startTextCanvasGroup;

    [Tooltip("スタートテキストの表示時間（秒）")]
    [SerializeField]
    private float startTextDuration = 1.5f;

    private void Start()
    {
        // メニューの開閉はGameManager.IsGameStartedを見て自律的に判断するため、
        // ここでの無効化呼び出しは不要（ResetGameState()がfalseのまま開始される）

        StartCoroutine(FadeIn());
        StartCoroutine(ShowStartText());
    }

    private IEnumerator FadeIn()
    {
        Color color = fadeImage.color;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            // 1フレームの進み幅を制限し、コマ落ちで一気にフェードが進むのを防ぐ
            timer += Mathf.Min(Time.deltaTime, 0.05f);
            color.a = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            fadeImage.color = color;
            yield return null;
        }

        color.a = 0f;
        fadeImage.color = color;
    }

    private IEnumerator ShowStartText()
    {
        startTextCanvasGroup.alpha = 1f;
        yield return new WaitForSeconds(startTextDuration);
        startTextCanvasGroup.alpha = 0f;

        // ゲーム開始
        // これにより GameManager.IsGameStarted を見ているメニューも自動的に開けるようになる
        GameManager.StartGame();
    }
}
