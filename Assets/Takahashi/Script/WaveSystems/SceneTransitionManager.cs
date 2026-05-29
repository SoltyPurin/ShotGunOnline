using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ウェーブを終了する際に使用するクラス
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    //シーン遷移に必要なenum
    public enum TransitionState
    {
        Normal,
        FadeOut,
        SceneTransition,
    }

    [SerializeField, Header("取得系")]
    private WaveUIManager _waveUIManager = default;
    [SerializeField]
    private  WaveManager _waveManager = default;
    [SerializeField]
    private BGMControl_Ver2 _bgmControll = default;

    [SerializeField, Header("シーン遷移にかかる時間")]
    private float _transitionTime = 5.0f;
    private float _sceneTransitionTime = 3.0f;

    [SerializeField, Header("フェードアウト音に使うオーディオソース")]
    private AudioSource _audioSource = default;

    [SerializeField, Header("遷移時の音")]
    private AudioClip _audioClip = default;

    private float _score = 0;
    public float GetScore
    {
        get { return _score; }
    }

    [HideInInspector]
    public TransitionState _state = TransitionState.Normal;
    [HideInInspector]
    public bool _transition = false;

    private float _timer = 0;
    private bool _sceneLoadTransition = false;

    private void Start()
    {
        //数値の初期化
        _score = 0;
        _timer = 0;
        _transition = false;
    }

    private void FixedUpdate()
    {
        //シーン遷移をswitchによって管理
        switch (_state)
        {
            case TransitionState.Normal:
                break;

            case TransitionState.FadeOut:
                GameFadeOut();
                break;

            case TransitionState.SceneTransition:
                SceneTransition();
                break;
        }
    }

    public void StartSceneTransition()
    {
        _state = TransitionState.FadeOut;
    }

    /// <summary>
    /// ウェーブ終了からフェードアウトまでの管理用のメソッド
    /// </summary>
    private void GameFadeOut()
    {        
        if(_timer >= _transitionTime)
        {
            _timer = 0;
            _waveUIManager.FadeOutImage();
            _audioSource.PlayOneShot(_audioClip);
            _bgmControll.BGMFade_Out(1, 0);
            _state = TransitionState.SceneTransition;
            return;
        }

        _timer += Time.deltaTime;
    }

    /// <summary>
    /// フェードアウト後からシーン遷移までの管理用メソッド
    /// </summary>
    private void SceneTransition()
    {
        //_timerが_sceneTransitionTime以上になったらシーンを移動する。
        if(_timer >= _sceneTransitionTime)
        {
            _timer = 0;
            SceneManager.LoadScene(_waveManager.NextSceneName);
            return;
        }

        _timer += Time.deltaTime;
    }
}
