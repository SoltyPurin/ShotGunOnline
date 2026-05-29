using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
class Phase3UIs
{
    #region [変数]
    [Header("キャンバス")]
    [SerializeField, Tooltip("メインキャンバス")]
    private GameObject _mainCanvas = default;
    [SerializeField, Tooltip("キャンバス")]
    private GameObject _phase3Canvas = default;
    [SerializeField, Tooltip("メインキャンバスのアニメーター")]
    private Animator _canvasAnimator = default;

    [Header("UI")]
    [SerializeField, Tooltip("次へのボックス")]
    private GameObject _nextBox = default;
    [SerializeField, Tooltip("条件によって出現するボックス")]
    private GameObject _prevBox = default;
    [SerializeField, Tooltip("テキスト")]
    private GameObject _text = default;
    [SerializeField, Tooltip("次のテキスト")]
    private GameObject _nextText = default;
    #endregion

    #region [プロパティ]
    public GameObject MainCanvas => _mainCanvas;
    public GameObject Phase3Canvas => _phase3Canvas;
    public Animator CanvasAnimator => _canvasAnimator;
    public GameObject NextBox => _nextBox;
    public GameObject PreviousBox => _prevBox;
    public GameObject Text => _text;
    public GameObject NextText => _nextText;
    #endregion
}

public class TutorialPhase3 : TutorialCountUpper
{
    private enum Phase3State
    {
        Wait,
        WaitForSoulMax,
        First,
        Second,
        Third,
        Active,
    }
    private Phase3State _state = Phase3State.Wait;

    #region 【変数】
    [SerializeField, Header("UI（キャンバスなど）")]
    private Phase3UIs UIs;

    [SerializeField, Header("フェーズ3のデコイ")]
    private GameObject _phase3Decoy = default;

    [SerializeField, Header("待つ時間")]
    private float _waitTime = 2;

    [SerializeField, Header("敵の数")]
    private int _enemyCount = 0;

    //各種private変数
    private bool _isNext = false;
    private bool _prevText = false;
    private bool _endTutorial = false;
    private float _timer = 0.0f;
    private int _counter = 0;
    private Gamepad _gamePad;
    private PlayerStateManager _playerStateManager = default;
    private SoulKeep _soulKeep = default;
    private InputPause _inputPause = default;
    private readonly string USE_ULTIMATE_BUTTON = "Ultimate";
    private readonly float LEFT_TRIGGER_INPUT_VALUE = 0.9f;
    #endregion

    private void Start()
    {
        //コントローラーがない場合はリターン
        if (Gamepad.current == null)
        {
            return;
        }

        _gamePad = Gamepad.current;

        GameObject player = GameObject.FindWithTag("Player");

        //プレイヤーから各種スクリプトを取得
        _soulKeep = player.GetComponent<SoulKeep>();
        _playerStateManager = player.GetComponent<PlayerStateManager>();
        _inputPause = GameObject.Find("PauseVision").GetComponent<InputPause>();

        //各種変数を初期化
        _timer = 0;
        _isNext = false;
        _endTutorial = false;
        _counter = _enemyCount;
    }

    /// <summary>
    /// 毎フレーム実行されるメソッド
    /// ※Updateなのは途中でTimeScaleを0にするため
    /// </summary>
    private void Update()
    {
        if(_gamePad == null)
        {
            _gamePad = Gamepad.current;
            return;
        }

        switch (_state)
        {
            case Phase3State.WaitForSoulMax:

                SoulKeeper();

                if (_soulKeep.VStock >= 1 && _playerStateManager.PlayerState != PlayerState.Fall)
                {
                    _state = Phase3State.First;

                    UIs.Phase3Canvas.SetActive(true);
                    _inputPause.CanChangeTimeScale = false;

                    Time.timeScale = 0;
                }

                break;

            case Phase3State.First:

                //チュートリアル表示から次に移行可能までの時間待つ
                BoxActiveTimer();

                //次に移行可能かつ、Ａボタンが押された時、次のチュートリアルに移行する。
                if (_isNext 
                        && PushtoLorA()
                        && !_inputPause.IsPauseing)
                {
                    if (!_prevText)
                    {
                        UIs.NextBox.SetActive(false);
                    }
                    else
                    {
                        UIs.PreviousBox.SetActive(false);
                    }

                    UIs.Text.SetActive(false);
                    UIs.NextText.SetActive(true);

                    _isNext = false;

                    if (_playerStateManager.PlayerState != PlayerState.Ultimate)
                    {
                        _prevText = true;
                    }

                    _state = Phase3State.Second;
                }

                break;

            case Phase3State.Second:

                //チュートリアル表示から次に移行可能までの時間待つ
                BoxActiveTimer();

                //次に移行可能かつ、Ａボタンが押された時、タイムスケールを元に戻し移動可能にする。
                if (_isNext 
                        && PushtoLorA()
                        && !_inputPause.IsPauseing)
                {
                    UIs.Phase3Canvas.SetActive(false);
                    UIs.MainCanvas.SetActive(true);

                    Time.timeScale = 1;
                    _inputPause.CanChangeTimeScale = true;

                    _state = Phase3State.Active;
                }

                break;

            //Lボタンを押していない者用
            case Phase3State.Third:

                break;

            case Phase3State.Active:

                SoulKeeper();

                break;
        }
    }

    //------------【メソッド】------------

    /// <summary>
    /// フェーズを開始するためのメソッド
    /// </summary>
    public void PhaseStart()
    {
        _state = Phase3State.WaitForSoulMax;
    }

    /// <summary>
    /// チュートリアルテキスト表示から次に移行するまでの待機時間を管理するメソッド
    /// </summary>
    private void BoxActiveTimer()
    {
        //もし指定の秒数経ったら、次に移行可能にしてリターンする。
        if (_timer >= _waitTime)
        {
            _isNext = true;

            if (!_prevText)
            {
                UIs.NextBox.SetActive(true);
            }
            else
            {
                UIs.PreviousBox.SetActive(true);
            }

            _timer = 0;
            return;
        }

        //タイマーにタイムスケールの影響を受けないDeltaTimeを加算
        _timer += Time.unscaledDeltaTime;
    }

    private void SoulKeeper()
    {
        if (_soulKeep.VStock <= 1 && !_endTutorial)
        {
            _soulKeep.AdditionCoin();
        }
    }

    /// <summary>
    /// フェーズ内の全ての敵を倒させるチュートリアル用メソッド
    /// 敵側から呼び出す。
    /// </summary>
    public override void CountDown()
    {
        //カウンターの値をマイナスする。
        _counter--;

        //もし敵が0以下になったら、次に移行可能にする。
        if (_counter <= 0)
        {
            _endTutorial = true;
            Destroy(_phase3Decoy);
        }
    }

    private bool PushtoLorA()
    {
        float leftTriggerValue = Gamepad.current.leftTrigger.ReadValue();
        bool isLeftTriggerInput = leftTriggerValue >= LEFT_TRIGGER_INPUT_VALUE;

        if (!_prevText && Gamepad.current.aButton.wasPressedThisFrame)
        {
            return true;
        }
        else if (_prevText && Input.GetButtonDown(USE_ULTIMATE_BUTTON) || isLeftTriggerInput)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// フェーズを終了させられるかどうかを管理するメソッド
    /// </summary>
    /// <returns>フェーズ終了可能ならtrue</returns>
    public bool EndPhase()
    {
        if (_phase3Decoy != null)
        {
            return false;
        }

        UIs.CanvasAnimator.SetTrigger("Hide");
        return true;
    }
}
