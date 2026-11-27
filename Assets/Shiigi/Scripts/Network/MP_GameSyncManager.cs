using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// NetworkVariable と NetworkSceneManager を用いてゲーム開始フェーズを全端末で同期する。
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

    /// <summary>
    /// フェーズ変更通知イベント
    /// </summary>
    public event Action<GamePhase> PhaseChanged;

    /// <summary>
    /// スポーン順によらず購読可能なグローバルフェーズ変更イベント
    /// </summary>
    public static event Action<GamePhase> GlobalPhaseChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        GlobalPhaseChanged = null;
        Instance = null;
    }

    public override void OnNetworkSpawn()
    {
        Instance = this;

        // フェーズが変わったときのコールバックを全クライアントで購読（多重登録防止のため先に解除）
        CurrentPhase.OnValueChanged -= OnPhaseChanged;
        CurrentPhase.OnValueChanged += OnPhaseChanged;

        if (IsServer)
        {
            CurrentPhase.Value = GamePhase.Waiting;

            // NGO標準のシーン同期イベントを購読
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnSceneEvent -= HandleSceneEvent;
                NetworkManager.Singleton.SceneManager.OnSceneEvent += HandleSceneEvent;
            }
        }

        OnPhaseChanged(CurrentPhase.Value, CurrentPhase.Value);
    }

    public override void OnNetworkDespawn()
    {
        CleanupInstanceAndSubscriptions();
    }

    public override void OnDestroy()
    {
        CleanupInstanceAndSubscriptions();
        base.OnDestroy();
    }

    private void CleanupInstanceAndSubscriptions()
    {
        UnsubscribeAllEvents();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void UnsubscribeAllEvents()
    {
        CurrentPhase.OnValueChanged -= OnPhaseChanged;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent -= HandleSceneEvent;
        }
    }

    /// <summary>
    /// バックグラウンドで止まってた時の処理
    /// </summary>
    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && IsClient && !IsServer && IsSpawned)
        {
            // バックグラウンド復帰時にホストの現在のフェーズと同期を取る
            Debug.Log($"[MP_GameSyncManager] ウィンドウ復帰を検知。現在のフェーズ: {CurrentPhase.Value}");
            if (CurrentPhase.Value == GamePhase.Playing)
            {
                NotifyPhase(GamePhase.Playing);
            }
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
            Debug.Log("[MP_GameSyncManager] 全プレイヤーのシーン読み込みが完了。開始シーケンスを開始します");

            // プレイヤー生成と全員の準備完了（全プレイヤーの出現）を待ってから Playing に変更する
            var openingSync = MP_OpeningSyncManager.Instance;
            if (openingSync != null)
            {
                openingSync.BeginOpeningOnServer(SetPlayingPhase);
            }
            else
            {
                Debug.LogWarning("[MP_GameSyncManager] MP_OpeningSyncManager が見つからないため、準備完了を待たずに開始します。");
                SetPlayingPhase();
            }
        }
    }

    /// <summary>
    /// Playingに変更（全クライアントへ自動同期される）
    /// </summary>
    private void SetPlayingPhase()
    {
        if (!IsServer || !IsSpawned) return;

        CurrentPhase.Value = GamePhase.Playing;
    }

    /// <summary>
    /// フェーズ変更時の通知（全クライアントで一斉に発火）
    /// </summary>
    private void OnPhaseChanged(GamePhase previous, GamePhase current)
    {
        NotifyPhase(current);
    }

    private void NotifyPhase(GamePhase phase)
    {
        if (phase == GamePhase.Playing)
        {
            Debug.Log("[MP_GameSyncManager] GamePhase.Playing を受信。開始フェーズを通知します。");
        }
        PhaseChanged?.Invoke(phase);
        GlobalPhaseChanged?.Invoke(phase);
    }
}
