using UnityEngine;
using UnityEngine.UI;


public class ScoreManager : MonoBehaviour
{
    [SerializeField, Header("取得系")]
    private WaveManager _waveManager = default;
    [SerializeField]
    private SoulKeep _coinKeep = default;
    [SerializeField]
    private Text _timeText = default;

    [SerializeField, Header("スコアキーパー")]
    private GetScoreManager _getScoreManager = default;

    [SerializeField, Header("テキストの透明度")]
    private float _textAlpha = 0.5f;

    [HideInInspector]
    public float _scoreTimer = 0;

    private const int SCORE_MULTIPLE = 30;
    private int _minute = 0;
    private float _second = 0;
    private float _oldSecond = 0;
    private float _timer = 0;

    private bool _canRunning = true;

    [SerializeField, Header("タイム計測する？")]
    private bool _timerActive = true;

    #region シングルトン化
    [HideInInspector]
    public static ScoreManager _instance;

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
        //初期化
        _scoreTimer = 0;
        _minute = 0;
        _second = 0;
        _oldSecond = 0;

        //タイマーを使わない場合は何も表示しないようにする。
        if (!_timerActive)
        {
            _timeText.text = "";
        }
    }

    private void FixedUpdate()
    {
        if (_timerActive)
        {
            TimerChange();
        }
    }

    /// <summary>
    /// 時間計測&テキスト変更メソッド
    /// </summary>
    private void TimerChange()
    {
        if (!_canRunning)
        {
            return;
        }
        //ウェーブ遷移中でない、もしくはウェーブが終わっていない間は時間を計測しテキストを変更
        if (_waveManager.NowTransitionState == WaveManager.TransitionState.Wave && !_waveManager.ListEnd)
        {
            _scoreTimer += Time.deltaTime;
            _timer += Time.deltaTime;
            _second = _scoreTimer;

            //時間が60秒を超えたら、桁を繰り上がりさせる。
            if (_second >= 60.0f)
            {
                _minute++;
                _second = 0.0f;
                _scoreTimer = 0.0f;
            }

            //1秒単位で数値の変更があった場合のみタイマーの表記を変更する。
            if ((int)_second != (int)_oldSecond)
            {
                _timeText.text = _minute.ToString("00") + ":" + ((int)_second).ToString("00");
            }

            _oldSecond = _second;

        }
    }

    /// <summary>
    /// タイマーテキストの透明度を変えるメソッド
    /// </summary>
    /// <param name="cleanness">trueなら設定した透明度に変更、falseなら透明度をもとに戻す。</param>
    public void TextAlpha(bool cleanness)
    {
        if (!_timerActive)
        {
            return;
        }

        Color color = _timeText.color;

        if (cleanness)
        {
            color.a = _textAlpha;
            _timeText.color = color;
        }
        else
        {
            color.a = 1;
            _timeText.color = color;
        }
    }

    /// <summary>
    /// スコアを返すメソッド
    /// </summary>
    /// <returns>スコアをそのまま返します。現在はタイマーの値をそのまま返しますよ</returns>
    public void ScoreCalculation()
    {
        _getScoreManager.GetScore(_minute,_second);
    }
}
