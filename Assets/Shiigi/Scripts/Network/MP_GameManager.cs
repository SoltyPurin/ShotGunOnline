using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// ゲーム全体の状態遷移オーケストレーションを行うファサードクラス。
/// UI表示・ムービー再生・プレイヤースポーン・ローカル入力ロックの各単一責務クラスへ処理を委譲します。
/// </summary>
public class MP_GameManager : MonoBehaviour, IGameStateProvider
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
    public GameObject StarterCanvas => _uiStateBinder != null ? _uiStateBinder.StarterCanvas : _starterCanvas;

    [Header("進行 参照")]
    [SerializeField] private WaveManager _waveManager;

    [Header("開始ムービー")]
    [Tooltip("ゲーム開始時のPlayableDirector。OnGameStartで再生する")]
    [SerializeField] private PlayableDirector[] _startDirectors;

    [Header("プレイヤー生成")]
    [Tooltip("マルチプレイ用のPlayerプレハブ（NetWork Player Variantなど）")]
    [SerializeField] private GameObject _playerPrefab;

    // 各単一責務サブシステム
    private InGameUIStateBinder _uiStateBinder;
    private OpeningCutsceneController _cutsceneController;
    private NetworkPlayerSpawner _playerSpawner;
    private LocalPlayerStateLocker _localPlayerLocker;

    public IReadOnlyList<NetworkObject> Players
    {
        get
        {
            EnsureSubsystemsInitialized();
            return _playerSpawner.Players;
        }
    }

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

        DisableLegacyGameStarter();
        EnsureSubsystemsInitialized();
    }

    private void EnsureSubsystemsInitialized()
    {
        if (_playerSpawner != null) return;

        _uiStateBinder = new InGameUIStateBinder(_starterCanvas, _hudCanvas);
        _cutsceneController = new OpeningCutsceneController(_startDirectors);
        _playerSpawner = new NetworkPlayerSpawner(_playerPrefab);
        _localPlayerLocker = new LocalPlayerStateLocker();
    }

    private void DisableLegacyGameStarter()
    {
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
        EnsureSubsystemsInitialized();

        // _starterCanvas がインスペクター未設定の場合のフォールバック解決と初期UI反映
        _uiStateBinder.ResolveFallbackCanvases();
        _starterCanvas = _uiStateBinder.StarterCanvas;
        _uiStateBinder.ApplyStateToUI(_currentState);

        // Play On Awake をOFFにした Timeline の初期状態を再生せずに反映
        _cutsceneController.EvaluateInitialState();

        // WaveManagerのフォールバック
        if (_waveManager == null)
        {
            _waveManager = FindAnyObjectByType<WaveManager>();
        }
    }

    private void Update()
    {
        // ゲーム開始前は自プレイヤーの操作を制限
        if (_currentState == GameState.WaitingForPlayers && !_localPlayerLocker.HasLocalPlayer)
        {
            _localPlayerLocker.TryFindAndLockLocalPlayer();
        }
    }

    /// <summary>
    /// MP_GameSyncManager から呼ばれる：全員のロードが完了し開始シーケンスを実行
    /// </summary>
    public void OnGameStart()
    {
        EnsureSubsystemsInitialized();
        SetState(GameState.OpeningMovie);
        Debug.Log("[MP_GameManager] シーン読み込み完了。開始ムービーを再生します");

        StartCoroutine(_cutsceneController.PlaySequenceRoutine(StartGamePlay));
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
        _localPlayerLocker.FindAndUnlockLocalPlayer();
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
        _uiStateBinder?.ApplyStateToUI(newState);
    }

    public void RegisterPlayer(NetworkObject player)
    {
        EnsureSubsystemsInitialized();
        _playerSpawner.RegisterPlayer(player);
    }

    public void UnregisterPlayer(NetworkObject player)
    {
        EnsureSubsystemsInitialized();
        _playerSpawner.UnregisterPlayer(player);
    }

    /// <summary>
    /// ホスト側で全接続クライアントのプレイヤーをステージ固有のスポーン地点に生成する
    /// </summary>
    public void SpawnAllPlayers()
    {
        EnsureSubsystemsInitialized();
        _playerSpawner.SpawnAllPlayers(NetworkManager.Singleton, StageSpawnPoints.Instance);
    }
}
