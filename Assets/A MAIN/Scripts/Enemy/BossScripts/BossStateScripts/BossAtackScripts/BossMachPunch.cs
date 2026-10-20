using UnityEngine;

/// <summary>
/// ボスのパンチ攻撃を管理するメソッド
/// </summary>
public class BossMachPunch : MonoBehaviour
{
    #region[変数名]
    //---GameObject,Script,Animator等---------------------------------
    [SerializeField]
    private GameObject _punchArea = default;
    [SerializeField]
    private GameObject _dustSprite = default;
    [SerializeField]
    private GameObject _kaisin = default;
    [SerializeField]
    private BossStateManagement _stateManagement = default;
    [SerializeField]
    private NavMesh2DAgent _agent = default;

    //---int,float等の数値---------------------------------
    private float _attenuationRate;
    private float _punchSpeed;
    private float _initialPunchSpeed = 0;

    private Vector3 _directionToPlayerNormalized = default;

    //---bool------------------------------------------------
    private bool _isTargeting = false;
    internal bool _lockRotate = false;

    #endregion

    private void Start()
    {
        _punchSpeed = JsonSaver.Instance.EnemyJson.PunchSpeed;
        _attenuationRate = JsonSaver.Instance.EnemyJson.AttenuationRate;
        if (_stateManagement == null)
        {
            _stateManagement = GetComponent<BossStateManagement>();
        }
    }

    public void MachPunch()
    {
        // サーバー側でのみパンチ突進の方向を決定・突進開始（クライアントでは位置計算を行わない）
        if (_stateManagement != null && !_stateManagement.IsServer) return;
        if (_stateManagement == null || _stateManagement.Player == null)
        {
            return;
        }

        Vector3 _directionToPlayer = _stateManagement.Player.transform.position - this.gameObject.transform.position;
        _directionToPlayerNormalized = _directionToPlayer.normalized;
        _isTargeting = true;
        if (_punchArea != null) _punchArea.SetActive(true);

        if (_dustSprite != null)
        {
            Instantiate(_dustSprite, transform.position, Quaternion.identity);
        }
        _initialPunchSpeed = _punchSpeed;
        _lockRotate = true;
    }

    public void PunchEnd()
    {
        _lockRotate = false;
        if (_punchArea != null) _punchArea.SetActive(false);
        if (_kaisin != null) _kaisin.SetActive(false);

        if (_stateManagement != null && _stateManagement.IsServer)
        {
            _stateManagement._currentState = BossStateManagement.BossState.Cooldown;
        }
    }

    /// <summary>
    /// パンチでプレイヤー方向へ突進（※サーバー側のみ実行し、クライアントはNetworkTransformで同期する）
    /// </summary>
    public void PunchToPlayer()
    {
        // クライアント側では位置を直接書き換えない
        if (_stateManagement != null && !_stateManagement.IsServer) return;

        if (_isTargeting)
        {
            _punchSpeed *= _attenuationRate;
            transform.position += _directionToPlayerNormalized * _punchSpeed * Time.deltaTime;
        }
    }

    public void EndTargeting()
    {
        if (_stateManagement != null && !_stateManagement.IsServer) return;

        _isTargeting = false;
        _punchSpeed = _initialPunchSpeed;
        if (_kaisin != null)
        {
            _kaisin.SetActive(true);
        }
    }
}