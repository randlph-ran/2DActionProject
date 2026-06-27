using UnityEngine;

/// <summary>
/// SE・BGMの再生を管理する。
/// Title Sceneなどに配置し、DontDestroyOnLoadで持続させる。
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("SE設定")]
    [Tooltip("SE再生用AudioSource（複数同時再生のためAudioSource.PlayOneShotを使う）")]
    [SerializeField]
    private AudioSource seSource;

    [Tooltip("SE全体の音量")]
    [SerializeField, Range(0f, 1f)]
    private float seVolume = 1f;

    [Header("BGM設定")]
    [Tooltip("BGM再生用AudioSource（ループ再生用に専用で持つ）")]
    [SerializeField]
    private AudioSource bgmSource;

    [Tooltip("BGM全体の音量")]
    [SerializeField, Range(0f, 1f)]
    private float bgmVolume = 1f;

    private void Awake()
    {
        // 既に存在する場合は自身を破棄（シーン遷移で重複生成されるのを防ぐ）
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// SEを再生する（複数同時再生可）
    /// </summary>
    public void PlaySE(AudioClip clip)
    {
        if (clip == null || seSource == null) return;

        seSource.PlayOneShot(clip, seVolume);
    }

    /// <summary>
    /// BGMを再生する（既に同じ曲が再生中なら何もしない）
    /// </summary>
    public void PlayBGM(AudioClip clip, bool loop = true)
    {
        if (clip == null || bgmSource == null) return;

        // 既に同じ曲が再生中なら再生し直さない
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    /// <summary>
    /// BGMを停止する
    /// </summary>
    public void StopBGM()
    {
        if (bgmSource == null) return;

        bgmSource.Stop();
    }
}
