using UnityEngine;

/// <summary>
/// アタッチしたオブジェクトをシーンをまたいで持続させる。
/// 同名のオブジェクトが既に存在する場合は自身を破棄して重複を防ぐ。
/// Player と Menu のルートオブジェクトにアタッチする。
/// </summary>
public class PersistentObject : MonoBehaviour
{
    private void Awake()
    {
        // 同じ名前のオブジェクトが既に存在するか確認
        PersistentObject[] all = FindObjectsByType<PersistentObject>(FindObjectsSortMode.None);
        foreach (PersistentObject other in all)
        {
            if (other != this && other.gameObject.name == gameObject.name)
            {
                // 自身は重複なので破棄して処理を止める
                Destroy(gameObject);
                return;
            }
        }

        // 最初の1つだけ持続させる
        DontDestroyOnLoad(gameObject);
    }
}
