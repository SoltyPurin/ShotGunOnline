using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugSpeed : MonoBehaviour
{
    [SerializeField]
    private float ScaleTime = 0f;

    private bool _isTurbo = false;
    private void Start()
    {
        if (SaveHardOptionSetting._isTurboOn)
        {
            Time.timeScale = ScaleTime;
            _isTurbo = true;
        }
        else
        {
            Time.timeScale = 1;
            _isTurbo = false;
        }
    }

    private void FixedUpdate()
    {
        if (!_isTurbo)
        {
            return;
        }
        if (Time.timeScale <= ScaleTime)
        {
            Time.timeScale = ScaleTime;
        }
    }

}
