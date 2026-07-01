using System;
using Unity.Netcode;
using UnityEngine;

public enum GameMode
{
    SinglePlayer,
    MultiplayerHost,
    MultiplayerClient
}

public class GameModeManager : MonoBehaviour
{
    public static GameModeManager Instance { get; private set; }

    private GameMode _currentMode;
    public GameMode CurrentMode => _currentMode;

    // プロパティ
    public bool IsSinglePlayer => _currentMode == GameMode.SinglePlayer;
    public bool IsHost => _currentMode == GameMode.MultiplayerHost;
    public bool IsClient => _currentMode == GameMode.MultiplayerClient;

    private void Awake()
    {
        // シングルトンセットアップ
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        DetermineGameMode();
    }

    /// <summary>
    /// ゲームモードを決定するメソッド。ネットワークの状態に応じて、シングルプレイ、ホスト、またはクライアントのいずれかを設定
    /// </summary>
    private void DetermineGameMode()
    {
        bool networkRunning = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

        if (!networkRunning)
        {
            _currentMode = GameMode.SinglePlayer;
        }
        else if (NetworkManager.Singleton.IsHost)
        {
            _currentMode = GameMode.MultiplayerHost;
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            _currentMode = GameMode.MultiplayerClient;
        }
    }
}
