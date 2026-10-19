using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class HomingBullet : EnemyShotBullet
{
    #region Serialize変数
    [SerializeField, Header("出現してから何秒後にプレイヤーに飛んでいくか")]
    private float _waitTime = 2f;
    [SerializeField,Header("銃弾のスプライト")]
    private GameObject _bulletSprite = default;
    [SerializeField, Header("何秒経過したら消えるか")]
    private float _destroyTime = 5;
    #endregion
    #region 変数
    private GameObject _playerObject = default;
    private Vector2 _firstDirection = default;
    private float _initAliveTime = 0;
    private bool _canMoveToPlayer = false;
    private float _smooth = 0.4f;
    private Vector3 _lastPosition = default;
    private bool _hasLastPosition = false;
    #endregion

    private void Start()
    {
        _smooth = JsonSaver.Instance.EnemyJson.BurretSmooth;
    }
    /// <summary>
    /// プレイヤーのオブジェクトと方向を設定する
    /// </summary>
    /// <param name="playerObj">プレイヤーのオブジェクト</param>
    /// <param name="bulletDirection">最初に飛んで行く方向</param>
    public override void DirectionSetting(GameObject playerObj,Vector2 bulletDirection)
    {
        _playerObject = playerObj;
        _firstDirection = bulletDirection;
        _direction = bulletDirection;
        float angle = Mathf.Atan2(bulletDirection.y, bulletDirection.x) * Mathf.Rad2Deg;
        _bulletSprite.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        StartCoroutine(WaitMoveToPlayer());
    }

    /// <summary>
    /// プレイヤーの方向に移動開始させるフラグを立てる
    /// </summary>
    /// <returns>変数で設定した時間分待つ</returns>
    private IEnumerator WaitMoveToPlayer()
    {
        yield return new WaitForSeconds(_waitTime);
        _canMoveToPlayer = true;
    }

    private bool IsClientOnly()
    {
        return NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsServer;
    }

    /// <summary>
    /// 繧ｯ繝ｩ繧､繧｢繝ｳ繝亥�ｴ縺ｯ蠑ｾ縺ｮ蜷代″縺悟酔譛溘＆繧後↑縺�縺溘ａ縲∝ｮ滄圀縺ｮ遘ｻ蜍墓婿蜷代°繧牙髄縺阪ｒ豎ｺ繧√ｋ
    /// </summary>
    private void LateUpdate()
    {
        if (!IsClientOnly()) return;
        if (_hasLastPosition)
        {
            Vector3 delta = transform.position - _lastPosition;
            if (delta.sqrMagnitude > 0.000001f)
            {
                float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                _bulletSprite.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }
        _lastPosition = transform.position;
        _hasLastPosition = true;
    }
    /// <summary>
    /// 最初は指定された方向に飛び、その後プレイヤーの方向に飛んでいく
    /// </summary>
    public override void FixedUpdate()
    {
        // ???????????????(???????????????)
        if (IsClientOnly()) return;
        _initAliveTime += Time.fixedDeltaTime;
        if(_initAliveTime >= _destroyTime)
        {
            Destroy(this.gameObject);
        }

        if (!_canMoveToPlayer)
        {
            transform.position += (Vector3)_firstDirection * _moveSpeed * Time.fixedDeltaTime;
            return;
        }

        Vector3 playerObj = _playerObject.transform.position;
        Vector3 targetDir = (playerObj - this.transform.position).normalized;
        float t = _smooth * Time.fixedDeltaTime;
        _direction = Vector3.Slerp(_direction, targetDir,t).normalized;
        MoveProtocol(_direction);
    }
    /// <summary>
    /// 移動のメソッド
    /// </summary>
    /// <param name="direction"></param>
    private void MoveProtocol(Vector3 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        _bulletSprite.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        transform.position += direction * _moveSpeed * Time.fixedDeltaTime;
    }
}
