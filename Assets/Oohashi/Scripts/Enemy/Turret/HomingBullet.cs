using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class HomingBullet : EnemyShotBullet
{
    #region Serialize変数
    [SerializeField, Header("出てから何秒でプレイヤーに飛んでいくか")]
    private float _waitTime = 2f;
    [SerializeField, Header("弾のスプライト")]
    private GameObject _bulletSprite = default;
    [SerializeField, Header("何秒で弾が消えるか")]
    private float _destroyTime = 5;
    [SerializeField, Header("この距離(m)以内に入ったら誘導を切って直進する")]
    private float _stopHomingDistance = 1.8f;
    [SerializeField, Header("誘導を継続する最大時間(秒)")]
    private float _maxHomingDuration = 2.5f;
    #endregion

    #region 変数
    private GameObject _playerObject = default;
    private Vector2 _firstDirection = default;
    private float _initAliveTime = 0;
    private bool _canMoveToPlayer = false;
    private float _smooth = 0.4f;
    private float _homingTimer = 0f;
    private bool _isHomingExpired = false;
    #endregion

    #region 定数
    private readonly string PLAYER_TAG = "Player";
    #endregion

    private void Start()
    {
        _smooth = JsonSaver.Instance.EnemyJson.BurretSmooth;
    }

    ///// <summary>
    ///// プレイヤーのオブジェクトと方向を設定する
    ///// </summary>
    ///// <param name="playerObj">プレイヤーのオブジェクト</param>
    ///// <param name="bulletDirection">最初に飛ぶ進行方向</param>
    //[ClientRpc]
    //public override void DirectionSettingClientRpc(GameObject playerObj, Vector2 bulletDirection)
    //{
    //    _playerObject = playerObj;
    //    _firstDirection = bulletDirection;
    //    _direction = bulletDirection;
    //    _canMove = true;
    //    float angle = Mathf.Atan2(bulletDirection.y, bulletDirection.x) * Mathf.Rad2Deg;
    //    _bulletSprite.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    //    StartCoroutine(WaitMoveToPlayer());
    //}

    /// <summary>
    /// 全クライアント端末上でホーミングパラメータと移動フラグ（_canMove = true）をセットアップ
    /// </summary>
    [ClientRpc]
    protected override void SetDirectionClientRpc(NetworkObjectReference playerRef, Vector2 bulletDirection)
    {
        if (playerRef.TryGet(out NetworkObject pObj))
        {
            _playerObject = pObj.gameObject;
        }

        _firstDirection = bulletDirection;
        _direction = bulletDirection;
        _canMove = true; // ★ これで全クライアント上で移動許可が降りる！

        if (_bulletSprite != null)
        {
            float angle = Mathf.Atan2(bulletDirection.y, bulletDirection.x) * Mathf.Rad2Deg;
            _bulletSprite.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        StartCoroutine(WaitMoveToPlayer());
    }

    /// <summary>
    /// プレイヤーの方向に移動開始フラグを立てる
    /// </summary>
    /// <returns>設定した時間待機</returns>
    private IEnumerator WaitMoveToPlayer()
    {
        yield return new WaitForSeconds(_waitTime);
        _canMoveToPlayer = true;
    }

    /// <summary>
    /// 最も近いプレイヤーを検索する
    /// </summary>
    private GameObject FindNearestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag(PLAYER_TAG);
        GameObject nearest = null;
        float minDistance = float.MaxValue;
        foreach (GameObject p in players)
        {
            if (p == null) continue;
            float dist = Vector2.Distance(transform.position, p.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = p;
            }
        }
        return nearest;
    }

    /// <summary>
    /// 最初は指定された方向に飛び、その後プレイヤーの方向に飛んでいく
    /// </summary>
    public override void FixedUpdate()
    {
        if (_isDestroyed || !_canMove)
        {
            return;
        }

        _initAliveTime += Time.fixedDeltaTime;
        if (_initAliveTime >= _destroyTime)
        {
            if (IsServer)
            {
                DestroyProtocol();
            }
            return;
        }

        if (!_canMoveToPlayer)
        {
            transform.position += (Vector3)_firstDirection * _moveSpeed * Time.fixedDeltaTime;
            return;
        }

        if (_playerObject == null)
        {
            _playerObject = FindNearestPlayer();
            if (_playerObject == null)
            {
                MoveProtocol(_direction);
                return;
            }
        }

        _homingTimer += Time.fixedDeltaTime;

        // 追尾時間切れ、または一定距離まで近づいたら誘導を解除して直進させる
        // （接近時の急旋回による理不尽なヒットを防ぎ、すれ違い回避を可能にする）
        float distanceToPlayer = Vector2.Distance(transform.position, _playerObject.transform.position);
        if (_isHomingExpired || _homingTimer >= _maxHomingDuration || distanceToPlayer <= _stopHomingDistance)
        {
            _isHomingExpired = true;
            MoveProtocol(_direction);
            return;
        }

        Vector3 playerObj = _playerObject.transform.position;
        Vector3 targetDir = (playerObj - this.transform.position).normalized;
        float t = _smooth * Time.fixedDeltaTime;
        _direction = Vector3.Slerp(_direction, targetDir, t).normalized;
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