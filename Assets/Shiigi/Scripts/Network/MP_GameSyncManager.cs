using Unity.Netcode;
using UnityEngine;

/// <summary>
/// NGO標準の NetworkVariable と NetworkSceneManager イベントを用いたシンプルな進行同期クラス
/// </summary>
public class MP_GameSyncManager : NetworkBehaviour
{
    public static MP_GameSyncManager Instance { get; private set; }

    public enum GamePhase : byte
    {
        Waiting,        // ロード待機中（Now Loading... 表示中）
        Playing         // ゲームプレイ中
    }

    // サーバーが管理し、全員に自動同期されるフェーズ変数
    public NetworkVariable<GamePhase> CurrentPhase = new NetworkVariable<GamePhase>(
        GamePhase.Waiting,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        Instance = this;

        // フェーズが変わったときのコールバックを全クライアントで購読
        CurrentPhase.OnValueChanged += OnPhaseChanged;

        OnPhaseChanged(CurrentPhase.Value, CurrentPhase.Value);

        if (IsServer)
        {
            CurrentPhase.Value = GamePhase.Waiting;

            // NGO標準のシーン同期イベントを購読
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnSceneEvent += HandleSceneEvent;
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        CurrentPhase.OnValueChanged -= OnPhaseChanged;

        if (IsServer && NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent -= HandleSceneEvent;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// NGO標準のシーン同期イベントハンドラ
    /// </summary>
    private void HandleSceneEvent(SceneEvent sceneEvent)
    {
        if (!IsServer) return;

        // サーバーおよび接続中の全クライアントがシーンロードを完了した瞬間に発火
        if (sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted)
        {
            Debug.Log("[MP_GameSyncManager] 全プレイヤーのシーン読み込みが完了。ゲームを開始");
            
            // Playingに変更（全クライアントへ自動同期）
            CurrentPhase.Value = GamePhase.Playing;
        }
    }

    /// <summary>
    /// フェーズ変更時の通知（全クライアントで一斉に発火）
    /// </summary>
    private void OnPhaseChanged(GamePhase previous, GamePhase current)
    {
        if (current == GamePhase.Waiting)
        {
            MP_GameManager.Instance?.OnWaitingForPlayers();
        }
        else if (current == GamePhase.Playing)
        {
            Debug.Log("[MP_GameSyncManager] GamePhase.Playing を受信。ゲーム本編を開始します。");
            MP_GameManager.Instance?.OnGameStart();
        }
    }
}
