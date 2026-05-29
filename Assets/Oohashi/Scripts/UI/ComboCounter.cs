using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ComboCounter : MonoBehaviour
{
    [SerializeField, Header("コンボを一時停止するためのウェーブマネージャー")]
    private WaveManager _waveManager = default;

    [SerializeField, Header("コンボが続くまでの時間")]
    private float _comboLimitTime = 5;

    [SerializeField, Header("プレイヤーの移動スクリプト")]
    private PlayerMove _playerMove = default;

    [SerializeField,Header("チャージ時間のスクリプト")]
    private InputPlayerShot _inputPlayerShot = default;

    [SerializeField, Header("スコアデータ")]
    private ScoreData _scoreData = default;

    [SerializeField,Header("敵一体につきご褒美に加算する数字")]
    private float _bonusValue = 0.01f;

    //ご褒美継続時間
    private float _bonusContinuationTime = 0;

    //ご褒美の効果の強さ
    private float _bonusStrengthOfEffect = 0;

    //ご褒美が継続中かどうか
    private bool _isContinuationBonus = false;

    //最初のコンボ継続可能時間を保存
    private float _originLimitTime = default;

    //敵を倒した数
    private int _deathCounter = 0;
    public int DeathCounter
    {
        get { return _deathCounter; }
    }

    //コンボが途切れた時に撃破数を保存する値
    private int _saveDeathCounter = 0;

    //コンボの継続を開始したかどうか
    private bool _isContinuation = false;


    private int _maxCombo = 0;

    private void Start()
    {
        _originLimitTime = _comboLimitTime;
    }

    /// <summary>
    /// 敵が死んだときにコンボ値をプラスする
    /// </summary>
    public void ComboPlus()
    {
        _comboLimitTime = _originLimitTime;
        _deathCounter++;
        _isContinuation = true;
    }

    private void FixedUpdate()
    {
        if (_waveManager.NowTransitionState == WaveManager.TransitionState.Transition)
        {
            return; //ウェーブ移行中は全ての動作を停止するため早期リターン
        }

        if (_isContinuation/* && _waveManager.CanTransition*/)
        {
            _comboLimitTime -= Time.fixedDeltaTime;
            if (_comboLimitTime <=0)
            {
                CalcBonusTimeAndEffect();
                _isContinuation = false;
                _deathCounter = 0;
            }
        }

        if (_isContinuationBonus)
        {
            _bonusContinuationTime -= Time.fixedDeltaTime;
            if(_bonusContinuationTime <= 0)
            {
                _playerMove.ResetMoveSpeed();
                _inputPlayerShot.ResetBonusMultiplier();
                _isContinuationBonus = false;
                _deathCounter = 0;
            }
        }
    }
    /// <summary>
    /// ボーナス時間と効果量を計算する
    /// </summary>
    private void CalcBonusTimeAndEffect()
    {
        if(_deathCounter < _saveDeathCounter)
        {
            return;//前回の討伐数を上回ってなければリターン
        }
        if(_maxCombo < _deathCounter)
        {
            _maxCombo = _deathCounter;
            _scoreData.SaveMaxCombo(_maxCombo);
        }
        _playerMove.ResetMoveSpeed();
        _inputPlayerShot.ResetBonusMultiplier();
        _isContinuationBonus = true;
        _saveDeathCounter = _deathCounter;
        _bonusContinuationTime = _deathCounter * 2;
        //+1しないと小数点以下になって減速するため足す
        _bonusStrengthOfEffect = 1+_deathCounter * _bonusValue;
        _playerMove.BonusSet(_bonusStrengthOfEffect );
        _inputPlayerShot.BonusSet(_bonusStrengthOfEffect );
    }


}
