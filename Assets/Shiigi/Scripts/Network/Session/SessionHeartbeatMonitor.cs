using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;

/// <summary>
/// ホスト・クライアント間のハートビート送受信とタイムアウト検知を担当するクラス (SRP)。
/// 毎フレーム走るサーバー側の生存チェックで再利用バッファを使用し、GCアロケーションをゼロに保ちます。
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

    private readonly Dictionary<string, float> _lastHeartbeatByPlayerId = new();
    private readonly Dictionary<string, ulong> _clientIdByPlayerId = new();
    private readonly Dictionary<ulong, string> _playerIdByClientId = new();
    private readonly HashSet<string> _playersBeingRemoved = new();

    // 毎フレームのGCアロケーションを防ぐ再利用バッファ
    private readonly HashSet<string> _activePlayerIdsBuffer = new();
    private readonly List<string> _stalePlayerIdsBuffer = new();
    private readonly List<string> _timedOutPlayerIdsBuffer = new();
    private readonly List<string> _removedCleanupBuffer = new();

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
        _activePlayerIdsBuffer.Clear();
        _stalePlayerIdsBuffer.Clear();
        _timedOutPlayerIdsBuffer.Clear();
        _removedCleanupBuffer.Clear();
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

        using var writer = new FastBufferWriter(1, Allocator.Temp);
        messagingManager.SendNamedMessageToAll(
            ServerHeartbeatMessageName,
            writer,
            NetworkDelivery.Unreliable);
    }

    private void SendHeartbeat(ISession currentSession, CustomMessagingManager messagingManager)
    {
        if (currentSession?.CurrentPlayer == null || messagingManager == null) return;

        using var writer = new FastBufferWriter(HeartbeatWriterInitialSize, Allocator.Temp, HeartbeatWriterMaxSize);
        writer.WriteValueSafe(currentSession.CurrentPlayer.Id);
        messagingManager.SendNamedMessage(
            HeartbeatMessageName,
            NetworkManager.ServerClientId,
            writer,
            NetworkDelivery.Unreliable);
    }

    private void OnHeartbeatReceived(ulong senderClientId, FastBufferReader reader)
    {
        var currentSession = _sessionProvider();
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsServer || currentSession == null) return;
        if (senderClientId == NetworkManager.ServerClientId) return;

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
        if (playerId == currentSession.CurrentPlayer?.Id) return;

        if (_clientIdByPlayerId.TryGetValue(playerId, out var existingClientId) &&
            existingClientId != senderClientId &&
            networkManager.ConnectedClients.ContainsKey(existingClientId))
        {
            return;
        }

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
        if (currentSession is not IHostSession) return;

        var hostPlayerId = currentSession.CurrentPlayer?.Id;
        _activePlayerIdsBuffer.Clear();

        var players = currentSession.Players;
        for (int i = 0; i < players.Count; i++)
        {
            string playerId = players[i].Id;
            if (playerId == hostPlayerId) continue;

            _activePlayerIdsBuffer.Add(playerId);
            if (!_lastHeartbeatByPlayerId.ContainsKey(playerId) && !_playersBeingRemoved.Contains(playerId))
            {
                _lastHeartbeatByPlayerId[playerId] = now;
            }
        }

        if (_playersBeingRemoved.Count > 0)
        {
            _removedCleanupBuffer.Clear();
            foreach (var removingId in _playersBeingRemoved)
            {
                if (!_activePlayerIdsBuffer.Contains(removingId))
                {
                    _removedCleanupBuffer.Add(removingId);
                }
            }
            for (int i = 0; i < _removedCleanupBuffer.Count; i++)
            {
                _playersBeingRemoved.Remove(_removedCleanupBuffer[i]);
            }
        }

        _stalePlayerIdsBuffer.Clear();
        _timedOutPlayerIdsBuffer.Clear();

        foreach (var heartbeat in _lastHeartbeatByPlayerId)
        {
            if (!_activePlayerIdsBuffer.Contains(heartbeat.Key))
            {
                _stalePlayerIdsBuffer.Add(heartbeat.Key);
            }
            else if (now - heartbeat.Value >= _heartbeatTimeout)
            {
                _timedOutPlayerIdsBuffer.Add(heartbeat.Key);
            }
        }

        for (int i = 0; i < _stalePlayerIdsBuffer.Count; i++)
        {
            string playerId = _stalePlayerIdsBuffer[i];
            _lastHeartbeatByPlayerId.Remove(playerId);
            RemovePlayerBinding(playerId);
        }

        for (int i = 0; i < _timedOutPlayerIdsBuffer.Count; i++)
        {
            RemoveTimedOutPlayerAsync(_timedOutPlayerIdsBuffer[i]).Forget();
        }
    }

    private async UniTaskVoid RemoveTimedOutPlayerAsync(string playerId)
    {
        var currentSession = _sessionProvider();
        if (currentSession is not IHostSession hostSession || !_playersBeingRemoved.Add(playerId)) return;

        try
        {
            Debug.Log($"[MSM] プレイヤータイムアウト: {playerId}");
            await hostSession.RemovePlayerAsync(playerId);

            if (!ReferenceEquals(_sessionProvider(), currentSession)) return;

            _lastHeartbeatByPlayerId.Remove(playerId);

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
