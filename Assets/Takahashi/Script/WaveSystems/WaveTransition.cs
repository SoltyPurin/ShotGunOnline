using UnityEngine;

public class WaveTransition : MonoBehaviour
{
    #region 【Enum】
    public enum State
    {
        Wait,
        TransitionToNormal,
        TransitionToBoss,
        TransitionToTutorial,
        FadeOut,
    }

    private State _nowState = State.Wait;
    public State NowState
    {
        get { return _nowState; }
    }
    #endregion

    #region 【変数】
    [SerializeField, Header("スクリプト取得系")]
    private WaveUIManager _waveUIManager = default;
    [SerializeField]
    private BGMControl_Ver2 _bgmControll = default;
    [SerializeField]
    private ScoreManager _scoreManager = default;
    [SerializeField]
    private PlayerRespawn _respawn = default;
    [SerializeField]
    private PlayerMove _playerMove = default;

    [SerializeField, Header("アニメーター取得")]
    private Animator _fadeAnimator = default;

    [SerializeField, Header("遷移にかかる時間")]
    private float _transitionInterval = 5.0f;
    [SerializeField, Tooltip("ボスの場合")]
    private float _bossTransitionInterval = 5.0f;
    [SerializeField, Tooltip("チュートリアルの場合")]
    private float _tutorialTransitionInterval = 2.0f;
    [SerializeField, Tooltip("フェードアウトからフェードインまでの時間")]
    private float _fadeOutInterval = 3.0f;

    private ScoreUIManager _scoreUIManager = default;


    //ウェーブ遷移時ステージ外に落ちないようにするためのもの、trueなら落ちない。
    private bool _isWaveChangeMoment = false;
    public bool IsWaveChangeMoment
    {
        get { return _isWaveChangeMoment; }
    }

    //チュートリアルステージかどうかの判別
    private bool _tutorial = false;
    public bool Tutorial
    {
        set { _tutorial = value; }
    }

    //遷移時に使うタイマー
    private float _intervalTimer = 0.0f;

    //リスポーンしたかどうかの判定
    private bool _respawnCheck = false;

    //ウェーブを削除可能かどうかの判定
    private bool _canWaveDestroy = false;
    public bool CanWaveDestroy
    {
        get { return _canWaveDestroy; }
    }
    #endregion

    #region 【定数】
    //各種音量調節用定数
    private const float CUT_ON_TIME = 1.0f;
    private const float CUT_ON_VALUE = 1000f;
    private const float CUT_OFF_TIME = 3.0f;
    private const float FADE_OUT_TIME = 3.0f;
    private const float MAX_VALUE = 22000f;

    private readonly string WAVESCORE = "WaveScore";
    #endregion

    private void Start()
    {
        //タイマーの初期化
        _intervalTimer = 0.0f;

        //各種boolの初期化
        _respawnCheck = false;
        _canWaveDestroy = false;

        GameObject uiObj = GameObject.FindWithTag(WAVESCORE);
        if(uiObj != null)
        {
            _scoreUIManager = FindFirstObjectByType<ScoreUIManager>();
        }
        else
        {
        }
    }

    /// <summary>
    /// 一定秒数ごとに呼ばれる処理
    /// </summary>
    private void FixedUpdate()
    {
        //Switchを使い、各ウェーブの種類に応じて処理を変更
        switch (_nowState)
        {
            //待機状態
            case State.Wait:
                break;

            //通常遷移
            case State.TransitionToNormal:
                NormalTransition();
                break;

            //ボス戦遷移
            case State.TransitionToBoss:
                BossTransition();
                break;

            //チュートリアル遷移
            case State.TransitionToTutorial:
                TutorialTransition();
                break;

            //フェードアウト処理
            case State.FadeOut:
                FadeOutStage();
                break;
        }
    }

    //------------【メソッド】------------

    /// <summary>
    /// ウェーブ遷移開始用メソッド
    /// </summary>
    /// <param name="type">次のウェーブの属性を代入、値によって遷移の仕方が変わる。</param>
    public void StartTransition(Wave.WaveEnemyType type)
    {
        //チュートリアルなら特に何もせずリターン
        if (_tutorial)
        {
            _nowState = State.TransitionToTutorial;
            return;
        }

        //代入された値に応じて、switchで遷移方法を変更
        switch(type)
        {
            //次が通常ウェーブだった場合通常遷移を実行
            case Wave.WaveEnemyType.Normal:
                _nowState = State.TransitionToNormal;
                break;


            //次がボスウェーブだった場合ボス戦遷移を実行
            case Wave.WaveEnemyType.Boss:
                _nowState = State.TransitionToBoss;
                break;

            //次が中ボスウェーブだった場合中ボス戦遷移を実行
            case Wave.WaveEnemyType.MediumBoss:
                _nowState = State.TransitionToNormal;
                break;
        }
    }

    /// <summary>
    /// 通常のウェーブ遷移用メソッド
    /// </summary>
    private void NormalTransition()
    {
        //最初に実行される処理
        if (_intervalTimer <= 0.0f)
        {
            //画面を暗くし、音楽のHighをカットする。
            _fadeAnimator.SetTrigger("In");
            _scoreManager.TextAlpha(true);
            _bgmControll.HiCutOn(CUT_ON_VALUE, CUT_ON_TIME);
        }
        //タイマーが一定の値以上になったら実行
        else if(_intervalTimer >= _transitionInterval)
        {
            //ステートを変更する。
            _intervalTimer = 0;
            _nowState = State.FadeOut;
            return;
        }

        _intervalTimer += Time.deltaTime;
    }

    /// <summary>
    /// ボス戦ウェーブ遷移用メソッド
    /// </summary>
    private void BossTransition()
    {
        //最初に実行される処理
        if (_intervalTimer <= 0.0f)
        {
            //画面を暗くし、音楽をフェードアウトさせる。
            _fadeAnimator.SetTrigger("In");
            _bgmControll.BGMFade_Out(FADE_OUT_TIME, 0);
            _scoreManager.TextAlpha(true);
        }
        //タイマーが一定の値以上になったら実行
        else if (_intervalTimer >= _bossTransitionInterval)
        {
            //音楽をストップさせ、AudioSourceの値を元に戻す。その後ステートを変更
            _bgmControll.BGM_Stop();
            _bgmControll.BGMFade_In(0.01f, _bgmControll._bgmVolume);
            _intervalTimer = 0;
            _nowState = State.FadeOut;
            return;
        }

        _intervalTimer += Time.deltaTime;
    }

    /// <summary>
    /// チュートリアル遷移用メソッド
    /// </summary>
    private void TutorialTransition()
    {
        //タイマーが一定の値以上になったら実行
        if( _intervalTimer >= _tutorialTransitionInterval)
        {
            //リスポーン地点を更新し、ステートを変更
            _respawn.CountUp();
            _intervalTimer = 0;
            _nowState = State.Wait;
            return;
        }

        _intervalTimer += Time.deltaTime;
    }

    /// <summary>
    /// フェードアウトから次のステージへの遷移用メソッド
    /// </summary>
    private void FadeOutStage()
    {
        if(_intervalTimer <= 0.0f)
        {
            _fadeAnimator.SetTrigger("Max");
            _isWaveChangeMoment = true;
            _playerMove.IsFadeing = true;
            _respawn.CountUp();
            _respawnCheck = false;
        }
        else if ( _intervalTimer >= _fadeOutInterval)
        {
            _fadeAnimator.SetTrigger("Out");
            _scoreManager.TextAlpha(false);
            _isWaveChangeMoment = false;
            _playerMove.IsFadeing = false;
            _bgmControll.HiCutOff(MAX_VALUE, CUT_OFF_TIME);
            _intervalTimer = 0;
            _nowState = State.Wait;
            return;
        }
        else if(_intervalTimer >= _fadeOutInterval * 0.5f && !_respawnCheck)
        {
            _respawn.Respawn();
            _scoreUIManager.HiddenText();
            _respawnCheck = true;
        }

        _intervalTimer += Time.deltaTime;
    }

    /// <summary>
    /// ウェーブ遷移が終わったかを返すBoolメソッド
    /// </summary>
    /// <returns>ウェーブ遷移が完了しているならtrue、そうでないならfalse。</returns>
    public bool EndTransition()
    {
        if(_nowState != State.Wait)
        {
            return false;
        }
        else
        {
            return true;
        }
    }
}
