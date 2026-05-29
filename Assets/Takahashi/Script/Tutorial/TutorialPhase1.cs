using UnityEngine;

/// <summary>
/// 一番目のチュートリアルを管理するスクリプト
/// </summary>
public class TutorialPhase1 : MonoBehaviour
{
    [SerializeField, Header("デコイ")]
    private GameObject _phase1Decoy = default;

    [SerializeField, Header("キャンバスのアニメーター")]
    private Animator _phase1canvasAnim = default;

    /// <summary>
    /// フェーズを終了させられるかどうかを管理するメソッド
    /// </summary>
    /// <returns>フェーズ終了可能ならtrue</returns>
    public bool EndPhase()
    {
        if(_phase1Decoy != null)
        {
            return false;
        }

        _phase1canvasAnim.SetTrigger("Hide");
        return true;
    }
}
