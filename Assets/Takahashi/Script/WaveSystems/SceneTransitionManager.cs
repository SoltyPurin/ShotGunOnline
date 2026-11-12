using Unity.Netcode;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ウェーブ終了時に使用クラス
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    // シーン遷移に必要なenum
    public enum TransitionState
    {
        Normal,
        FadeOut,
        SceneTransition,
    }

    [SerializeField, Header("取得系")]
    private WaveUIManager _waveUIManager = default;
    [SerializeField]
    private WaveManager _waveManager = default;
    [SerializeField]
    private BGMControl_Ver2 _bgmControll = default;

    [SerializeField, Header("シーン遷移にかかる時間")]
    private float _transitionTime = 5.0f;
    private float _sceneTransitionTime = 3.0f;

    [SerializeField, Header("フェードアウトに使うオーディオソース")]
    private AudioSource _audioSource = default;

    [SerializeField, Header("遷移時の音")]
    private AudioClip _audioClip = default;

    private float _score = 0;
    public float GetScore
    {
        get { return _score; }
    }

    [HideInInspector]
    private TransitionState _state = TransitionState.Normal;
    public TransitionState State
    {
        get { return _state; }
        set { _state = value; }
    }

    [HideInInspector]
    public bool _transition = false;

    private float _timer = 0;
    private bool _sceneLoadTransition = false;
    private int _despawnFrame = -1;

    private void Start()
    {
        // 値の初期化
        _score = 0;
        _timer = 0;
        _transition = false;
    }

    private void Update()
    {
        // シーン遷移をswitchによって管理 (Time.timeScale = 0 でも停止しないよう Update + unscaledDeltaTime を使用)
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
        FreezeWatchdogLogger.Checkpoint("SceneTransitionManager.StartSceneTransition");
        Time.timeScale = 1.0f;
        _timer = 0;
        _sceneLoadTransition = false;
        _despawnFrame = -1;
        _state = TransitionState.FadeOut;
    }

    /// <summary>
    /// ウェーブ終了フェードアウトまでの管理用のメソッド
    /// </summary>
    private void GameFadeOut()
    {
        if (Time.timeScale != 1.0f)
        {
            Time.timeScale = 1.0f;
        }

        if (_timer >= _transitionTime)
        {
            _timer = 0;
            FreezeWatchdogLogger.Checkpoint("SceneTransitionManager.GameFadeOut: フェードアウト時間到達");
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                if (NetworkManager.Singleton.IsServer && _waveUIManager != null)
                {
                    _waveUIManager.FadeOutImageClientRpc();
                }
            }
            else if (_waveUIManager != null)
            {
                _waveUIManager.FadeOutImage();
            }

            if (_audioSource != null && _audioClip != null)
            {
                _audioSource.PlayOneShot(_audioClip);
            }
            if (_bgmControll != null)
            {
                _bgmControll.BGMFade_Out(1, 0);
            }
            _state = TransitionState.SceneTransition;
            return;
        }

        _timer += Time.unscaledDeltaTime;
    }

    /// <summary>
    /// フェードアウト完了後のシーン遷移管理メソッド
    /// </summary>
    private void SceneTransition()
    {
        if (Time.timeScale != 1.0f)
        {
            Time.timeScale = 1.0f;
        }

        // _timerが_sceneTransitionTime以上になったらシーン移動。
        if (_timer >= _sceneTransitionTime)
        {
            _timer = 0;
            FreezeWatchdogLogger.Checkpoint("SceneTransitionManager.SceneTransition: シーン遷移時間到達");

            // Despawn 後に1フレーム以上待ってから LoadScene する (同一フレーム実行による競合対策)
            if (_sceneLoadTransition)
            {
                if (Time.frameCount <= _despawnFrame)
                {
                    // 同一フレーム内で複数回呼ばれた場合は次フレームまで待つ
                    _timer = _sceneTransitionTime;
                    return;
                }

                LoadNextScene();
                return;
            }
            _sceneLoadTransition = true;
            _despawnFrame = Time.frameCount;
            Debug.Log("[SceneTransition] 遷移開始: Despawn処理を開始します");

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                // 1. 動的スポーンされた残存オブジェクト（敵・弾など）のみ安全にDespawn
                // ※ プレイヤーキャラクター（IsPlayerObject）やシーン配置オブジェクト（IsSceneObject）は絶対に破壊しない
                if (NetworkManager.Singleton.SpawnManager != null)
                {
                    var spawnedList = new System.Collections.Generic.List<NetworkObject>(NetworkManager.Singleton.SpawnManager.SpawnedObjectsList);
                    foreach (var netObj in spawnedList)
                    {
                        if (netObj == null || !netObj.IsSpawned) continue;

                        // プレイヤー本体は絶対にDespawnしない（切断やNullRefフリーズを防止）
                        if (netObj.IsPlayerObject) continue;

                        // DontDestroyOnLoad 内のオブジェクト（NetworkManager自身やセッション管理等）は維持
                        if (netObj.gameObject.scene.name == "DontDestroyOnLoad") continue;

                        // シーンに最初から配置されていたオブジェクトはDespawn(true)で破壊しない（NGO同期破損を防止）
                        if (netObj.IsSceneObject == true) continue;

                        netObj.Despawn(true);
                    }
                }

                // 2. ネットワーク同期されていないローカルオブジェクト（ローカルコイン等）を安全に破棄
                MoveCoin[] remainingCoins = Object.FindObjectsByType<MoveCoin>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (MoveCoin coin in remainingCoins)
                {
                    if (coin != null)
                    {
                        Destroy(coin.gameObject);
                    }
                }

                CoinControl[] remainingCoinControls = Object.FindObjectsByType<CoinControl>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (CoinControl coinCtrl in remainingCoinControls)
                {
                    if (coinCtrl != null)
                    {
                        Destroy(coinCtrl.gameObject);
                    }
                }
            }

            Debug.Log("[SceneTransition] Despawn処理完了: 次フレームで LoadScene します");
            // 次回の Update で (フレームが進んでいれば) すぐ LoadScene できるようにする
            _timer = _sceneTransitionTime;
            return;
        }

        _timer += Time.unscaledDeltaTime;
    }

    /// <summary>
    /// Despawn 完了後に次のシーンをロードする
    /// </summary>
    private void LoadNextScene()
    {
        FreezeWatchdogLogger.Checkpoint($"SceneTransitionManager.LoadNextScene: 開始 (IsServer: {NetworkManager.Singleton?.IsServer})");
        Time.timeScale = 1.0f;
        // 二重ロード防止のため、以降は Update で処理しない
        _state = TransitionState.Normal;

        string nextScene = _waveManager != null ? _waveManager.NextSceneName : "ResultScene";
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null && NetworkManager.Singleton.IsListening)
        {
            // サーバーのみが NetworkSceneManager.LoadScene を呼び出す（クライアントはサーバー主導で自動同期ロード）
            if (NetworkManager.Singleton.IsServer)
            {
                Debug.Log($"[SceneTransition] NetworkSceneManager.LoadScene({nextScene}) を実行します");
                NetworkManager.Singleton.SceneManager.LoadScene(nextScene, LoadSceneMode.Single);
            }
            else
            {
                Debug.Log("[SceneTransition] クライアントはサーバーからのシーン遷移指示を待機します");
            }
        }
        else
        {
            Debug.Log($"[SceneTransition] SceneManager.LoadScene({nextScene}) を実行します");
            SceneManager.LoadScene(nextScene);
        }
    }
}
