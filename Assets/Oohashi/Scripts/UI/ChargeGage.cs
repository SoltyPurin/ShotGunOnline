using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChargeGage : MonoBehaviour
{
    [Header("スライダーを設定")]
    [SerializeField] private Slider _slider = default;

    [Header("スクリプトを設定")]
    [SerializeField] private InputPlayerShot _playerShot = default;

    private void FixedUpdate()
    {
        _slider.value = _playerShot.ChargeValue;
    }
}
