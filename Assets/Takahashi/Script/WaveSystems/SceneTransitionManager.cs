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

        // 遷移待機中に回復床の新規 Spawn / Despawn(true) が起きないよう停止する
        foreach (FloorOasis oasis in FindObjectsByType<FloorOasis>(FindObjectsInactive.Include))
        {
            if (oasis != null)
            {
                oasis.PrepareForSceneTransition();
            }
        }
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
                if (Time.frameCount < _despawnFrame + 6)
                {
                    // 破棄処理・RPC処理完了から最低6フレーム待機してUnityのDelayedCallManagerを安全に完了させる
                    _timer = _sceneTransitionTime;
                    return;
                }

                LoadNextScene();
                return;
            }
            _sceneLoadTransition = true;
            _despawnFrame = Time.frameCount;
            Debug.Log("[SceneTransition] 遷移開始: 全端末の描画停止と残存ネットワークオブジェクトの同期解除を開始します");

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                // まず全クライアント・ホストの SpriteRenderer / コルーチン / アンロード時破棄処理を一括停止する
                if (_waveUIManager != null && _waveUIManager.IsSpawned)
                {
                    _waveUIManager.PrepareAllClientsForSceneUnloadClientRpc();
                }
                DisableAllSceneRenderersAndBehaviours();

                // 1. 動的スポーンされた残存オブジェクト（プレイヤー・敵・弾・ソウル等）のネットワーク追跡のみ安全に解除
                // ※ netObj.Despawn(false) を使用し、手動 Destroy は行わない（旧シーン破棄時に自動回収）
                // ※ プレイヤー（IsPlayerObject）も DestroyWithScene=true のため、事前に Despawn(false) しておかないと
                //    LoadScene 内部の MoveObjectsToDontDestroyOnLoad() で Despawn(true) (=Destroy) が同一フレーム実行されデッドロックする
                if (NetworkManager.Singleton.SpawnManager != null)
                {
                    var spawnedList = new System.Collections.Generic.List<NetworkObject>(NetworkManager.Singleton.SpawnManager.SpawnedObjectsList);
                    foreach (var netObj in spawnedList)
                    {
                        if (netObj == null || !netObj.IsSpawned) continue;

                        // DontDestroyOnLoad 内のオブジェクト（NetworkManager自身やセッション管理等）は維持
                        if (netObj.gameObject.scene.name == "DontDestroyOnLoad") continue;

                        // シーン配置オブジェクトはDespawnしない
                        if (netObj.IsSceneObject == true) continue;

                        // Destroy(true) ではなく Despawn(false) で同期のみ安全に解除
                        netObj.Despawn(false);
                    }
                }
            }
            else
            {
                DisableAllSceneRenderersAndBehaviours();
            }

            Debug.Log("[SceneTransition] 同期解除完了: 破棄キューフラッシュ後に LoadScene します");
            // 次回の Update で (フレームが進んでいれば) すぐ LoadScene できるようにする
            _timer = _sceneTransitionTime;
            return;
        }

        _timer += Time.unscaledDeltaTime;
    }

    /// <summary>
    /// フェードアウト開始時にプレイヤーの操作・当たり判定とギミック・コインの動作を停止し、
    /// シーン遷移直前の新規生成や点滅コルーチン起動を防ぐ
    /// </summary>
    public static void FreezeGameplayBeforeSceneUnload()
    {
        foreach (FloorOasis oasis in FindObjectsByType<FloorOasis>(FindObjectsInactive.Include))
        {
            if (oasis != null) oasis.PrepareForSceneTransition();
        }

        // プレイヤーの Collider を無効化する前に感圧板と連動床を停止し、OnTriggerExit2D 発火を防ぐ
        DisableBehavioursAndCoroutines<DetectOnPlayer>();
        DisableBehavioursAndCoroutines<ClearFunctionBaseClass>();
        DisableBehavioursAndCoroutines<CandleTakeFire>();
        DisableBehavioursAndCoroutines<MoveCoin>();
        DisableBehavioursAndCoroutines<CoinControl>();

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            if (player == null) continue;

            DisableBehaviourOnObject<PlayerMove>(player);
            DisableBehaviourOnObject<InputPlayerShot>(player);
            DisableBehaviourOnObject<PlayerAiming>(player);
            DisableBehaviourOnObject<InputChangeState>(player);
            DisableBehaviourOnObject<PlayerKnockBack>(player);

            foreach (Collider2D col in player.GetComponentsInChildren<Collider2D>(true))
            {
                if (col != null) col.enabled = false;
            }

            foreach (Rigidbody2D rb in player.GetComponentsInChildren<Rigidbody2D>(true))
            {
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.simulated = false;
                }
            }
        }
    }

    /// <summary>
    /// LoadScene 実行前に、現在シーン内の全 SpriteRenderer・演出・アンロード時 Destroy 処理を一括停止する
    /// </summary>
    public static void DisableAllSceneRenderersAndBehaviours()
    {
        FreezeGameplayBeforeSceneUnload();

        foreach (Wave wave in FindObjectsByType<Wave>(FindObjectsInactive.Include))
        {
            if (wave != null) wave.PrepareForSceneTransition();
        }

        foreach (StageController stage in FindObjectsByType<StageController>(FindObjectsInactive.Include))
        {
            if (stage != null) stage.PrepareForSceneTransition();
        }

        DisableBehavioursAndCoroutines<FollowingCameraToBurrel>();

        foreach (ParticleSystem ps in FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include))
        {
            if (ps == null || ps.gameObject.scene.name == "DontDestroyOnLoad") continue;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        foreach (SpriteRenderer sr in FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include))
        {
            if (sr == null || sr.gameObject.scene.name == "DontDestroyOnLoad") continue;
            sr.enabled = false;
        }

        foreach (LineRenderer lr in FindObjectsByType<LineRenderer>(FindObjectsInactive.Include))
        {
            if (lr == null || lr.gameObject.scene.name == "DontDestroyOnLoad") continue;
            lr.enabled = false;
        }

        foreach (TrailRenderer tr in FindObjectsByType<TrailRenderer>(FindObjectsInactive.Include))
        {
            if (tr == null || tr.gameObject.scene.name == "DontDestroyOnLoad") continue;
            tr.enabled = false;
        }
    }

    private static void DisableBehavioursAndCoroutines<T>() where T : MonoBehaviour
    {
        foreach (T behaviour in FindObjectsByType<T>(FindObjectsInactive.Include))
        {
            if (behaviour == null || behaviour.gameObject.scene.name == "DontDestroyOnLoad") continue;
            behaviour.StopAllCoroutines();
            behaviour.enabled = false;
        }
    }

    private static void DisableBehaviourOnObject<T>(GameObject target) where T : MonoBehaviour
    {
        foreach (T behaviour in target.GetComponentsInChildren<T>(true))
        {
            if (behaviour != null)
            {
                behaviour.StopAllCoroutines();
                behaviour.enabled = false;
            }
        }
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
