using Unity.Netcode;
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
    [SerializeField, Header("穴あけ")]
    protected GameObject _shockHole;
    [SerializeField, Header("土煙")]
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
        if (_mainCamera != null)
        {
            _mainCameraShake = _mainCamera.GetComponent<BossJumpAtackShake>();
        }
        if (_stateManagement == null)
        {
            _stateManagement = GetComponent<BossStateManagement>();
        }
    }

    public void JumpAtackToPlayer()
    {
        if (_bossCollider != null)
        {
            _bossCollider.isTrigger = true;
        }
    }

    // 着地時のエフェクト再生（全クライアントへ通知）
    [ClientRpc]
    public void JumpLandClientRpc()
    {
        if (_mainCameraShake != null)
        {
            _mainCameraShake.Shake();
        }
        if (_jumpAtackArea != null) _jumpAtackArea.SetActive(true);
        if (_dustFromJumpAttack != null) _dustFromJumpAttack.SetActive(true);

        Vector3 shockHolePos = new Vector3(transform.position.x, transform.position.y - 2, 0);
        if (_shockHole != null)
        {
            Instantiate(_shockHole, shockHolePos, Quaternion.identity);
        }

        if (IsServer && _stateManagement != null && _stateManagement._changed)
        {
            for (int i = 0; i < _shockLangeList.Length; i++)
            {
                if (_stateManagement.Player == null) break;
                _jumpAtackWaveDirection = (_stateManagement.Player.transform.position - transform.position).normalized;
                Vector2 rotatedDir = Quaternion.AngleAxis(_shockLangeList[i], Vector3.forward) * _jumpAtackWaveDirection;
                EnemyShotBullet shotBullet = Instantiate(_shockWave, transform.position, Quaternion.identity).GetComponent<EnemyShotBullet>();
                NetworkObject nBullet = shotBullet.GetComponent<NetworkObject>();
                shotBullet.transform.localScale = new Vector3(3, 3, 1);
                shotBullet.DirectionSetting(this.gameObject, rotatedDir);
                nBullet.Spawn();
            }
        }
        if (_bossCollider != null)
        {
            _bossCollider.isTrigger = false;
        }
    }

    public void JumpAttackEnd()
    {
        if (_jumpAtackArea != null) _jumpAtackArea.SetActive(false);
        if (_stateManagement != null && _stateManagement.IsServer)
        {
            _stateManagement._currentState = BossStateManagement.BossState.Cooldown;
        }
    }

    /// <summary>
    /// プレイヤーの位置へジャンプ移動（※サーバー側のみ実行し、クライアントはNetworkTransformで同期する）
    /// </summary>
    public void MoveToJumpAtackPosition()
    {
        // クライアント側では位置を直接書き換えない（NetworkTransformと競合して位置ズレを起こすのを防ぐ）
        if (_stateManagement != null && !_stateManagement.IsServer) return;
        if (_stateManagement == null || _stateManagement.Player == null) return;

        Vector3 targetPosition = new Vector3(
            _stateManagement.Player.transform.position.x,
            _stateManagement.Player.transform.position.y,
            transform.position.z);

        // Lerpで移動
        Vector3 newPos = Vector3.Lerp(transform.position, targetPosition, 0.1f);

        // プレイヤーからの距離を制限
        Vector3 offset = newPos - targetPosition;
        float maxDistance = 15.0f;
        offset = Vector3.ClampMagnitude(offset, maxDistance);

        // 位置更新
        transform.position = targetPosition + offset;
    }

    public void AimStart()
    {
        if (_stateManagement != null && _stateManagement.IsServer)
        {
            _stateManagement._aimFall = false;
        }
    }

    public void AimEnd()
    {
        if (_stateManagement != null && _stateManagement.IsServer)
        {
            _stateManagement._aimFall = true;
        }
    }
}