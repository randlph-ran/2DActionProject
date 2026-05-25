using UnityEngine;

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
        // Enterキー押下
        if (Input.GetKeyDown(KeyCode.Return))
        {
            Debug.Log("Enterキー押された");
            // Scene切替
            SceneManager.LoadScene(nextSceneName);
            Debug.Log("LoadSceneに進む");
        }
    }
}