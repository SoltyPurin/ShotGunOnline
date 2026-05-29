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


    //---int,floatなどの数値---------------------------------
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
    }

    public void MachPunch()
    {
        Vector3 _directionToPlayer = _stateManagement.Player.transform.position - this.gameObject.transform.position;
        _directionToPlayerNormalized = _directionToPlayer.normalized;
        _isTargeting = true;
        _punchArea.SetActive(true);


        Instantiate(_dustSprite);
        _initialPunchSpeed = _punchSpeed;
        _lockRotate = true;
    }

    public void PunchEnd()
    {
        _lockRotate = false;
        _punchArea.SetActive(false);
        _kaisin.SetActive(false);
        _stateManagement._currentState = BossStateManagement.BossState.Cooldown;
    }

    public void PunchToPlayer()
    {
        if (_isTargeting)
        {
            _punchSpeed *= _attenuationRate; // 減衰率は調整可能
            transform.position += _directionToPlayerNormalized * _punchSpeed * Time.deltaTime;
        }
    }

    public void EndTargeting()
    {
        _isTargeting = false;
        _punchSpeed = _initialPunchSpeed;
        if (!_kaisin)
        {
            _kaisin.SetActive(true);
        }
    }
}
