using UnityEngine;

// 通常ステージシーンでBGMを再生するための最小限のコンポーネント
// シーン内の常駐オブジェクト（GameManager等）にアタッチして使う
public class StageBGM : MonoBehaviour
{
    [Tooltip("このシーンで再生するBGM")]
    [SerializeField]
    private AudioClip bgm;

    private void Start()
    {
        // このシーンのBGMを再生
        SoundManager.Instance?.PlayBGM(bgm);
    }
}
