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

    private readonly float _heartbeatInterval;
    private readonly float _heartbeatTimeout;
    private readonly Func<ISession> _sessionProvider;
    private readonly Action _onHostTimeout;

    private readonly Dictionary<string, float> _lastHeartbeatByPlayerId = new Dictionary<string, float>();
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
                NetworkDelivery.ReliableSequenced);
        }
    }

    private void SendHeartbeat(ISession currentSession, CustomMessagingManager messagingManager)
    {
        if (currentSession?.CurrentPlayer == null || messagingManager == null) return;

        using (var writer = new FastBufferWriter(128, Allocator.Temp))
        {
            writer.WriteValueSafe(currentSession.CurrentPlayer.Id);
            messagingManager.SendNamedMessage(
                HeartbeatMessageName,
                NetworkManager.ServerClientId,
                writer,
                NetworkDelivery.ReliableSequenced);
        }
    }

    private void OnHeartbeatReceived(ulong senderClientId, FastBufferReader reader)
    {
        var currentSession = _sessionProvider();
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer || currentSession == null) return;

        reader.ReadValueSafe(out string playerId);
        if (string.IsNullOrWhiteSpace(playerId) || currentSession.GetPlayer(playerId) == null) return;

        _lastHeartbeatByPlayerId[playerId] = Time.unscaledTime;
    }

    private void OnServerHeartbeatReceived(ulong senderClientId, FastBufferReader reader)
    {
        _clientSilenceTimer = 0f;
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
            _lastHeartbeatByPlayerId.Remove(playerId);
        }
        catch (Exception ex)
        {
            _lastHeartbeatByPlayerId[playerId] = Time.unscaledTime;
            _playersBeingRemoved.Remove(playerId);
            Debug.LogError($"[MSM] タイムアウトしたプレイヤーの削除に失敗: {ex.Message}");
        }
    }
}
