using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoulCountUI : MonoBehaviour
{
    //ソウルを入手したときに+1って表示する、
    //ただし複数のソウルを一定時間内に取得したら+2,+3と数が増える
    //数字が消えるまでにソウルを追加取得したら+の数字が変わる
    [SerializeField, Header("数字が変わるまでの猶予時間兼消えるまでの時間")]
    private float _graceTime = 2f;

    [SerializeField, Header("プラスカウントを表示するテキスト")]
    private Text _countText = default;

    [SerializeField,Header("カウントが飛び出るアニメーター")]
    private Animator _animator = default;

    private float _initGraceTime = 0;

    private int _countValue = 0;

    private bool _isGraceTime = false;

    private readonly string POPUP_TRIGGER = "PopUp";

    public void SoulPlus()
    {
        _countValue++;
        _isGraceTime = true;
        _initGraceTime = _graceTime;
        _animator.SetTrigger(POPUP_TRIGGER);
    }

    private void FixedUpdate()
    {
        if (!_isGraceTime)
        {
            return;
        }
        if(_initGraceTime >= 0)
        {
            _isGraceTime = true;
            _initGraceTime -= Time.fixedDeltaTime;
            _countText.text = "+"+ _countValue.ToString();
        }
        else
        {
            _countValue = 0;
            _countText.text = " ";
            _isGraceTime = false;
            _animator.ResetTrigger(POPUP_TRIGGER);
        }

    }

}
