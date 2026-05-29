using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public class EnemyCountAndCreateTime : MonoBehaviour
{
    #region Serialize変数群

    [SerializeField, Header("敵一体倒すのに想定される最大時間")]
    private float _expectedClearTime = 10;

    [SerializeField, Header("被弾スコアを出す際に敵の数に掛ける数字")]
    private float _damageMultiplication = 0.3f;

    [SerializeField, Header("ボスを倒すのに想定される時間")]
    private float _bossExpectedTime = 50;

    [SerializeField,Header("中ボスを倒すのに想定される時間")]
    private float _mediumBossExpectedTime = 30;

    [SerializeField, Header("ボス戦の時は想定時間の何割以内にクリアすればSランクか")]
    private float _bossTimeSRankRatio = 0.3f;

    [SerializeField, Header("ボス戦の時は想定時間の何割以内にクリアすればAランクか")]
    private float _bossTimeARankRatio = 0.6f;

    [SerializeField, Header("何割以内にクリアすればSランクか")]
    private float _timeSRankRatio = 0.6f;

    [SerializeField, Header("何割以内にクリアすればAランクか")]
    private float _timeARankRatio = 0.9f;

    [SerializeField, Header("一回の被弾で減らす最低値")]
    private float _oneDamageDecExTime = 3;

    [SerializeField, Header("スコアデータを取得するスクリプト")]
    private ScoreData _scoreData = default;

    [SerializeField, Header("プレイヤーの被弾スクリプト")]
    private PlayerDamageKnockBack _playerDamage = default;

    [SerializeField, Header("サークルを描くスクリプト")]
    private RealTimeScoreCircle _realTimeCircle = default;

    #endregion

    #region 変数


    //一回の被弾でタイムから減らす値
    private float _damageDecValue = 0;

    //今回のウェーブの想定最大時間を代入
    private float _baseTime = 0;

    //今回のウェーブのSランク時間を代入
    private float _sRankTime = 0;

    //今回のウェーブのAランク時間を代入
    private float _aRankTime = 0;

    private bool _canCountTimer = false;

    private const int SRANK = 3;

    private const int ARANK = 2;

    private const int BRANK = 1;

    //時間のランクを入れる
    private int _timeRank = 0;

    #endregion

    #region ゲッター変数
    //そのウェーブの現在の経過時間
    private float _initWaveTime = 0;
    public float InitWaveTime
    {
        get { return _initWaveTime; }
    }

    #endregion

    #region 定数
    private readonly string PLAYER_TAG = "Player";
    private readonly string RANK_GAGE = "RankGage";
    #endregion

    //private void Start()
    //{
    //    _scoreData = ScoreData._scoreData;
    //    if (_scoreData == null)
    //    {
    //        Debug.LogError("スコアデータ取得失敗（ビルド時）！");
    //    }
    //    _realTimeCircle = GameObject.Find(RANK_GAGE).GetComponent<RealTimeScoreCircle>();
    //    _playerDamage = GameObject.FindWithTag(PLAYER_TAG).GetComponent<PlayerDamageKnockBack>();
    //    _scoreData = GetComponent<ScoreData>(); 
    //    if(_realTimeCircle == null)
    //    {
    //        Debug.Log("サークルが取得できてません！");
    //    }
    //    if(_playerDamage == null)
    //    {
    //        Debug.Log("プレイヤーの被弾スクリプトが取得できてません！");
    //    }
    //    if(_scoreData == null)
    //    {
    //        Debug.Log("スコアデータを取得できてません！");
    //    }
    //}

    public void EnemyCount(int count,int difficulty, Wave.WaveEnemyType type)
    {
        switch (type)
        {
            case Wave.WaveEnemyType.Normal:
                _baseTime = count * _expectedClearTime;
                _sRankTime = _baseTime * _timeSRankRatio;
                _aRankTime = _baseTime * _timeARankRatio;
                StartCoroutine(TimerFlagStart(false));
                break;

            case Wave.WaveEnemyType.Boss:
                _baseTime = count *_bossExpectedTime;
                _sRankTime = _baseTime * _bossTimeSRankRatio;
                _aRankTime = _baseTime * _bossTimeARankRatio;
                StartCoroutine(TimerFlagStart(true));
                break;

            case Wave.WaveEnemyType.MediumBoss:
                _baseTime = count * _mediumBossExpectedTime;
                _sRankTime = _baseTime * _bossTimeSRankRatio;
                _aRankTime = _baseTime * _bossTimeARankRatio;
                StartCoroutine(TimerFlagStart(false));
                break;
        }

        _damageDecValue = _oneDamageDecExTime * difficulty;
        

        _realTimeCircle.SetValue(_sRankTime, _aRankTime,difficulty,_damageDecValue);

    }

    private IEnumerator TimerFlagStart(bool isBossWave)
    {
        yield return new WaitForSeconds(1);
        if (!isBossWave)
        {
            _canCountTimer = true;
        }

    }

    public void TakeDamage()
    {
        _initWaveTime += _damageDecValue;

        _realTimeCircle.Trenble();

    }


    private void FixedUpdate()
    {
        if (_canCountTimer)
        {
            _initWaveTime += Time.fixedDeltaTime;
        }
    }

    public void TimerStart()
    {
        _canCountTimer = true;
    }

    public void TimerStop()
    {
        _canCountTimer = false;
    }

    public void Calculator()
    {
        if (_initWaveTime <= _sRankTime)
        {
            _timeRank = SRANK;
        }
        else if (_initWaveTime <= _aRankTime)
        {
            _timeRank = ARANK;
        }
        else
        {
            _timeRank = BRANK;
        }

        _scoreData.SetScore(_timeRank, _playerDamage.DamageCount, _initWaveTime);
        _playerDamage.ResetDamageCount();


        _initWaveTime = 0;

    }

}
