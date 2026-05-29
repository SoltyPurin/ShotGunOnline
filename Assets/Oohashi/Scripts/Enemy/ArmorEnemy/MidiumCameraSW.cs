using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 中ボスのウェーブが開始したらカメラワークを変更させるスクリプト
/// </summary>
public class MidiumCameraSW : MonoBehaviour
{
    private FollowingCameraToBurrel _cameraMove = default;

    private readonly string CAMERA_TAGNAME = "MainCamera";

    private void Start()
    {
        _cameraMove = GameObject.FindWithTag(CAMERA_TAGNAME).GetComponent<FollowingCameraToBurrel>();
        _cameraMove.IsMidiumBoss = true;
    }

    public void SwitchOff()
    {
        _cameraMove.IsMidiumBoss = false;
    }

}
