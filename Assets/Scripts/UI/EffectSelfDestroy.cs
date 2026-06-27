using UnityEngine;

/// エフェクトオブジェクトがアニメーション終了後に自動で削除されるようにするスクリプト
public class EffectSelfDestroy : MonoBehaviour
{
    public void DestroySelf()
    {
        Destroy(gameObject);
    }
}