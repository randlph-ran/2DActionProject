using UnityEngine;
using UnityEngine.InputSystem;

// Scene切替用
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    // 遷移先Scene名
    [SerializeField]
    private string nextSceneName = "Stage1_1";

    // Update:
    // 毎フレーム処理
    private void Update()
    {
        // Enterキーまたは左クリックでScene遷移
        bool startPressed = Keyboard.current.enterKey.wasPressedThisFrame
            || Mouse.current.leftButton.wasPressedThisFrame
            || (Gamepad.current != null &&
            Gamepad.current.buttonSouth.wasPressedThisFrame);
        // 遷移条件を満たしたらScene遷移
        if (startPressed)
        {
            // Scene遷移
            SceneManager.LoadScene(nextSceneName);
        }
    }
}