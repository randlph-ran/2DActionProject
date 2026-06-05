using UnityEngine;
using UnityEngine.InputSystem;

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
        // Escapeキーが押されたらゲーム終了
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
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