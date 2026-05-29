using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioVolumeChange : MonoBehaviour
{
    [SerializeField, Header("オーディオミキサー")]
    private AudioMixer _audioMixer = default;

    [SerializeField, Header("射撃音のミキサー")]
    private AudioMixer _shotMixer = default;

    [SerializeField, Header("SEのテキスト")]
    private Text _seText = default;

    [SerializeField,Header("BGMのテキスト")]
    private Text _bgmText = default;

    private int _seDisplayValue = 80;

    private int _bgmDisplayValue = 80;

    //音量調整で動かす値、10ずつ動かす
    private int _moveValue = 10;

    private void Start()
    {
        _seText.text = _seDisplayValue.ToString();
        _bgmText.text = _bgmDisplayValue.ToString();

        ApplyBGMVolume(_bgmDisplayValue);
        ApplySEVolume(_seDisplayValue);
    }
    public void SEUp()
    {
            _seDisplayValue = Mathf.Clamp(_seDisplayValue + _moveValue, 0, 100);
        ApplySEVolume(_seDisplayValue);
    }

    public void SEDown()
    {
            _seDisplayValue = Mathf.Clamp(_seDisplayValue - _moveValue, 0, 100);
        ApplySEVolume(_seDisplayValue);
    }

    public void BGMUp()
    {
            _bgmDisplayValue = Mathf.Clamp(_bgmDisplayValue + _moveValue, 0, 100);
        ApplyBGMVolume(_bgmDisplayValue);
    }

    public void BGMDown()
    {
            _bgmDisplayValue = Mathf.Clamp(_bgmDisplayValue - _moveValue, 0, 100);
        ApplyBGMVolume(_bgmDisplayValue);
    }

    private void ApplySEVolume(int value)
    {
        float t = value / 100f;
        float db = -80f + Mathf.Pow(t, 0.5f) * 80f;
        _seText.text = value.ToString();
        _audioMixer.SetFloat("SE", db);
        _shotMixer.SetFloat("SE", db);
    }

    private void ApplyBGMVolume(int value)
    {
        float t = value / 100f;
        float db = -80f + Mathf.Pow(t, 0.5f) * 80f;
        _bgmText.text = value.ToString();
        _audioMixer.SetFloat("BGM", db);
    }

}
