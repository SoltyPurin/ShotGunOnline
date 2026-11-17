using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;

/// <summary>
/// ホスト・クライアント間のハートビート送受信とタイムアウト検知を担当するクラス (SRP)
/// </summary>
public class SessionHeartbeatMonitor
{
    private const string HeartbeatMessageName = "PlayerHeartbeat";
    private const string ServerHeartbeatMessageName = "ServerHeartbeat";
    private const float MaxDeltaTimeStep = 0.5f;
    private const int MaxPlayerIdLength = 128;
    private const int HeartbeatWriterInitialSize = 128;
    private const int HeartbeatWriterMaxSize = 1024;

    private readonly float _heartbeatInterval;
    private readonly float _heartbeatTimeout;
    private readonly Func<ISession> _sessionProvider;
    private readonly Action _onHostTimeout;

    private readonly Dictionary<string, float> _lastHeartbeatByPlayerId = new Dictionary<string, float>();
    private readonly Dictionary<string, ulong> _clientIdByPlayerId = new Dictionary<string, ulong>();
    private readonly Dictionary<ulong, string> _playerIdByClientId = new Dictionary<ulong, string>();
    private readonly HashSet<string> _playersBeingRemoved = new HashSet<string>();
    private float _nextHeartbeatTime;
    private float _clientSilenceTimer;
    private bool _hasTriggeredHostTimeout;

    public SessionHeartbeatMonitor(
        Func<ISession> sessionProvider,
        float heartbeatInterval = 2f,
        float heartbeatTimeout = 10f,
        Action onHostTimeout = null)
    {
        _sessionProvider = sessionProvider ?? throw new ArgumentNullException(nameof(sessionProvider));
        _heartbeatInterval = heartbeatInterval;
        _heartbeatTimeout = heartbeatTimeout;
        _onHostTimeout = onHostTimeout;
    }

    public void SubscribeMessaging(CustomMessagingManager messagingManager)
    {
        if (messagingManager == null) return;
        Reset();
        messagingManager.UnregisterNamedMessageHandler(HeartbeatMessageName);
        messagingManager.RegisterNamedMessageHandler(HeartbeatMessageName, OnHeartbeatReceived);
        messagingManager.UnregisterNamedMessageHandler(ServerHeartbeatMessageName);
        messagingManager.RegisterNamedMessageHandler(ServerHeartbeatMessageName, OnServerHeartbeatReceived);
    }

    public void UnsubscribeMessaging(CustomMessagingManager messagingManager)
    {
        if (messagingManager == null) return;
        messagingManager.UnregisterNamedMessageHandler(HeartbeatMessageName);
        messagingManager.UnregisterNamedMessageHandler(ServerHeartbeatMessageName);
    }

    public void Reset()
    {
        _lastHeartbeatByPlayerId.Clear();
        _clientIdByPlayerId.Clear();
        _playerIdByClientId.Clear();
        _playersBeingRemoved.Clear();
        _nextHeartbeatTime = 0f;
        _clientSilenceTimer = 0f;
        _hasTriggeredHostTimeout = false;
    }

    public void Tick(float now, NetworkManager networkManager)
    {
        var currentSession = _sessionProvider();
        if (currentSession == null || networkManager == null || !networkManager.IsListening) return;

        if (networkManager.IsServer)
        {
            CheckPlayerHeartbeats(now, currentSession);

            if (now >= _nextHeartbeatTime)
            {
                SendServerHeartbeat(networkManager.CustomMessagingManager);
                _nextHeartbeatTime = now + _heartbeatInterval;
            }
        }
        else if (networkManager.IsClient)
        {
            if (now >= _nextHeartbeatTime)
            {
                SendHeartbeat(currentSession, networkManager.CustomMessagingManager);
                _nextHeartbeatTime = now + _heartbeatInterval;
            }

            if (networkManager.IsConnectedClient && !_hasTriggeredHostTimeout)
            {
                _clientSilenceTimer += Mathf.Min(Time.unscaledDeltaTime, MaxDeltaTimeStep);
                if (_clientSilenceTimer >= _heartbeatTimeout)
                {
                    _hasTriggeredHostTimeout = true;
                    Debug.LogWarning("[MSM] ホストからのハートビートがタイムアウトしました。");
                    _onHostTimeout?.Invoke();
                }
            }
        }
    }

    private static void SendServerHeartbeat(CustomMessagingManager messagingManager)
    {
        if (messagingManager == null) return;

        using (var writer = new FastBufferWriter(1, Allocator.Temp))
        {
            messagingManager.SendNamedMessageToAll(
                ServerHeartbeatMessageName,
                writer,
                NetworkDelivery.Unreliable);
        }
    }

    private void SendHeartbeat(ISession currentSession, CustomMessagingManager messagingManager)
    {
        if (currentSession?.CurrentPlayer == null || messagingManager == null) return;

        // playerIdが想定より長い場合でもOverflowExceptionで毎フレーム失敗しないよう拡張可能なバッファを使用
        using (var writer = new FastBufferWriter(HeartbeatWriterInitialSize, Allocator.Temp, HeartbeatWriterMaxSize))
        {
            writer.WriteValueSafe(currentSession.CurrentPlayer.Id);
            messagingManager.SendNamedMessage(
                HeartbeatMessageName,
                NetworkManager.ServerClientId,
                writer,
                NetworkDelivery.Unreliable);
        }
    }

    private void OnHeartbeatReceived(ulong senderClientId, FastBufferReader reader)
    {
        var currentSession = _sessionProvider();
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsServer || currentSession == null) return;

        // ホスト自身のClientIdを名乗るメッセージは受け付けない
        if (senderClientId == NetworkManager.ServerClientId) return;

        // 不正・破損ペイロードで例外が伝播しないよう安全に読み取る
        string playerId;
        try
        {
            reader.ReadValueSafe(out playerId);
        }
        catch (Exception)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(playerId) || playerId.Length > MaxPlayerIdLength) return;
        if (currentSession.GetPlayer(playerId) == null) return;

        // ホスト自身のplayerIdを名乗るなりすましを防止
        if (playerId == currentSession.CurrentPlayer?.Id) return;

        // 既に別の接続中ClientIdに紐付いているplayerIdのなりすましを防止
        if (_clientIdByPlayerId.TryGetValue(playerId, out var existingClientId) &&
            existingClientId != senderClientId &&
            networkManager.ConnectedClients.ContainsKey(existingClientId))
        {
            return;
        }

        // 1つのClientIdが複数のplayerIdを名乗り、離脱済みプレイヤーを延命させる不正を防止
        if (_playerIdByClientId.TryGetValue(senderClientId, out var boundPlayerId) && boundPlayerId != playerId)
        {
            return;
        }

        _clientIdByPlayerId[playerId] = senderClientId;
        _playerIdByClientId[senderClientId] = playerId;
        _lastHeartbeatByPlayerId[playerId] = Time.unscaledTime;
    }

    private void OnServerHeartbeatReceived(ulong senderClientId, FastBufferReader reader)
    {
        // サーバー以外から届いたものは無視する
        if (senderClientId != NetworkManager.ServerClientId) return;
        _clientSilenceTimer = 0f;
    }

    private void RemovePlayerBinding(string playerId)
    {
        if (_clientIdByPlayerId.TryGetValue(playerId, out var clientId))
        {
            _clientIdByPlayerId.Remove(playerId);
            if (_playerIdByClientId.TryGetValue(clientId, out var boundPlayerId) && boundPlayerId == playerId)
            {
                _playerIdByClientId.Remove(clientId);
            }
        }
    }

    private void CheckPlayerHeartbeats(float now, ISession currentSession)
    {
        if (currentSession == null || !(currentSession is IHostSession)) return;

        var hostPlayerId = currentSession.CurrentPlayer?.Id;
        var activePlayerIds = new HashSet<string>();
        foreach (var player in currentSession.Players)
        {
            if (player.Id == hostPlayerId) continue;

            activePlayerIds.Add(player.Id);
            if (!_lastHeartbeatByPlayerId.ContainsKey(player.Id) && !_playersBeingRemoved.Contains(player.Id))
            {
                _lastHeartbeatByPlayerId[player.Id] = now;
            }
        }

        _playersBeingRemoved.RemoveWhere(playerId => !activePlayerIds.Contains(playerId));

        var stalePlayerIds = new List<string>();
        var timedOutPlayerIds = new List<string>();
        foreach (var heartbeat in _lastHeartbeatByPlayerId)
        {
            if (!activePlayerIds.Contains(heartbeat.Key))
            {
                stalePlayerIds.Add(heartbeat.Key);
            }
            else if (now - heartbeat.Value >= _heartbeatTimeout)
            {
                timedOutPlayerIds.Add(heartbeat.Key);
            }
        }

        foreach (var playerId in stalePlayerIds)
        {
            _lastHeartbeatByPlayerId.Remove(playerId);
            RemovePlayerBinding(playerId);
        }

        foreach (var playerId in timedOutPlayerIds)
        {
            RemoveTimedOutPlayerAsync(playerId).Forget();
        }
    }

    private async UniTaskVoid RemoveTimedOutPlayerAsync(string playerId)
    {
        var currentSession = _sessionProvider();
        if (!(currentSession is IHostSession hostSession) || !_playersBeingRemoved.Add(playerId)) return;

        try
        {
            Debug.Log($"[MSM] プレイヤータイムアウト: {playerId}");
            await hostSession.RemovePlayerAsync(playerId);

            // await 中にセッションが切り替わった（退出・Reset済み）場合は古い状態を書き換えない
            if (!ReferenceEquals(_sessionProvider(), currentSession)) return;

            _lastHeartbeatByPlayerId.Remove(playerId);

            // UGSセッションから削除したプレイヤーのNGO接続も切断してゴースト化を防ぐ
            var networkManager = NetworkManager.Singleton;
            if (networkManager != null &&
                networkManager.IsServer &&
                _clientIdByPlayerId.TryGetValue(playerId, out var clientId) &&
                clientId != NetworkManager.ServerClientId &&
                networkManager.ConnectedClients.ContainsKey(clientId))
            {
                networkManager.DisconnectClient(clientId);
            }
            RemovePlayerBinding(playerId);
        }
        catch (Exception ex)
        {
            if (ReferenceEquals(_sessionProvider(), currentSession))
            {
                _lastHeartbeatByPlayerId[playerId] = Time.unscaledTime;
                _playersBeingRemoved.Remove(playerId);
            }
            Debug.LogError($"[MSM] タイムアウトしたプレイヤーの削除に失敗: {ex.Message}");
        }
    }
}
