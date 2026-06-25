using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Opening / S1Clear / S2Clear など、1枚絵+ストーリーテキストのページを
// 複数枚（最大9枚）切り替えながら表示し、最後のページで決定入力すると
// 次Sceneへ進むイベントシーン共通スクリプト
public class StoryEventScene : MonoBehaviour
{
    // 1ページ分のデータ（背景画像＋テキスト）
    [Serializable]
    private class StoryPage
    {
        [Tooltip("このページで表示する背景の1枚絵")]
        public Sprite backgroundSprite;

        [Tooltip("このページで表示するストーリーテキスト")]
        [TextArea(2, 5)]
        public string storyText;

        [Tooltip("このページから次ページへ進む時に再生するSE")]
        public AudioClip advanceSE;
    }

    [Header("ページ設定（上限9枚）")]
    [Tooltip("表示するページを上から順に並べる。1〜9枚まで設定可能")]
    [SerializeField]
    private StoryPage[] pages = new StoryPage[1];

    [Header("表示先")]
    [Tooltip("背景の1枚絵を表示するImage")]
    [SerializeField]
    private Image backgroundImage;

    [Tooltip("背景の明るさ（0〜1、低いほど暗くなる）")]
    [SerializeField, Range(0f, 1f)]
    private float backgroundBrightness = 0.5f;

    [Tooltip("ストーリーテキストを表示するTextMeshProUGUI")]
    [SerializeField]
    private TextMeshProUGUI storyText;

    [Tooltip("テキストが表示されているCanvasGroup（フェード制御用）")]
    [SerializeField]
    private CanvasGroup storyTextCanvasGroup;

    [Header("タイミング設定")]
    [Tooltip("テキストが完全に表示されるまでの時間（秒）")]
    [SerializeField]
    private float textFadeDuration = 2f;

    [Tooltip("誤爆防止のため、ページごとに入力受付を開始するまでの待機時間（秒）")]
    [SerializeField]
    private float inputDelay = 1f;

    [Header("遷移設定")]
    [Tooltip("最後のページで決定入力した時に移動するScene名")]
    [SerializeField]
    private string nextSceneName;

    [Header("サウンド")]
    [Tooltip("このシーンで再生するBGM")]
    [SerializeField]
    private AudioClip bgm;

    [Tooltip("最後のページから次Sceneへ遷移する時に再生するSE")]
    [SerializeField]
    private AudioClip sceneTransitionSE;

    // 現在表示中のページ番号
    private int currentPageIndex;

    // 入力受付可能か
    private bool canAdvance;

    // ページ切替中か（多重実行防止）
    private bool isShowingPage;

    private const int MaxPageCount = 9;

    private void Start()
    {
        // このシーンのBGMを再生
        SoundManager.Instance?.PlayBGM(bgm);

        // ページ数の上限チェック
        if (pages.Length > MaxPageCount)
        {
            Debug.LogWarning($"StoryEventScene: pagesは上限{MaxPageCount}枚までです。先頭から{MaxPageCount}枚のみ使用します");
        }

        currentPageIndex = 0;
        ShowPage(currentPageIndex);
    }

    // 指定ページを表示する
    private void ShowPage(int pageIndex)
    {
        StoryPage page = pages[pageIndex];

        // 背景画像を切替えて暗さを適用
        if (backgroundImage != null)
        {
            backgroundImage.sprite = page.backgroundSprite;

            Color color = backgroundImage.color;
            color.r = color.g = color.b = backgroundBrightness;
            backgroundImage.color = color;
        }

        // テキストを切替え
        if (storyText != null)
        {
            storyText.text = page.storyText;
        }

        // テキストは非表示からフェードインさせる
        if (storyTextCanvasGroup != null)
        {
            storyTextCanvasGroup.alpha = 0f;
        }

        StartCoroutine(ShowPageCoroutine());
    }

    // テキストをじわっと表示してから入力受付を開始する
    private IEnumerator ShowPageCoroutine()
    {
        isShowingPage = true;
        canAdvance = false;

        float timer = 0f;

        while (timer < textFadeDuration)
        {
            timer += Time.deltaTime;

            if (storyTextCanvasGroup != null)
            {
                storyTextCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / textFadeDuration);
            }

            yield return null;
        }

        if (storyTextCanvasGroup != null)
        {
            storyTextCanvasGroup.alpha = 1f;
        }

        // 誤爆防止のため一定時間待ってから入力受付開始
        yield return new WaitForSeconds(inputDelay);

        isShowingPage = false;
        canAdvance = true;
    }

    private void Update()
    {
        // ページ切替中、または入力受付前なら無視
        if (isShowingPage || !canAdvance) return;

        // 決定ボタンまたはキー入力
        bool confirmPressed = Keyboard.current.enterKey.wasPressedThisFrame
            || Mouse.current.leftButton.wasPressedThisFrame
            || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);

        if (!confirmPressed) return;

        // 最後のページなら次Sceneへ、そうでなければ次ページを表示
        bool isLastPage = currentPageIndex >= pages.Length - 1 || currentPageIndex >= MaxPageCount - 1;

        if (isLastPage)
        {
            // シーン遷移時のSE再生（ページ送りとは別の音を鳴らせる）
            SoundManager.Instance?.PlaySE(sceneTransitionSE);

            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            // ページ送り時のSE再生（ページごとに個別設定）
            SoundManager.Instance?.PlaySE(pages[currentPageIndex].advanceSE);

            currentPageIndex++;
            ShowPage(currentPageIndex);
        }
    }
}
