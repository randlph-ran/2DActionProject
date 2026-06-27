// Scene切替に必要
using UnityEngine;
using UnityEngine.SceneManagement;

public class GoalPoint : MonoBehaviour
{
    // 次に移動するScene名
    [Tooltip("次に移動するScene名")]
    [SerializeField]
    private string nextSceneName;

    // Triggerへ何か入った時に呼ばれる
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 接触相手がPlayerなら
        if (other.CompareTag("Player"))
        {
            // 次シーン開始時はフェード待ち状態に戻す
            GameManager.ResetGameState();

            // シーン切替
            SceneManager.LoadScene(nextSceneName);

        }
    }
}