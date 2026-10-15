using System;
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

    // ネットワーク上のプレイヤーリスト
    private readonly List<NetworkObject> _players = new();
    public IReadOnlyList<NetworkObject> Players => _players;

    private PlayerStateManager _localPlayerState;

    public event Action<GameState, GameState> StateChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 既存の GameStarter がシーンに残っている場合、勝手にタイムラインで開始してしまうため無効化する
        var oldStarter = FindAnyObjectByType<GameStarter>();
        if (oldStarter != null && oldStarter.gameObject != this.gameObject)
        {
            Debug.LogWarning("[MP_GameManager] 既存の GameStarter が検出されたため無効化しました（競合防止）。");
            oldStarter.enabled = false;
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
    /// MP_GameSyncManager から呼ばれる：全員のロードが完了しゲーム本編開始
    /// </summary>
    public void OnGameStart()
    {
        SetState(GameState.Playing);
        Debug.Log("[MP_GameManager] ゲーム開始！");

        // 開始ムービー（Timeline）を全員で同じタイミングで再生
        if (_startDirectors != null)
        {
            foreach (var director in _startDirectors)
            {
                if (director == null) continue;
                director.time = 0;
                director.Play();
            }
        }

        // 自プレイヤーの操作を解禁
        if (_localPlayerState != null)
        {
            _localPlayerState.NormalState();
        }

        // ホスト/サーバー側のみウェーブを開始
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            if (_waveManager != null)
            {
                _waveManager.StartWave(0);
            }
            else
            {
                Debug.LogWarning("[MP_GameManager] WaveManager が見つかりません。");
            }
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
        var isWaiting = state == GameState.WaitingForPlayers;

        if (_starterCanvas != null)
        {
            _starterCanvas.SetActive(isWaiting);
        }

        if (_hudCanvas != null)
        {
            _hudCanvas.SetActive(!isWaiting);
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
}
