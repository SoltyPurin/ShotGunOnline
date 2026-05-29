using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputPause : MonoBehaviour
{
    private readonly string PAUSE_BUTTON = "Pause";
    private bool _isPauseing = false;
    public bool IsPauseing
    {
        get { return _isPauseing; }
    }

    private bool _canChangeTimeScale = true;
    public bool CanChangeTimeScale
    {
        set { _canChangeTimeScale = value; }
    }

    private void Update()
    {
        if (Input.GetButtonDown(PAUSE_BUTTON))
        {
            switch(_isPauseing)
            {
                case true:
                    Resume();
                    break;

                case false:
                    Stop();
                    break;
            }
        }
    }

    public void Resume()
    {
        _isPauseing = false;

        if(_canChangeTimeScale)
        {
            Time.timeScale = 1;
        }
    }

    private void Stop()
    {
        _isPauseing = true;

        if (_canChangeTimeScale)
        {
            Time.timeScale = 0;
        }
    }
}
