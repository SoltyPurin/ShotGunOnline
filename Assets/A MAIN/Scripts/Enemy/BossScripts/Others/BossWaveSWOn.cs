using UnityEngine;

/// <summary>
/// ボスのWaveが始まったらカメラに始まったことを知らせるメソッド
/// </summary>
public class BossWaveSWOn : MonoBehaviour
{
    #region[変数名]
    private GameObject _camera = default;
    private FollowingCameraToBurrel _cameraMove = default;

    #endregion
    void Start()
    {
        _camera = GameObject.FindGameObjectWithTag("MainCamera");
        _cameraMove = _camera.GetComponent<FollowingCameraToBurrel>();
        _cameraMove.IsBossWave = true;
    }
}
