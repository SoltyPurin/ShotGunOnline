using UnityEngine;

/// <summary>
/// タイトル項目選択アニメーション用スクリプト
/// 【作成者：髙橋英士】
/// </summary>
public class TriggerChoiceAnim : MonoBehaviour
{
    [SerializeField, Header("親のアニメーター")]
    private Animator _parentAnimator = default;

    /// <summary>
    /// 一番目が選択された時のメソッド
    /// </summary>
    public void TriggerToChoice1()
    {
        _parentAnimator.SetTrigger("1");
    }

    /// <summary>
    /// 二番目が選択された時のメソッド
    /// </summary>
    public void TriggerToChoice2()
    {
        _parentAnimator.SetTrigger("2");
    }

    /// <summary>
    /// 三番目が選択された時のメソッド
    /// </summary>
    public void TriggerToChoice3()
    {
        _parentAnimator.SetTrigger("3");
    }

    /// <summary>
    /// EXオプションが選択されたときのメソッド
    /// </summary>
    public void TriggerToChoice4()
    {
        _parentAnimator.SetTrigger("4");
    }
}
