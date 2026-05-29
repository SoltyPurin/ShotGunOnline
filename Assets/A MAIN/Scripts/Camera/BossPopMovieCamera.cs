using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossPopMovieCamera : MonoBehaviour
{
    internal bool _movieSw = false;
    private float _cameraSize = 7.0f;

    private void FixedUpdate()
    {
        if (_movieSw)
        {
            BossPopCamera();
        }
    }
    private void BossPopCamera()
    {
        Camera camera = GetComponent<Camera>();
        GameObject boss = GameObject.FindGameObjectWithTag("Boss");
        camera.orthographicSize = _cameraSize;
        transform.position = boss.transform.position;
        _movieSw = false;
    }
}
