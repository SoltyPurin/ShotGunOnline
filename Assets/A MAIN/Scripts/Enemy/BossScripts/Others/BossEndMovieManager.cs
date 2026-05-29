using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossEndMovieManager : MonoBehaviour
{
    private GameObject _camera = default;
    private FollowingCameraToBurrel _cameraMove = default;

    private void Start()
    {
        _camera = GameObject.FindWithTag("MainCamera");
        _cameraMove = _camera.GetComponent<FollowingCameraToBurrel>();
        _cameraMove.IsEndMovie = true;
    }

}
