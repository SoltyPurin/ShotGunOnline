using UnityEngine;

/// <summary>
/// 死んだことをチュートリアルマネージャーに知らせるスクリプト
/// チュートリアル内の敵にアタッチする（decoyは除く）
/// </summary>
public class ReturnCount : MonoBehaviour
{
    [SerializeField, Header("取得系")]
    public TutorialCountUpper _countUpper = default;

    /// <summary>
    /// デストロイした時、カウントを1つ上げる。
    /// </summary>
    private void OnDestroy()
    {
        _countUpper.CountDown();
    }
}
