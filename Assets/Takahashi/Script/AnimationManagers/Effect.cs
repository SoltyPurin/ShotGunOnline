using UnityEngine;

/// <summary>
/// 敵死亡時に出現するエフェクトのアニメーションなどを管理するクラス
/// 【作成者：髙橋英士】
/// </summary>
public class Effect : MonoBehaviour
{
    [SerializeField, Header("アニメーター")]
    private Animator _animator = default;

    /// <summary>
    /// アニメーション開始用メソッド（使わない）
    /// </summary>
    public void StartAnim()
    {
        _animator.SetTrigger("Start");
    }

    /// <summary>
    /// 自身を消滅させるためのメソッド
    /// </summary>
    public void DestroyThis()
    {
        Destroy(this.gameObject);
    }
}
