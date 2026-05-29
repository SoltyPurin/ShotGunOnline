using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
class Phase2UIs
{
    #region 【変数】
    [Header("キャンバス")]
    [SerializeField, Tooltip("下側に表示するチュートリアルキャンバス")]
    private GameObject _phase2Canvas = default;
    [SerializeField, Tooltip("時間停止させてから表示させるキャンバス")]
    private GameObject _noticeCanvas = default;

    [Header("UI")]
    [SerializeField, Tooltip("最初のテキスト")]
    private GameObject _textBox1 = default;
    [SerializeField, Tooltip("二番目のテキスト")]
    private GameObject _textBox2 = default;
    [SerializeField, Tooltip("次に移行可能であることを示すUI")]
    private GameObject _nextUI = default;
    #endregion

    //プロパティ
    public GameObject Phase2Canvas => _phase2Canvas;
    public GameObject NoticeCanvas => _noticeCanvas;
    public GameObject TextBox1 => _textBox1;
    public GameObject TextBox2 => _textBox2;
    public GameObject NextUI => _nextUI;
}

/// <summary>
/// 二番目のチュートリアルを管理するスクリプト
/// </summary>
public class TutorialPhase2 : TutorialCountUpper
{
    //複数の行程を挟むチュートリアルなのでenumを使用
    private enum Phase3State
    {
        Wait,
        WaitForPlayer,
        First,
        Second,
        Active,
    }
    private Phase3State _state = Phase3State.Wait;

    #region 【変数】
    [SerializeField, Header("フェーズ２のＵＩ")]
    private Phase2UIs _phase2UIs = default;

    [SerializeField, Header("フェーズ終了用デコイ")]
    private GameObject _phase2Decoy = default;

    [Header("設置物")]
    [SerializeField, Tooltip("矢印")]
    private GameObject _phase2Arrow = default;
    [SerializeField, Tooltip("壁")]
    private GameObject _wall = default;

    [SerializeField, Header("敵の数")]
    private int _enemyCount = 0;

    [SerializeField, Header("待つ時間")]
    private float _waitTime = 2;

    //各種private変数
    private float _timer = 0.0f;
    private int _counter = 0;
    private bool _canNext = false;
    private bool _waitPlayer = false;
    private bool _waitCoroutine = false;
    private Gamepad _gamePad = default;
    private InputPause _inputPause = default;
    private PlayerStateManager _playerStateManager = default;
    #endregion

    /// <summary>
    /// 生成時一度だけ実行されるメソッド
    /// </summary>
    private void Start()
    {
        //コントローラーがない場合はリターン
        if (Gamepad.current == null)
        {
            return;
        }

        _gamePad = Gamepad.current;

        //変数を初期化
        _timer = 0;
        _counter = _enemyCount;

        //ポーズ画面移行用スクリプト取得
        _inputPause = GameObject.Find("PauseVision").GetComponent<InputPause>();
        _playerStateManager = GameObject.FindWithTag("Player").GetComponent<PlayerStateManager>();
    }

    /// <summary>
    /// 毎フレーム実行されるメソッド
    /// ※Updateなのは途中でTimeScaleを0にするため
    /// </summary>
    private void Update()
    {
        switch (_state)
        {
            case Phase3State.WaitForPlayer:

                if(_playerStateManager.PlayerState == PlayerState.Fall)
                {
                    _waitPlayer = true;
                    return;
                }

                //プレイヤーの状態がFall以外なら、壁を破壊し、矢印を表示、そしてチュートリアルを表示する。
                if (_waitPlayer && !_waitCoroutine)
                {
                    StartCoroutine("WaitPlayer");
                }
                else if (!_waitPlayer)
                {
                    StartTutorialText();
                }

                break;

            case Phase3State.First:

                //チュートリアル表示から次に移行可能までの時間待つ
                BoxActiveTimer();

                //次に移行可能かつ、Ａボタンが押された時、次のチュートリアルに移行する。
                if (_canNext 
                        && Gamepad.current.aButton.wasPressedThisFrame
                        && !_inputPause.IsPauseing)
                {
                    _canNext = false;
                    _phase2UIs.TextBox1.SetActive(false);
                    _phase2UIs.TextBox2.SetActive(true);
                    _phase2UIs.NextUI.SetActive(false);
                    _state = Phase3State.Second;
                }

                break;

            case Phase3State.Second:

                //チュートリアル表示から次に移行可能までの時間待つ
                BoxActiveTimer();

                //次に移行可能かつ、Ａボタンが押された時、タイムスケールを元に戻し移動可能にする。
                if (_canNext 
                        && Gamepad.current.aButton.wasPressedThisFrame
                        && !_inputPause.IsPauseing)
                {
                    _phase2UIs.Phase2Canvas.SetActive(true);
                    _phase2UIs.NoticeCanvas.SetActive(false);
                    _canNext = false;
                    Time.timeScale = 1;
                    _inputPause.CanChangeTimeScale = true;
                    _state = Phase3State.Active;
                }

                break;
        }
    }

    //------------【メソッド】------------

    /// <summary>
    /// フェーズを開始するためのメソッド
    /// </summary>
    public void StartPhase()
    {
        _phase2UIs.Phase2Canvas.SetActive(true);
    }

    /// <summary>
    /// フェーズ内の全ての敵を倒させるチュートリアル用メソッド
    /// 敵側から呼び出す。
    /// </summary>
    public override void CountDown()
    {
        //カウンターの値をマイナスする。
        _counter--;

        //もし敵が0以下になったら以下を実行
        if(_counter <= 0)
        {
            //チュートリアルのステートを変更
            _state = Phase3State.WaitForPlayer;
        }
    }

    /// <summary>
    /// チュートリアルテキスト表示から次に移行するまでの待機時間を管理するメソッド
    /// </summary>
    private void BoxActiveTimer()
    {
        //もし指定の秒数経ったら、次に移行可能にしてリターンする。
        if (_timer >= _waitTime)
        {
            _canNext = true;
            _phase2UIs.NextUI.SetActive(true);
            _timer = 0;
            return;
        }

        //タイマーにタイムスケールの影響を受けないDeltaTimeを加算
        _timer += Time.unscaledDeltaTime;
    }

    /// <summary>
    /// キャンバスを非表示にするためのメソッド
    /// </summary>
    public void HideCanvas()
    {
        _phase2UIs.Phase2Canvas.SetActive(false);
    }

    /// <summary>
    /// プレイヤーが落ちた後、暗転が解除されるのを待つ用コルーチン
    /// </summary>
    private IEnumerator WaitPlayer()
    {
        _waitCoroutine = true;

        yield return new WaitForSeconds(1);

        StartTutorialText();
    }

    private void StartTutorialText()
    {
        //各種UIと設置物の配置を変更
        _wall.SetActive(false);
        _phase2Arrow.SetActive(true);
        _phase2UIs.Phase2Canvas.SetActive(false);
        _phase2UIs.NoticeCanvas.SetActive(true);

        //チュートリアル中ポーズ画面を押しても時間が変化しないようにする。
        _inputPause.CanChangeTimeScale = false;

        //タイムスケールを0にする。
        Time.timeScale = 0;

        _state = Phase3State.First;
    }

    /// <summary>
    /// フェーズを終了させられるかどうかを管理するメソッド
    /// </summary>
    /// <returns>フェーズ終了可能ならtrue</returns>
    public bool EndPhase()
    {
        if (_phase2Decoy != null)
        {
            return false;
        }

        return true;
    }
}
