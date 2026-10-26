using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;

public class EnemyCountAndCreateTime : NetworkBehaviour
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

    private GameObject _player;

    #endregion

    #region ゲッター変数
    //そのウェーブの現在の経過時間
    private float _currentWaveTime = 0;
    public float CurrentWaveTime
    {
        get { return _currentWaveTime; }
    }

    #endregion

    #region 定数
    private readonly string PLAYER_TAG = "Player";
    private readonly string RANK_GAGE = "RankGage";
    #endregion


    public void EnemyCount(int count,int difficulty, Wave.WaveEnemyType type)
    {
        //if (IsServer)
        //{
        //    EnemyCountClientRpc(count, difficulty, type);
        //}

        // ネットワーク接続中かつサーバーなら ClientRpc で全端末に同期
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && IsServer)
        {
            EnemyCountClientRpc(count, difficulty, type);
        }
        else
        {
            // オフライン / ソロプレイ時は直接ローカルで実行
            SetupTimerLocal(count, difficulty, type);
        }
    }

    [ClientRpc]
    private void EnemyCountClientRpc(int count, int difficulty, Wave.WaveEnemyType type)
    {
        SetupTimerLocal(count, difficulty, type);
    }

    private void SetupTimerLocal(int count, int difficulty, Wave.WaveEnemyType type)
    {
        _currentWaveTime = 0f;
        _canCountTimer = false;
        _realTimeCircle.StartTimer();
        switch (type)
        {
            case Wave.WaveEnemyType.Normal:
                _baseTime = count * _expectedClearTime;
                _sRankTime = _baseTime * _timeSRankRatio;
                _aRankTime = _baseTime * _timeARankRatio;
                StartCoroutine(TimerFlagStart(false));
                break;

            case Wave.WaveEnemyType.Boss:
                _baseTime = count * _bossExpectedTime;
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

        _realTimeCircle.SetValue(_sRankTime, _aRankTime, difficulty, _damageDecValue);
        _realTimeCircle.StartTimer();
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
        Debug.Log("スコアの被弾処理呼び出し");
        _currentWaveTime += _damageDecValue;

        _realTimeCircle.Trenble();

    }


    private void FixedUpdate()
    {
        //FindPlayerAndTimerStart();
        if (_canCountTimer)
        {
            _currentWaveTime += Time.fixedDeltaTime;
        }
    }

    //private void FindPlayerAndTimerStart()
    //{
    //    _player = GameObject.FindWithTag("Player");
    //    if(_player != null )
    //    {
    //        _realTimeCircle.StartTimer();
    //    }
    //}

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
        if (!IsServer && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            // サーバー以外は計算しない（二重計算防止）
        }
        // --- プレイヤーを安全に自動取得する処理を追加 ---
        if (_playerDamage == null)
        {
            // シーン内のすべてのプレイヤーオブジェクトを探す
            GameObject[] players = GameObject.FindGameObjectsWithTag(PLAYER_TAG);
            foreach (GameObject p in players)
            {
                NetworkObject netObj = p.GetComponent<Unity.Netcode.NetworkObject>();
                // 自分が操作しているローカルプレイヤーのコンポーネントを取得
                if (netObj != null && netObj.IsLocalPlayer)
                {
                    _playerDamage = p.GetComponent<PlayerDamageKnockBack>();
                    break;
                }
            }
        }

        // 保険のガード：もしそれでもプレイヤーやスコアデータが見つからなければエラーを防ぐためにスキップ
        if (_playerDamage == null)
        {
            Debug.LogWarning("現在のシーンに操作可能なローカルプレイヤー(PlayerDamageKnockBack)が見つからないため、スコア計算をスキップしました。");
            _currentWaveTime = 0;
            return;
        }
        if (_scoreData == null)
        {
            Debug.LogError("ScoreData スクリプトへの参照がありません。インスペクターを確認してください。");
            _currentWaveTime = 0;
            return;
        }
        if (_currentWaveTime <= _sRankTime)
        {
            _timeRank = SRANK;
        }
        else if (_currentWaveTime <= _aRankTime)
        {
            _timeRank = ARANK;
        }
        else
        {
            _timeRank = BRANK;
        }

        WaveManager waveManager = FindAnyObjectByType<WaveManager>();
        if (waveManager != null)
        {
            waveManager.SyncScoreClientRpc(_timeRank, _playerDamage.DamageCount, _currentWaveTime);
        }
        //_scoreData.SetScore(_timeRank, _playerDamage.DamageCount, _initWaveTime);
        _playerDamage.ResetDamageCount();


        _currentWaveTime = 0;

    }

}
