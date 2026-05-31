using UnityEngine;

public class GameManager : MonoBehaviour
{
    // ゲーム開始フラグ
    public static bool IsGameStarted { get; private set; }

    // ゲーム開始前の初期化
    private void Awake()
    {
        // ゲーム開始前はフラグをfalseに設定
        IsGameStarted = false;
    }

    public static void StartGame()
    {
        // ゲーム開始フラグをtrueに設定
        IsGameStarted = true;
    }
}