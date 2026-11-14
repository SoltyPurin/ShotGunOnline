using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BossJumpAtackShake : NetworkBehaviour
{
    //震えさせてもいいかのフラグ
    internal bool _beShake = false;

    [SerializeField, Header("カメラを揺らす値")]
    private float _cameraChakeValue = 1.5f;
    private void LateUpdate()
    {
        if (_beShake)
        {
            CameraShake();
        }
    }

    /// <summary>
    /// カメラを震えさせるメソッド。カメラの座標移動で揺らしてる
    /// </summary>
    private void CameraShake()
    {
        float horizontalShakeValue = Random.Range(-_cameraChakeValue, _cameraChakeValue);
        float verticalShakeValue = Random.Range(-_cameraChakeValue, _cameraChakeValue);
        Vector3 initPos = this.transform.position;
        initPos.x += horizontalShakeValue * 0.5f;
        initPos.y += verticalShakeValue * 0.5f;
        this.transform.position = initPos;
    }

    internal void Shake()
    {
        StartCoroutine(ShakeCoroutine());
    }

    internal void ShakeStart()
    {
        _beShake = true;
    }
    internal void ShakeEnd()
    {
        _beShake = false;
    }

    private IEnumerator ShakeCoroutine()
    {
        _beShake = true;
        float shakeTime = 2;
        if (!IsClient)
        {
            shakeTime /= 2;
        }
        yield return new WaitForSeconds(shakeTime);
        _beShake = false;
    }
}
