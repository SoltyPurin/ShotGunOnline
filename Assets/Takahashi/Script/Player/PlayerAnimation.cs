using UnityEngine;

/// <summary>
/// プレイヤーのアニメーションを管理するスクリプト
/// </summary>
public class PlayerAnimation : MonoBehaviour
{
    [SerializeField, Header("正面のアニメーター")]
    private Animator _frontAnim = default;

    [SerializeField, Header("背面のアニメーター")]
    private Animator _backAnim = default;

    [SerializeField, Header("スプライトマネージャーのアニメータ")]
    private Animator _spriteManagerAnim = default;

    [SerializeField, Header("ソウルゲット時のエフェクト")]
    private Animator _soulGetEffect = default;

    private void Start()
    {
        //アニメーションBoolの初期化
        _frontAnim.SetBool("KnockBack", false);
        _frontAnim.SetBool("Run", false);
        _frontAnim.SetBool("Wait", false);
        _backAnim.SetBool("KnockBack", false);
        _backAnim.SetBool("Run", false);
        _backAnim.SetBool("Wait", false);
    }

    /// <summary>
    /// 待機状態のアニメーション遷移
    /// </summary>
    public void Wait()
    {
        _frontAnim.SetBool("Run", false);
        _frontAnim.SetBool("KnockBack", false);
        _backAnim.SetBool("Run", false);
        _backAnim.SetBool("KnockBack", false);
    }

    /// <summary>
    /// 走るアニメーションの遷移
    /// </summary>
    /// <param name="Speed">アニメーションスピードを代入０～１</param>
    public void Run(float Speed)
    {
        _frontAnim.SetBool("Run", true);
        _backAnim.SetBool("Run", true);
        _frontAnim.SetFloat("RunSpeed", Speed);
        _backAnim.SetFloat("RunSpeed", Speed);
    }

    /// <summary>
    /// ノックバック状態のアニメーション遷移
    /// </summary>
    public void KnockBack()
    {
        _frontAnim.SetBool("KnockBack", true);
        _backAnim.SetBool("KnockBack", true);
    }

    /// <summary>
    /// ソウルゲット時のアニメーション遷移
    /// </summary>
    public void SoulGet()
    {
        _soulGetEffect.SetTrigger("Get");
        _spriteManagerAnim.SetTrigger("Flash");
    }
}