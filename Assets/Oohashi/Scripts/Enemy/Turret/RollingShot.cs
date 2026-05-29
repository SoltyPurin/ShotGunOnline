using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RollingShot : MonoBehaviour
{
    #region Serialize•Ï”
    [SerializeField, Header("‰½•b‚Éˆê‰ñ‰ñ“]UŒ‚‚·‚é‚©")]
    private float _rollingTimeing = 2f;

    [SerializeField, Header("e’e‚ÌƒIƒuƒWƒFƒNƒg")]
    private GameObject _bulletObj = default;

    [SerializeField, Header("‰½“x‚Ý‚É’e‚ðo‚·‚©")]
    private int _carveValue = 30;
    #endregion

    #region •Ï”

    private float _rollingAngle = 360;

    private float _initRollingWaitTime = 0;

    private GameObject _playerObject = default;
    #endregion

    #region ’è”

    private readonly string PLAYER_TAG = "Player";
    #endregion
    private void Awake()
    {
        _playerObject = GameObject.FindWithTag(PLAYER_TAG);
    }

    private void Update()
    {
        if( _initRollingWaitTime > _rollingTimeing)
        {
            _initRollingWaitTime = 0;
            for(int i =0;i < _rollingAngle; i+=_carveValue)
            {
                float angleDeg = i;
                float angleRad = angleDeg * Mathf.Deg2Rad; // ƒ‰ƒWƒAƒ“‚É•ÏŠ·
                Vector2 bulletDirection = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
                GameObject obj = Instantiate(_bulletObj,transform.position, Quaternion.identity);
                HomingBullet homing = obj.GetComponent<HomingBullet>();
                homing.DirectionSetting(_playerObject,bulletDirection);
            }
        }
        else
        {
            _initRollingWaitTime += Time.deltaTime;
        }
    }
}
