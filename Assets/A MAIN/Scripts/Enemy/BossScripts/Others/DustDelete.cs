using UnityEngine;

/// <summary>
/// 砂埃エフェクトを消すメソッド
/// </summary>
public class DustDelete : MonoBehaviour
{
    [SerializeField]
    private bool _isJumpDust = default;
    public void DelDust()
    {
        if (_isJumpDust)
        {
            this.gameObject.SetActive(false);
        }
        else
        {
            Destroy(this.gameObject);

        }
    }
}
