using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class StopVibe : MonoBehaviour
{
    //パッドを保存する変数
    private Gamepad _gamepad = default;

    private void Awake()
    {
        if (Gamepad.current == null)
        {
            //パッドが接続されてなかったらリターン
            return;
        }
        _gamepad = Gamepad.current;
        _gamepad.SetMotorSpeeds(0, 0);

    }
}
