using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HardOption : MonoBehaviour
{
    [SerializeField, Header("現在の体力の値を表示するテキスト")]
    private Text _heartValueText = default;
    [SerializeField,Header("現在のターボモードのON,OFFを表示するテキスト")]
    private Text _turboModeText =default;
    private int _heartValue = 5;
    private bool _isTurboOn = false;

    private SaveHardOptionSetting _saveHardOptionSetting;

    private void Start()
    {
        _saveHardOptionSetting = GameObject.Find("SaveHardOptionSetting").GetComponent<SaveHardOptionSetting>();
        _heartValueText.text = _heartValue.ToString();
        _turboModeText.text = _isTurboOn.ToString();
        _saveHardOptionSetting.UpdateHeartValue(_heartValue);
        _saveHardOptionSetting.UpdateTurboMode(_turboModeText);
    }
    public void HeartAddition()
    {
        if(_heartValue < 5)
        {
            _heartValue++;
            _heartValueText.text = _heartValue.ToString();
            _saveHardOptionSetting.UpdateHeartValue(_heartValue);
        }
    }

    public void HeartDecrement()
    {
        if(_heartValue > 1)
        {
            _heartValue--;
            _heartValueText.text = _heartValue.ToString();
            _saveHardOptionSetting.UpdateHeartValue(_heartValue);
        }
    }

    public void SwitchTurboMode()
    {
        _isTurboOn = !_isTurboOn;
        _turboModeText.text = _isTurboOn.ToString();
        _saveHardOptionSetting.UpdateTurboMode(_isTurboOn);
    }
}
