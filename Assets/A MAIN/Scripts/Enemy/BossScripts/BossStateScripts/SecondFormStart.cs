using UnityEngine;
/// <summary>
/// ボスの第二形態変移時のステータスアップのメソッド
/// </summary>
public class SecondFormStart : MonoBehaviour
{
    #region[変数名]
    //---GameObject,Script,Animator等---------------------------------
    [SerializeField]
    private GameObject _secondEffect = default;
    [SerializeField]
    private NavMesh2DAgent _agent = default;
    [SerializeField]
    private BossStateManagement _stateManagement = default;
    [SerializeField]
    private BossHP _bossHP = default;
    [SerializeField]
    private SpriteRenderer _bossSprite = default;
    [SerializeField]
    private BossAnimeManager _animeManager = default;

    //---int,floatなどの数値---------------------------------
    private Color _secondColor = new Color(1, 0.47f, 0.47f);
    [SerializeField]
    private float _secondSpeed = 15.0f;

    #endregion
    public void ToBESecondForm()
    {
        _bossSprite.color = _secondColor;
        _bossHP._isInvincible = true;
        _animeManager.ResetAllTriggers();
        _stateManagement.enabled = false;
        _secondEffect.SetActive(true);
        _agent._moveSpeed = _secondSpeed;

    }
}
