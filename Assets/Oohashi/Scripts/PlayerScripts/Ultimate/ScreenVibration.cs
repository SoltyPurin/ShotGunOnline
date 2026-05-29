using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenVibration : MonoBehaviour
{
    //震えさせてもいいかのフラグ
    private bool _canShake = false;
    public bool CanShake
    {
        set { _canShake = value; }
    }
    [SerializeField, Header("カメラを揺らす値")]
    private float _cameraChakeValue = 1.5f;
    private void FixedUpdate()
    {
        if (_canShake)
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
}
