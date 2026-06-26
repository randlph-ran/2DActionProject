using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Scene切替用
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    // 遷移先Scene名
    [Tooltip("遷移先Scene名")]
    [SerializeField]
    private string nextSceneName = "Stage1_1";

    [Tooltip("スタート入力後、Scene遷移するまでの待機時間（秒）")]
    [SerializeField]
    private float startDelay = 1f;

    [Header("サウンド")]

    [Tooltip("Titleで再生するBGM")]
    [SerializeField]
    private AudioClip titleBGM;

    [Tooltip("ゲームスタート時に再生するSE")]
    [SerializeField]
    private AudioClip startSE;

    // スタート入力済みか（多重入力防止）
    private bool isStarting;

    private void Start()
    {
        // TitleのBGMを再生
        SoundManager.Instance?.PlayBGM(titleBGM);
    }

    // Update:
    // 毎フレーム処理
    private void Update()
    {
        // スタート済みなら入力を無視する
        if (isStarting) return;

        // Enterキーまたは左クリック、ゲームパッドはEastボタン（メニューの決定と統一）でScene遷移
        bool startPressed = Keyboard.current.enterKey.wasPressedThisFrame
            || Mouse.current.leftButton.wasPressedThisFrame
            || (Gamepad.current != null &&
            Gamepad.current.buttonEast.wasPressedThisFrame);

        // 遷移条件を満たしたらScene遷移
        if (startPressed)
        {
            isStarting = true;

            // スタートSEを再生
            SoundManager.Instance?.PlaySE(startSE);

            // 1秒ほど間を置いてからScene遷移
            StartCoroutine(StartGameCoroutine());
        }
    }

    // 一定時間待ってからScene遷移する
    private IEnumerator StartGameCoroutine()
    {
        yield return new WaitForSeconds(startDelay);

        // Scene遷移
        SceneManager.LoadScene(nextSceneName);
    }
}
