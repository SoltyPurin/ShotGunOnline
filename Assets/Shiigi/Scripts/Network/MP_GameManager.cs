using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// ゲーム全体の進行管理（状態遷移、プレイヤー管理、UI、ウェーブ開始）を行うクラス
/// </summary>
public class MP_GameManager : MonoBehaviour
{
    public static MP_GameManager Instance { get; private set; }

    public enum GameState
    {
        WaitingForPlayers, // 全員のロード待機中
        OpeningMovie,      // 開始ムービー再生中
        Playing,           // ゲームプレイ中
        GameOver,          // ゲームオーバー
        GameClear          // ゲームクリア
    }

    [Header("ゲーム状態")]
    [SerializeField] private GameState _currentState = GameState.WaitingForPlayers;
    public GameState CurrentState => _currentState;

    [Header("UI 参照")]
    [Tooltip("ゲームプレイ中のHUD Canvas")]
    [SerializeField] private GameObject _hudCanvas;
    [Tooltip("開始前のNow Loading画面Canvas")]
    [SerializeField] private GameObject _starterCanvas;
    public GameObject StarterCanvas => _starterCanvas;

    [Header("進行 参照")]
    [SerializeField] private WaveManager _waveManager;

    [Header("開始ムービー")]
    [Tooltip("ゲーム開始時のPlayableDirector。OnGameStartで再生する")]
    [SerializeField] private PlayableDirector[] _startDirectors;

    [Header("プレイヤー生成")]
    [Tooltip("マルチプレイ用のPlayerプレハブ（NetWork Player Variantなど）")]
    [SerializeField] private GameObject _playerPrefab;

    // ネットワーク上のプレイヤーリスト
    private readonly List<NetworkObject> _players = new();
    public IReadOnlyList<NetworkObject> Players => _players;

    private PlayerStateManager _localPlayerState;

    public event Action<GameState, GameState> StateChanged;

    private void Awake()
    {
        // バックグラウンド時（非フォーカス・最小化など）でも処理・通信が停止しないように設定
        Application.runInBackground = true;

        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 既存の GameStarter がシーンに残っている場合、勝手にタイムラインで開始してしまうため無効化・破棄する
        var oldStarter = FindAnyObjectByType<GameStarter>();
        if (oldStarter != null && oldStarter.gameObject != this.gameObject)
        {
            Debug.LogWarning("[MP_GameManager] 既存の GameStarter が検出されたため無効化しました（競合防止）。");
            oldStarter.enabled = false;
            Destroy(oldStarter);
        }
    }

    private void Start()
    {
        // _starterCanvas がインスペクター未設定の場合のフォールバック
        if (_starterCanvas == null)
        {
            var loadingText = GameObject.Find("NowLoadingText");
            if (loadingText != null)
            {
                var canvas = loadingText.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    _starterCanvas = canvas.gameObject;
                }
            }
        }

        // 初期状態はイベントが発火しないため、現在の状態を明示的に反映する
        ApplyStateToUI(_currentState);

        // Play On Awake をOFFにした Timeline の初期状態(ローディング表示など)を、再生せずに反映する
        if (_startDirectors != null)
        {
            foreach (var director in _startDirectors)
            {
                if (director == null) continue;
                director.time = 0;
                director.Evaluate();
            }
        }

        // WaveManagerのフォールバック
        if (_waveManager == null)
        {
            _waveManager = FindAnyObjectByType<WaveManager>();
        }
    }

    private void Update()
    {
        // ゲーム開始前は自プレイヤーの操作を制限
        if (_localPlayerState == null && _currentState == GameState.WaitingForPlayers)
        {
            FindAndLockLocalPlayer();
        }
    }

    /// <summary>
    /// 自端末のプレイヤーを探し、ゲーム開始前は操作をロックする
    /// </summary>
    private void FindAndLockLocalPlayer()
    {
        var playerObjs = GameObject.FindGameObjectsWithTag("Player");
        foreach (var obj in playerObjs)
        {
            var netObj = obj.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsLocalPlayer)
            {
                _localPlayerState = obj.GetComponent<PlayerStateManager>();
                if (_localPlayerState != null)
                {
                    _localPlayerState.MovieState();
                }
                break;
            }
        }
    }

    /// <summary>
    /// MP_GameSyncManager から呼ばれる：全員のロードが完了し開始シーケンスを実行
    /// </summary>
    public void OnGameStart()
    {
        StartCoroutine(StartMovieSequenceRoutine());
    }

    private IEnumerator StartMovieSequenceRoutine()
    {
        SetState(GameState.OpeningMovie);
        Debug.Log("[MP_GameManager] シーン読み込み完了。開始ムービーを再生します");

        float maxDuration = 0f;
        if (_startDirectors != null && _startDirectors.Length > 0)
        {
            foreach (var director in _startDirectors)
            {
                if (director == null) continue;
                director.time = 0;
                director.Play();
                if (director.duration > maxDuration)
                {
                    maxDuration = (float)director.duration;
                }
            }
        }

        // ムービーの長さ分待機（ムービーがない場合は即座に進行）
        if (maxDuration > 0f)
        {
            yield return new WaitForSeconds(maxDuration);
        }

        // ムービー終了後にプレイヤー生成・ゲームプレイ開始
        StartGamePlay();
    }

    private void StartGamePlay()
    {
        SetState(GameState.Playing);
        Debug.Log("[MP_GameManager] ムービー終了。プレイヤーをスポーンしてゲーム開始！");

        // ホスト/サーバー側のみプレイヤーのスポーンとウェーブ開始を実行
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            SpawnAllPlayers();

            if (_waveManager != null)
            {
                _waveManager.StartWave(0);
            }
            else
            {
                Debug.LogWarning("[MP_GameManager] WaveManager が見つかりません。");
            }
        }

        // 自プレイヤーの操作を解禁
        FindAndUnlockLocalPlayer();
    }

    /// <summary>
    /// 自端末のプレイヤーを探索して操作ロックを解除する
    /// </summary>
    private void FindAndUnlockLocalPlayer()
    {
        if (_localPlayerState == null)
        {
            var playerObjs = GameObject.FindGameObjectsWithTag("Player");
            foreach (var obj in playerObjs)
            {
                var netObj = obj.GetComponent<NetworkObject>();
                if (netObj != null && netObj.IsLocalPlayer)
                {
                    _localPlayerState = obj.GetComponent<PlayerStateManager>();
                    break;
                }
            }
        }

        if (_localPlayerState != null)
        {
            _localPlayerState.NormalState();
        }
    }

    public void OnWaitingForPlayers()
    {
        SetState(GameState.WaitingForPlayers);
    }

    private void SetState(GameState newState)
    {
        if (_currentState == newState)
            return;

        var previousState = _currentState;
        _currentState = newState;
        StateChanged?.Invoke(previousState, newState);
        ApplyStateToUI(newState);
    }

    private void ApplyStateToUI(GameState state)
    {
        // 開始演出Canvas（Now Loading表示および開始ムービーUI）はロード待機中〜開始ムービー再生中まで表示
        if (_starterCanvas != null)
        {
            _starterCanvas.SetActive(state == GameState.WaitingForPlayers || state == GameState.OpeningMovie);
        }

        // HUDはゲームプレイ中のみ表示（ムービー中は非表示）
        if (_hudCanvas != null)
        {
            _hudCanvas.SetActive(state == GameState.Playing);
        }
    }

    public void RegisterPlayer(NetworkObject player)
    {
        if (!_players.Contains(player))
        {
            _players.Add(player);
        }
    }

    public void UnregisterPlayer(NetworkObject player)
    {
        _players.Remove(player);
    }

    /// <summary>
    /// ホスト側で全接続クライアントのプレイヤーをステージ固有のスポーン地点に生成する
    /// </summary>
    public void SpawnAllPlayers()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        if (_playerPrefab == null)
        {
            Debug.LogError("[MP_GameManager] _playerPrefab がインスペクターで設定されていません！");
            return;
        }

        var spawnPoints = StageSpawnPoints.Instance;
        var clients = NetworkManager.Singleton.ConnectedClientsList;

        int index = 0;
        foreach (var client in clients)
        {
            // 既にそのクライアント用の PlayerObject が存在する場合はスキップ（重複防止）
            if (client.PlayerObject != null)
            {
                Debug.Log($"[MP_GameManager] ClientId {client.ClientId} のプレイヤーは既にスポーン済みです。");
                continue;
            }

            Transform spawnPoint = spawnPoints != null ? spawnPoints.GetSpawnPoint(index) : null;
            Vector3 pos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
            Quaternion rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

            GameObject playerObj = Instantiate(_playerPrefab, pos, rot);
            var netObj = playerObj.GetComponent<NetworkObject>();

            // クライアントの所有権を付与してネットワーク上にスポーン
            netObj.SpawnAsPlayerObject(client.ClientId, destroyWithScene: true);
            RegisterPlayer(netObj);

            Debug.Log($"[MP_GameManager] ClientId {client.ClientId} のプレイヤーをスポーンしました (位置: {pos})");
            index++;
        }
    }
}
