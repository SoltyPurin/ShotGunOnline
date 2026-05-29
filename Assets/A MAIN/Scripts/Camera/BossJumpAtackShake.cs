using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossJumpAtackShake : MonoBehaviour
{
    //震えさせてもいいかのフラグ
    internal bool _beShake = false;

    [SerializeField, Header("カメラを揺らす値")]
    private float _cameraChakeValue = 1.5f;
    private void FixedUpdate()
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
        initPos.x += horizontalShakeValue;
        initPos.y += verticalShakeValue;
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
        yield return new WaitForSeconds(2.0f);
        _beShake = false;
    }
}
