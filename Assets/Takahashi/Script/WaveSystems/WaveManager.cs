using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// ウェーブを管理するクラス
/// </summary>
public class WaveManager : Unity.Netcode.NetworkBehaviour
{
    #region 【Enum】
    public enum WaveType
    {
        Normal,
        Tutorial,
    }

    public enum TransitionState
    {
        Transition,
        Wave,
    }
    private TransitionState _nowTransitionState = TransitionState.Wave;
    public TransitionState NowTransitionState
    {
        get { return _nowTransitionState; }
    }
    #endregion

    #region 【変数】

    [SerializeField, Header("ウェーブのタイプ")]
    private WaveType _waveType = WaveType.Normal;
    public WaveType GameWaveType
    {
        get { return _waveType; }
    }

    [SerializeField, Header("ウェーブリスト")]
    private List<Wave> _waveList;
    public List<Wave> WaveList
    {
        get { return _waveList; }
    }

    [SerializeField, Min(0), Header("ウェーブの番号")]
    private NetworkVariable<int> _waveIndex = new NetworkVariable<int>(0);
    public int WaveIndex
    {
        get { return _waveIndex.Value; }
    }

    [SerializeField, Header("取得系")]
    private BGMControl_Ver2 _bgmControll = default;
    [SerializeField]
    private WaveUIManager _uiManager = default;
    [SerializeField]
    private ScoreManager _scoreManager = default;
    [SerializeField]
    private SceneTransitionManager _sceneTransitionManager = default;
    [SerializeField]
    private WaveTransition _waveTransition = default;
    [SerializeField]
    GameObject _deathEffects = default;

    [SerializeField, Header("次に遷移するシーン")]
    private string _nextSceneName = default;
    public string NextSceneName
    {
        get { return _nextSceneName; }
    }

    //ウェーブプレハブを格納する用のリスト
    private List<Wave> _cloneList = new List<Wave>();

    //指標
    private int _cloneIndex = 0;

    //ウェーブが終了したか
    private bool _listEnd = false;
    public bool ListEnd
    {
        get { return _listEnd; }
    }
    private bool _startWave = false;

    //ボスウェーブかどうか
    private bool _isBossWave = false;
    public bool IsBossWave
    {
        get { return _isBossWave; }
    }
    #endregion

    #region シングルトン化
    [HideInInspector]
    public static WaveManager _instance;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    private void Start()
    {
        //数値の初期化
        _cloneList.Clear();

        //チュートリアルステージだった場合、ウェーブ遷移の演出をカットするように
        if (_waveType == WaveType.Tutorial)
        {
            _waveTransition.Tutorial = true;
        }

        switch (_waveType)
        {
            case WaveType.Tutorial:
                _bgmControll.BGM_Start(2);
                _bgmControll.BGM_Pause();
                break;

            case WaveType.Normal:
                _bgmControll.BGM_Start(0);
                _bgmControll.BGM_Pause();
                break;
        }
    }

    private void FixedUpdate()
    {
        if (!_startWave)
        {
            return;
        }

        switch (_nowTransitionState)
        {
            case TransitionState.Wave:
                WaveDelete();
                break;

            case TransitionState.Transition:
                EndTransition();
                break;
        }
    }

    /// <summary>
    /// ウェーブを開始する用のメソッド
    /// </summary>
    /// <param name="index">index番目のウェーブを複製しcloneListに格納する。</param>
    public void StartWave(int index)
    {
        if (!IsServer)
        {
            return;
        }
        Wave cloneWave = Instantiate(_waveList[_waveIndex.Value]);
            _cloneList.Add(cloneWave);


        foreach (Wave.ObjData enemyData in cloneWave.ObjList)
        {
            if (enemyData._waveObj != null)
            {
                enemyData._waveObj.gameObject.SetActive(true);
            }
        }
        //_cloneList.Add((Wave)Instantiate(_waveList[index]));
        if (cloneWave.GetComponent<NetworkObject>() != null)
        {
            cloneWave.GetComponent<NetworkObject>().Spawn();
        }

        foreach (Wave.ObjData enemyData in cloneWave.ObjList)
        {
            if (enemyData._waveObj != null)
            {
                if (enemyData._waveObj.CompareTag("Boss"))
                {
                    continue;
                }
                enemyData._waveObj.gameObject.SetActive(false);
            }
        }

        StartWaveClientRpc(index);


    }
    /// <summary>
    /// ネットワーク対応のウェーブスタート処理
    /// </summary>
    /// <param name="index"></param>
    [ClientRpc]
    private void StartWaveClientRpc(int index)
    {
        if (!_startWave)
        {
            _startWave = true;
        }

        if (_waveType != WaveType.Tutorial)
        {
            _uiManager.SetFirstEnemyCounter(_waveList[_waveIndex.Value].ObjList.Count);
            _uiManager.ReMoveRank();
        }
        //ウェーブのオート遷移が有効なら演出を出す。
        if (_waveList[_waveIndex.Value].AutoTransition)
        {
            StartWaveProduction(index);
        }

    }
    /// <summary>
    /// ウェーブ開始時の演出を出す用メソッド
    /// </summary>
    /// <param name="index"></param>
    public void StartWaveProduction(int index)
    {
        int waveCount = index + 1;

        //ボスウェーブだった場合、BOSSと表示しBGMを変える。
        if (_waveList[index]._waveType == Wave.WaveEnemyType.Boss)
        {
            _isBossWave = true;
            _uiManager.TextChangeClientRpc("BOSS", 0);
            _uiManager.HiddenText(0);
            _bgmControll.BGM_Start(1);
        }
        else
        //普通のウェーブだった場合、現在ウェーブ数を表示する。
        {
            //チュートリアルかそうでないかで演出を変更
            switch (_waveType)
            {
                case WaveType.Normal:
                    //初回だった場合BGMを流すよ
                    if (index == 0)
                    {
                        _bgmControll.BGM_UnPause();
                    }
                    //ウェーブ開始時テキストがあるなら、それを表示。無いなら現在のウェーブ数を表示。
                    if (_waveList[index].WaveStartText != "")
                    {
                        _uiManager.TextChangeClientRpc(_waveList[index].WaveStartText, 0);
                    }
                    else
                    {
                        _uiManager.TextChangeClientRpc("Wave " + waveCount, 0);
                    }

                    _uiManager.TextChangeClientRpc(waveCount + " / " + _waveList.Count, 1);
                    _uiManager.HiddenText(0);
                    _uiManager.HiddenText(1);
                    break;

                case WaveType.Tutorial:
                    if (index == 0)
                    {
                        _bgmControll.BGM_UnPause();
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// ウェーブの削除判定、及び次ウェーブ移行判定用のメソッド
    /// </summary>
    private void WaveDelete()
    {
        for (int i = 0; i < _cloneList.Count; i++)
        {
            //ウェーブが終了可能だった場合、ウェーブを削除する。
            if (_cloneList[i] && _cloneList[i].IsDelete())
            {
                Destroy(_cloneList[i].gameObject);
            }
        }

        //ウェーブがリストに存在しない、あるいは全て終わった場合リターン
        if (_waveList.Count <= 0 || IsEnd() || _cloneList.Count <= _cloneIndex)
        {
            return;
        }

        //ウェーブ終了可能なら次のウェーブへ
        if (_cloneList[_cloneIndex].IsEnd())
        {
            GameObject[] eBullets = GameObject.FindGameObjectsWithTag("EnemyBullet");
            foreach (GameObject eBullet in eBullets)
            {
                Destroy(eBullet);
            }
            _uiManager.SetEnemyCounter(0);
            NextWave();
        }
    }

    /// <summary>
    /// ウェーブ移行用メソッド
    /// </summary>
    private void NextWave()
    {
        Debug.Log("ウェーブの数更新");
        //ウェーブがまだ残っているなら次のウェーブへ
        if (_waveIndex.Value < (_waveList.Count - 1))
        {
            //それぞれの指標をプラスする
            _waveIndex.Value++;
            _cloneIndex++;

            _waveTransition.StartTransition(_waveList[_waveIndex.Value]._waveType);
            _nowTransitionState = TransitionState.Transition;
        }
        //残っていないなら終了
        else
        {
            if (_waveType == WaveType.Tutorial)
            {
                TutorialWaveEnd();
            }
            else
            {
                BossWaveEnd();
            }
        }
    }

    /// <summary>
    /// 終了判定用メソッド
    /// </summary>
    /// <returns>_listEndの値をそのまま返す</returns>
    public bool IsEnd()
    {
        return _listEnd;
    }

    /// <summary>
    /// ウェーブ遷移が終了したかどうかを判別するメソッド
    /// </summary>
    private void EndTransition()
    {
        if (_waveTransition.EndTransition())
        {
            StartWave(_waveIndex.Value);
            _nowTransitionState = TransitionState.Wave;
        }
    }

    public void BossWaveEnd()
    {
        _listEnd = true;
        _scoreManager.TextAlpha(true);

        if (IsServer)
        {
            int min = _scoreManager.Minute;
            float sec = _scoreManager.Second;
            int totalDamage = ScoreData._scoreData != null ? ScoreData._scoreData.TotalTakenDamage : 0;
            SyncFinalScoreClientRpc(min, sec,totalDamage);
        }

        _sceneTransitionManager.StartSceneTransition();
    }

    /// <summary>
    /// 全クライアント端末の GetScoreManager にクリアタイムを同期書き込みする RPC
    /// </summary>
    [ClientRpc]
    private void SyncFinalScoreClientRpc(int minute, float seconds,int totalDamageCount)
    {
        if (GetScoreManager._this != null)
        {
            GetScoreManager._this.GetScore(minute, seconds);
        }
        else if (_scoreManager != null)
        {
            _scoreManager.ScoreCalculation();
        }

        if (ScoreData._scoreData != null)
        {
            ScoreData._scoreData.SyncTotalDamageFromHost(totalDamageCount);
        }
    }
    private void TutorialWaveEnd()
    {
        _listEnd = true;
        _scoreManager.TextAlpha(true);

        _sceneTransitionManager.StartSceneTransition();
    }

    /// <summary>
    /// 敵の数が変動した時にカウントを変えるスクリプト
    /// </summary>
    /// <param name="count">敵の数</param>
    [ClientRpc]
    public void SetChangeCountClientRpc(int count)
    {
        _uiManager.SetEnemyCounter(count);
    }

    /// <summary>
    /// サーバーで計算したスコア結果を全プレイヤーのScoreDataに反映させるRPC
    /// </summary>
    [ClientRpc]
    public void SyncScoreClientRpc(int timeRank, int damageCount, float clearTime)
    {
        if (ScoreData._scoreData != null)
        {
            ScoreData._scoreData.SetScore(timeRank, damageCount, clearTime);
        }
    }

    /// <summary>
    /// クライアントが被弾した際、全プレイヤーのタイマーとScoreDataにペナルティを同期する
    /// </summary>
    [Rpc(SendTo.Server, RequireOwnership = false)]
    //[ServerRpc(InvokePermission = RpcInvokePermission.Everyone)]
    public void NotifyTakeDamageServerRpc()
    {
        if (ScoreData._scoreData != null)
        {
            ScoreData._scoreData.AddDamageCount();
        }
        ApplyDamagePenaltyClientRpc();
    }

    /// <summary>
    /// 全プレイヤー端末のタイマーを減算させる ClientRpc
    /// </summary>
    [ClientRpc]
    private void ApplyDamagePenaltyClientRpc()
    {
        EnemyCountAndCreateTime timer = FindAnyObjectByType<EnemyCountAndCreateTime>();
        if (timer != null)
        {
            timer.TakeDamage();
        }
        else
        {
            Debug.Log("タイマーないよ");
        }
    }

}
