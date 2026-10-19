using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Multiplayer;
using UnityEngine;

/// <summary>
/// UGSのMultiplayerServiceを用いたルームの作成・検索・参加・退出・Ready属性更新のみを担当するクラス (SRP)
/// </summary>
public class LobbySessionService : ILobbySessionService
{
    private readonly IUnityServicesInitializer _servicesInitializer;
    private readonly Action _onSessionConnected;
    private readonly Action _onSessionLeaving;

    private ISession _currentSession;
    public ISession CurrentSession => _currentSession;

    public event Action OnSessionChanged;

    public LobbySessionService(
        IUnityServicesInitializer servicesInitializer,
        Action onSessionConnected = null,
        Action onSessionLeaving = null)
    {
        _servicesInitializer = servicesInitializer ?? throw new ArgumentNullException(nameof(servicesInitializer));
        _onSessionConnected = onSessionConnected;
        _onSessionLeaving = onSessionLeaving;
    }

    public async UniTask<bool> CreateRoomSessionAsync(string roomName, int maxPlayers)
    {
        if (!_servicesInitializer.IsInitialized)
        {
            Debug.LogError("Servicesが初期化されていません");
            return false;
        }

        try
        {
            ConfigureWebSocketTransport();

            var options = new SessionOptions
            {
                Name = roomName,
                MaxPlayers = maxPlayers,
                IsPrivate = false
            }
            .WithRelayNetwork()
            .WithNetworkOptions(new NetworkOptions { RelayProtocol = RelayProtocol.WSS });

            _currentSession = await MultiplayerService.Instance.CreateSessionAsync(options);
            _currentSession.Changed += HandleSessionChanged;

            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.StartHost();
            }

            _onSessionConnected?.Invoke();
            Debug.Log("[MSM]: ホストとして接続 (WSS)");

            return true;
        }
        catch (Exception ex)
        {
            Debug.Log($"ルーム作成失敗: {ex.Message}");
            return false;
        }
    }

    public async UniTask<List<ISessionInfo>> SearchRoomSessionsAsync()
    {
        if (!_servicesInitializer.IsInitialized)
        {
            Debug.LogError("Servicesが初期化されていません");
            return new List<ISessionInfo>();
        }

        try
        {
            var queryOptions = new QuerySessionsOptions();
            QuerySessionsResults queryResponse = await MultiplayerService.Instance.QuerySessionsAsync(queryOptions);
            return new List<ISessionInfo>(queryResponse.Sessions);
        }
        catch (Exception ex)
        {
            Debug.LogError($"ルーム検索失敗: {ex.Message}");
            return new List<ISessionInfo>();
        }
    }

    public async UniTask<bool> JoinRoomSessionAsync(ISessionInfo targetRoom)
    {
        if (!_servicesInitializer.IsInitialized)
        {
            Debug.Log("Servicesが初期化されていません");
            return false;
        }

        try
        {
            ConfigureWebSocketTransport();

            var joinOptions = new JoinSessionOptions()
                .WithNetworkOptions(new NetworkOptions { RelayProtocol = RelayProtocol.WSS });

            _currentSession = await MultiplayerService.Instance.JoinSessionByIdAsync(targetRoom.Id, joinOptions);
            _currentSession.Changed += HandleSessionChanged;

            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.StartClient();
            }

            _onSessionConnected?.Invoke();
            Debug.Log("[MSM]: クライアントとして接続 (WSS)");

            return true;
        }
        catch (Exception ex)
        {
            Debug.Log($"ルームに参加失敗: {ex.Message}");
            return false;
        }
    }

    private static void ConfigureWebSocketTransport()
    {
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.NetworkConfig?.NetworkTransport is UnityTransport transport)
        {
            transport.UseWebSockets = true;
        }
    }

    public async UniTask LeaveRoomSessionAsync()
    {
        if (_currentSession == null) return;

        try
        {
            _onSessionLeaving?.Invoke();
            _currentSession.Changed -= HandleSessionChanged;
            await _currentSession.LeaveAsync();
            _currentSession = null;

            NetworkManager.Singleton?.Shutdown();
        }
        catch (Exception ex)
        {
            Debug.LogError($"ルーム退出時にエラーが発生しました: {ex.Message}");
        }
    }

    public async UniTask SetReadyStatusAsync(bool isReady)
    {
        if (_currentSession == null || _currentSession.CurrentPlayer == null)
        {
            Debug.LogError("セッションまたはプレイヤーが存在しません");
            return;
        }

        try
        {
            var properties = SessionPlayerPropertyHelper.CreateReadyProperties(isReady);
            _currentSession.CurrentPlayer.SetProperties(properties);
            await _currentSession.SaveCurrentPlayerDataAsync();
        }
        catch (Exception ex)
        {
            Debug.LogError($"準備状態の更新に失敗しました: {ex.Message}");
        }
    }

    private void HandleSessionChanged()
    {
        OnSessionChanged?.Invoke();
    }
}

