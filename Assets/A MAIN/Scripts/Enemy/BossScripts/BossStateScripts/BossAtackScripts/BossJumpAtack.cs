using UnityEngine;

/// <summary>
/// ボスのジャンプ攻撃を管理するメソッド
/// </summary>
public class BossJumpAtack : BossColliderTriggerSW
{
    #region[変数名]
    //---GameObject,Script,Animator等---------------------------------
    [SerializeField, Header("衝撃波のオブジェクト")]
    protected GameObject _shockWave;
    [SerializeField, Header("地割れ")]
    protected GameObject _shockHole;
    [SerializeField, Header("砂埃")]
    protected GameObject _dustFromJumpAttack;
    [SerializeField]
    private GameObject _jumpAtackArea = default;
    [SerializeField]
    private BossStateManagement _stateManagement = default;
    private GameObject _mainCamera = default;
    private BossJumpAtackShake _mainCameraShake = default;

    protected float[] _shockLangeList = { 0, 45, 90, 135, 180, -45, -90, -135 };
    protected Vector2 _jumpAtackWaveDirection;

    #endregion

    private void Start()
    {
        _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        _mainCameraShake = _mainCamera.GetComponent<BossJumpAtackShake>();
    }

    public void JumpAtackToPlayer()
    {
        _bossCollider.isTrigger = true;
    }

    //着地時の当たり判定発生
    public void JumpLand()
    {
        _mainCameraShake.Shake();
        _jumpAtackArea.SetActive(true);
        _dustFromJumpAttack.SetActive(true);
        Vector3 shockHolePos = new Vector3(transform.position.x, transform.position.y - 2, 0);
        Instantiate(_shockHole, shockHolePos, Quaternion.identity);


        if (_stateManagement._changed)
        {
            for (int i = 0; i < _shockLangeList.Length; i++)
            {
                _jumpAtackWaveDirection = (_stateManagement.Player.transform.position - transform.position).normalized;
                Vector2 rotatedDir = Quaternion.AngleAxis(_shockLangeList[i], Vector3.forward) * _jumpAtackWaveDirection;
                EnemyShotBullet shotBullet = Instantiate(_shockWave, transform.position, Quaternion.identity).GetComponent<EnemyShotBullet>();
                shotBullet.transform.localScale = new Vector3(3, 3, 1);
                shotBullet.DirectionSetting(this.gameObject,rotatedDir);
            }
        }
        
        _bossCollider.isTrigger = false;
    }

    public void JumpAttackEnd()
    {
        _jumpAtackArea.SetActive(false);
        _stateManagement._currentState = BossStateManagement.BossState.Cooldown;
    }
    public void MoveToJumpAtackPosition()
    {
        Vector3 targetPosition = new Vector3(
        _stateManagement.Player.transform.position.x,
        _stateManagement.Player.transform.position.y,
        transform.position.z);

        // Lerpで近づく
        Vector3 newPos = Vector3.Lerp(transform.position, targetPosition, 0.1f);

        // プレイヤーとの相対ベクトル
        Vector3 offset = newPos - targetPosition;

        // 最大距離を制限
        float maxDistance = 15.0f;
        offset = Vector3.ClampMagnitude(offset, maxDistance);

        // 制限を掛けた位置に更新
        transform.position = targetPosition + offset;

    }
    public void AimStart()
    {
        _stateManagement._aimFall = false;
    }
    public void AimEnd()
    {
        _stateManagement._aimFall = true;
    }
}
