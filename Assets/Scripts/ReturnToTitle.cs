using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnToTitle : MonoBehaviour
{
    // タイトルへ戻るまでの時間
    [SerializeField]
    private float returnTime = 5f;

    private void Start()
    {
        // カウント開始
        StartCoroutine(ReturnToTitleCoroutine());
    }

    /// <summary>
    /// 一定時間後にタイトルへ戻る
    /// </summary>
    private IEnumerator ReturnToTitleCoroutine()
    {
        // 指定秒数待機
        yield return new WaitForSeconds(returnTime);

        // タイトルシーンへ移動
        SceneManager.LoadScene("Title");
    }
}
