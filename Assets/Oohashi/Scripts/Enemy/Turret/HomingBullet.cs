using System.Collections;
using System.Collections.Generic;
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
    #endregion

    #region 変数
    private GameObject _playerObject = default;
    private Vector2 _firstDirection = default;
    private float _initAliveTime = 0;
    private bool _canMoveToPlayer = false;
    private float _smooth = 0.4f;
    #endregion

    #region 定数
    private readonly string PLAYER_TAG = "Player";
    #endregion

    private void Start()
    {
        _smooth = JsonSaver.Instance.EnemyJson.BurretSmooth;
    }

    /// <summary>
    /// プレイヤーのオブジェクトと方向を設定する
    /// </summary>
    /// <param name="playerObj">プレイヤーのオブジェクト</param>
    /// <param name="bulletDirection">最初に飛ぶ進行方向</param>
    public override void DirectionSetting(GameObject playerObj, Vector2 bulletDirection)
    {
        _playerObject = playerObj;
        _firstDirection = bulletDirection;
        _direction = bulletDirection;
        float angle = Mathf.Atan2(bulletDirection.y, bulletDirection.x) * Mathf.Rad2Deg;
        _bulletSprite.transform.rotation = Quaternion.Euler(0f, 0f, angle);
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
        _initAliveTime += Time.fixedDeltaTime;
        if (_initAliveTime >= _destroyTime)
        {
            Destroy(this.gameObject);
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
