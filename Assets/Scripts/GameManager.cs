using UnityEngine;

public class GameManager : MonoBehaviour
{
    // ゲーム開始フラグ
    public static bool IsGameStarted { get; private set; }

    // ゲーム開始前の初期化
    private void Awake()
    {
        Debug.Log("GameManager Awake");
        Debug.Log($"{gameObject.scene.name} の GameManager Awake");

        IsGameStarted = false;
    }


    /// <summary>
    /// ゲーム開始処理を実行するメソッド
    /// </summary>
    public static void StartGame()
    {
        //ログ
        Debug.Log("StartGame 実行");

        // ゲーム開始フラグをtrueに設定
        IsGameStarted = true;
    }

    // ゲーム開始状態をリセット
    public static void ResetGameState()
    {
        IsGameStarted = false;
    }
}