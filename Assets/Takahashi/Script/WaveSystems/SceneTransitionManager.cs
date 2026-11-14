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

            // Despawn 後に数フレーム待機してから LoadScene する (破棄キューのフラッシュと非同期シーンロードの競合対策)
            if (_sceneLoadTransition)
            {
                if (Time.frameCount < _despawnFrame + 3)
                {
                    // 破棄処理完了から最低3フレーム待機してUnityのDelayedCallManagerを安全に完了させる
                    _timer = _sceneTransitionTime;
                    return;
                }

                LoadNextScene();
                return;
            }
            _sceneLoadTransition = true;
            _despawnFrame = Time.frameCount;
            Debug.Log("[SceneTransition] 遷移開始: 残存ネットワークオブジェクトの同期解除を開始します");

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                // 1. 動的スポーンされた残存オブジェクト（敵・弾など）のネットワーク追跡のみ安全に解除
                // ※ netObj.Despawn(false) を使用し、手動 Destroy は行わない（旧シーン破棄時に自動回収）
                // ※ 手動 Destroy を行うと、Unityの DelayedCallManager (SpriteRenderer::MainThreadCleanup) と
                //    非同期シーンロードの PersistentManager::Lock が衝突してデッドロックするため
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

                        // シーン配置オブジェクトはDespawnしない
                        if (netObj.IsSceneObject == true) continue;

                        // SpriteRenderer があれば事前に非アクティブ化してアセットロック競合を防止
                        SpriteRenderer[] renderers = netObj.GetComponentsInChildren<SpriteRenderer>(true);
                        foreach (var sr in renderers)
                        {
                            if (sr != null) sr.enabled = false;
                        }

                        // Destroy(true) ではなく Despawn(false) で同期のみ安全に解除
                        netObj.Despawn(false);
                    }
                }

                // 2. ローカルのコインや演出オブジェクトも手動 Destroy せず、非表示化のみ行う
                // (GameObject 自体は LoadSceneMode.Single による旧シーンアンロードで自動的かつ安全に一括破棄される)
                MoveCoin[] remainingCoins = Object.FindObjectsByType<MoveCoin>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (MoveCoin coin in remainingCoins)
                {
                    if (coin != null)
                    {
                        SpriteRenderer sr = coin.GetComponent<SpriteRenderer>();
                        if (sr != null) sr.enabled = false;
                    }
                }
            }

            Debug.Log("[SceneTransition] 同期解除完了: 破棄キューフラッシュ後に LoadScene します");
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
