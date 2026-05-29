using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class KonamiKommando : MonoBehaviour
{
    //体力3の敵の数3倍移動速度3倍でMasterMモード
    private bool _isTyping;
    [SerializeField, Header("入力猶予時間")]
    private float _graceTime = 3;
    [SerializeField, Header("シーン遷移のスクリプト")]
    private MoveScene _moveScene = default;
    private float _inputGraceTimer;
    private String pass;
    private String command;

    private enum StageState
    {
        Hard,
        Normal,
    }

    private StageState _state = StageState.Normal;

    private GetAxisDown _axisDown = default;

    [SerializeField,Header("とりあえずリセット")]
    private GameReseter _reset = default;

    [SerializeField, Header("ハードモードの画像")]
    private GameObject _hardObjcet = default;

    [SerializeField, Header("ハードステージに行く音")]
    private AudioClip _hardStageSE = default;

    [SerializeField,Header("通常時に戻る音")]
    private AudioClip _returnNormalSE =default;

    private EXOptionViewer _exOptionObj = default;

    private AudioSource _audio = default;

    private Animator _animator = default;

    private bool _canunlockHard= true;


    void Start()
    {
        _axisDown = GetComponent<GetAxisDown>();
        _audio = GetComponent<AudioSource>();
        _animator = GetComponent<Animator>();
        _exOptionObj = GetComponent<EXOptionViewer>();
        _hardObjcet.SetActive(false);
        pass = "UUDDLRLRXY";
    }

    void Update()
    {
        _inputGraceTimer -= Time.deltaTime;
        if (_inputGraceTimer <= 0)
        {
            _isTyping = false;
            command = "";
        }

        if (_axisDown.UpDown)
        {
            _inputGraceTimer = _graceTime;
            _isTyping = true;
            command = "U";
        }

        if (_isTyping)
        {
            if (_axisDown.UpDown)
            {
                command = command + "U";
            }

            if (_axisDown.DownDown)
            {
                command = command + "D";
            }

            if (_axisDown.RightDown)
            {
                command = command + "R";
            }

            if (_axisDown.LeftDown)
            {
                command = command + "L";
            }

            if (Input.GetButtonDown("X"))
            {
                command = command + "X";
            }

            if (Input.GetButtonDown("Y"))
            {
                command = command + "Y";
            }
        }

        if (command == "UUDDLRLRXY" && _canunlockHard)
        {
            _canunlockHard = false;
            _animator.SetTrigger("Flash");
            _audio.PlayOneShot(_hardStageSE);
            _exOptionObj.OnlineEXOption();
            _moveScene.KonamiCommand();
            command = "";
        }
        else if(command == "UUDDLRLRXY" && !_canunlockHard)
        {
            _canunlockHard = true;
            _animator.SetTrigger("Flash");
            _audio.PlayOneShot(_returnNormalSE);
            _exOptionObj.OFFLineEXOption();
            _moveScene.ReturnKonami();
            command = "";
        }
    }

    public void ShowTheHard()
    {
        _hardObjcet.SetActive(!_hardObjcet.activeInHierarchy);
    }

}
