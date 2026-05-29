using UnityEngine;

[System.Serializable]
class TutorialPhases
{
    [SerializeField] private TutorialPhase1 _phase1 = default;
    [SerializeField] private TutorialPhase2 _phase2 = default;
    [SerializeField] private TutorialPhase3 _phase3 = default;
    [SerializeField] private EndMovieManager _endMovieManager = default;

    //参照用プロパティ
    public TutorialPhase1 Phase1 => _phase1;
    public TutorialPhase2 Phase2 => _phase2;
    public TutorialPhase3 Phase3 => _phase3;
    public EndMovieManager EndMovieManager => _endMovieManager;
}

/// <summary>
/// チュートリアルを統括するスクリプト
/// </summary>
public class TutorialManager : MonoBehaviour
{
    //複数の行程を挟むのでenumで管理
    private enum TutorialState
    {
        Phase1,
        Phase2,
        Phase3,
        MovieTransition,
        Movie,
        TutorialEnd,
    }
    private TutorialState _state = TutorialState.Phase1;

    #region 【変数】
    [SerializeField, Header("チュートリアルのフェーズ")]
    private TutorialPhases Phases;

    [SerializeField, Header("終了用デコイ")]
    private GameObject _tutorialDecoy = default;

    [SerializeField, Header("遷移用フェードイメージ")]
    private Animator _fadeAnimator = default;

    [SerializeField, Header("ムービー遷移にかかる時間")]
    private float _transitionTime = 4;

    //各種private変数
    private PlayerRespawn _playerRespawn = default;
    private PlayerStateManager _playerStateManager = default;
    private float _timer = 0.0f;
    #endregion

    private void Start()
    {
        //プレイヤーの各種スクリプトを取得
        _playerRespawn = GameObject.Find("RespawnPoint").GetComponent<PlayerRespawn>();
        _playerStateManager = GameObject.FindWithTag("Player").GetComponent<PlayerStateManager>();

        //タイマーをリセット
        _timer = 0.0f;
    }

    /// <summary>
    /// 毎フレーム実行されるメソッド
    /// </summary>
    void FixedUpdate()
    {
        //フェーズごとに、そのフェーズが終了可能か監視
        switch (_state)
        {
            case TutorialState.Phase1:

                EndPhase1();

                break;

            case TutorialState.Phase2:

                EndPhase2();

                break;

            case TutorialState.Phase3:

                EndPhase3();

                break;

            case TutorialState.MovieTransition:

                MovieTransitionTimer();

                break;

            case TutorialState.Movie:
                break;
        }
    }

    //------------【メソッド】------------

    /// <summary>
    /// フェーズ1終了時に実行するメソッド
    /// </summary>
    private void EndPhase1()
    {
        //終了可能だった場合、以下を実行
        if (Phases.Phase1.EndPhase())
        {
            Phases.Phase2.StartPhase();
            _playerRespawn.CountUp();
            _state = TutorialState.Phase2;
        }
    }

    /// <summary>
    /// フェーズ2終了時に実行するメソッド
    /// </summary>
    private void EndPhase2()
    {
        //終了可能だった場合、以下を実行
        if (Phases.Phase2.EndPhase())
        {
            Phases.Phase2.HideCanvas();
            Phases.Phase3.PhaseStart();
            _playerRespawn.CountUp();
            _state = TutorialState.Phase3;
        }
    }

    /// <summary>
    /// フェーズ3終了時に実行するメソッド
    /// </summary>
    private void EndPhase3()
    {
        //終了可能だった場合、以下を実行
        if (Phases.Phase3.EndPhase())
        {
            _fadeAnimator.SetTrigger("Fade");
            _state = TutorialState.MovieTransition;
        }
    }

    /// <summary>
    /// フェーズ3終了からムービーに移行するまでの時間を管理するメソッド
    /// </summary>
    private void MovieTransitionTimer()
    {
        //タイマーの値が数値を上回ったら以下を実行
        if(_timer >= _transitionTime)
        {
            _timer = 0;
            _playerStateManager.MovieState();
            Phases.EndMovieManager.StartMovie();
            _fadeAnimator.SetTrigger("Hide");
            _state = TutorialState.Movie;
            return;
        }

        //タイマーに加算
        _timer += Time.deltaTime;
    }
}
