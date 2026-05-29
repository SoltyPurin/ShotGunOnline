using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ウェーブを管理するクラス
/// </summary>
public class WaveManager : MonoBehaviour
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
    private int _waveIndex = 0;
    public int WaveIndex
    {
        get { return _waveIndex; }
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
        if (!_startWave)
        {
            _startWave = true;
        }

        if(_waveType != WaveType.Tutorial)
        {
            _uiManager.SetFirstEnemyCounter(_waveList[_waveIndex].ObjList.Count);
            _uiManager.ReMoveRank();
        }

        //Listにウェーブプレハブをコピーし追加
        _cloneList.Add((Wave)Instantiate(_waveList[index]));

        //ウェーブのオート遷移が有効なら演出を出す。
        if (_waveList[_waveIndex].AutoTransition)
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
            _uiManager.TextChange("BOSS", 0);
            _uiManager.HiddenText(0);
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
                        _uiManager.TextChange(_waveList[index].WaveStartText, 0);
                    }
                    else
                    {
                        _uiManager.TextChange("Wave " + waveCount, 0);
                    }

                    _uiManager.TextChange(waveCount + " / " + _waveList.Count, 1);
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
        //ウェーブがまだ残っているなら次のウェーブへ
        if (_waveIndex < (_waveList.Count - 1))
        {
            //それぞれの指標をプラスする
            _waveIndex++;
            _cloneIndex++;

            _waveTransition.StartTransition(_waveList[_waveIndex]._waveType);
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
            StartWave(_waveIndex);
            _nowTransitionState = TransitionState.Wave;
        }
    }

    public void BossWaveEnd()
    {
        _listEnd = true;
        _scoreManager.TextAlpha(true);

        //if (_waveType != WaveType.Tutorial)
        //{
        //    _scoreManager.ScoreCalculation();
        //}
        _scoreManager.ScoreCalculation();

        _sceneTransitionManager.StartSceneTransition();
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
    public void SetChangeCount(int count)
    {
        _uiManager.SetEnemyCounter(count);
    }
}
