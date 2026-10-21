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
    private const float ConnectionTimeoutSeconds = 15f;

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

            // Relay接続が完了するまでは一覧に表示されないよう非公開(IsPrivate = true)で作成
            var options = new SessionOptions
            {
                Name = roomName,
                MaxPlayers = maxPlayers,
                IsPrivate = true
            }
            .WithRelayNetwork()
            .WithNetworkOptions(new NetworkOptions { RelayProtocol = RelayProtocol.WSS });

            _currentSession = await MultiplayerService.Instance.CreateSessionAsync(options);
            _currentSession.Changed += HandleSessionChanged;

            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.StartHost();
            }

            // Relayとの接続確立を待機
            var isConnected = await WaitForNetworkConnectionAsync(isHost: true, ConnectionTimeoutSeconds);
            if (!isConnected)
            {
                Debug.LogError("[MSM]: Relayホスト接続待機がタイムアウトしました。");
                await LeaveRoomSessionAsync();
                return false;
            }

            // ホスト自身のRelay接続完了プロパティを保存
            if (_currentSession.CurrentPlayer != null)
            {
                var relayProps = SessionPlayerPropertyHelper.CreateRelayConnectedProperties(true);
                _currentSession.CurrentPlayer.SetProperties(relayProps);
                await _currentSession.SaveCurrentPlayerDataAsync();
            }

            // Relay接続が確立したのでルームを公開（ルーム一覧に表示されるようにする）
            var hostSession = _currentSession.AsHost();
            hostSession.IsPrivate = false;
            await hostSession.SavePropertiesAsync();

            _onSessionConnected?.Invoke();
            Debug.Log("[MSM]: ホストとして接続完了、ルーム一覧に公開しました (WSS)");

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"ルーム作成失敗: {ex.Message}");
            await LeaveRoomSessionAsync();
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

            // Relayとのクライアント接続確立を待機
            var isConnected = await WaitForNetworkConnectionAsync(isHost: false, ConnectionTimeoutSeconds);
            if (!isConnected)
            {
                Debug.LogError("[MSM]: Relayクライアント接続待機がタイムアウトまたは失敗しました。");
                await LeaveRoomSessionAsync();
                return false;
            }

            // Relay接続が完了したため、プレイヤープロパティに接続済みフラグを付与して保存
            if (_currentSession.CurrentPlayer != null)
            {
                var relayProps = SessionPlayerPropertyHelper.CreateRelayConnectedProperties(true);
                _currentSession.CurrentPlayer.SetProperties(relayProps);
                await _currentSession.SaveCurrentPlayerDataAsync();
            }

            _onSessionConnected?.Invoke();
            Debug.Log("[MSM]: クライアントとして接続完了 (WSS)");

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"ルームに参加失敗: {ex.Message}");
            await LeaveRoomSessionAsync();
            return false;
        }
    }

    /// <summary>
    /// NetworkManager (Relay) の接続確立を非同期で待機する
    /// </summary>
    private static async UniTask<bool> WaitForNetworkConnectionAsync(bool isHost, float timeoutSeconds)
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null) return false;

        float elapsedTime = 0f;
        const float checkInterval = 0.1f;

        while (elapsedTime < timeoutSeconds)
        {
            if (isHost)
            {
                if (networkManager.IsServer && networkManager.IsListening)
                {
                    return true;
                }
            }
            else
            {
                if (networkManager.IsConnectedClient)
                {
                    return true;
                }

                // 接続を試行している最中にリッスンが停止した場合（接続拒否・ハンドシェイク失敗など）
                if (!networkManager.IsListening && elapsedTime > 1.0f)
                {
                    Debug.LogWarning("[MSM]: クライアント接続処理中にリッスンが停止しました。");
                    return false;
                }
            }

            await UniTask.Delay(TimeSpan.FromSeconds(checkInterval), ignoreTimeScale: true);
            elapsedTime += checkInterval;
        }

        return false;
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
        try
        {
            _onSessionLeaving?.Invoke();

            if (_currentSession != null)
            {
                _currentSession.Changed -= HandleSessionChanged;
                try
                {
                    await _currentSession.LeaveAsync();
                }
                catch (Exception sessionEx)
                {
                    Debug.LogWarning($"[MSM]: セッション退出APIで例外が発生しました (クリーンアップを継続します): {sessionEx.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"ルーム退出処理でエラーが発生しました: {ex.Message}");
        }
        finally
        {
            _currentSession = null;

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }
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