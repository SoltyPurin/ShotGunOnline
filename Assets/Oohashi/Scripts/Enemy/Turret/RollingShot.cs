using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RollingShot : MonoBehaviour
{
    #region Serialize変数
    [SerializeField, Header("何秒に一回攻撃するか")]
    private float _rollingTimeing = 2f;

    [SerializeField, Header("弾のオブジェクト")]
    private GameObject _bulletObj = default;

    [SerializeField, Header("何度刻みで弾を出すか")]
    private int _carveValue = 30;
    #endregion

    #region 変数
    private float _rollingAngle = 360;
    private float _initRollingWaitTime = 0;
    private GameObject _playerObject = default;
    #endregion

    #region 定数
    private readonly string PLAYER_TAG = "Player";
    #endregion

    private void Awake()
    {
        _playerObject = FindNearestPlayer();
    }

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

    private void Update()
    {
        if (_initRollingWaitTime > _rollingTimeing)
        {
            _initRollingWaitTime = 0;
            _playerObject = FindNearestPlayer();

            for (int i = 0; i < _rollingAngle; i += _carveValue)
            {
                float angleDeg = i;
                float angleRad = angleDeg * Mathf.Deg2Rad; // ラジアンに変換
                Vector2 bulletDirection = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
                GameObject obj = Instantiate(_bulletObj, transform.position, Quaternion.identity);
                HomingBullet homing = obj.GetComponent<HomingBullet>();
                homing.DirectionSetting(_playerObject, bulletDirection);
            }
        }
        else
        {
            _initRollingWaitTime += Time.deltaTime;
        }
    }
}
