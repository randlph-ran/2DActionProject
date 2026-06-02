using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameQuitManager : MonoBehaviour
{
    // シングルトン管理用
    private static GameQuitManager instance;

    private void Awake()
    {
        // 既に存在する場合は自分を削除
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        // 自分を登録
        instance = this;

        // シーンを跨いで保持
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        // Escキー押下
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            QuitGame();
        }
    }

    /// <summary>
    /// ゲーム終了処理
    /// </summary>
    private void QuitGame()
    {
#if UNITY_EDITOR

        // Unityエディタの場合はPlayモード終了
        EditorApplication.isPlaying = false;

#else

        // ビルド版ではアプリ終了
        Application.Quit();

#endif
    }
}