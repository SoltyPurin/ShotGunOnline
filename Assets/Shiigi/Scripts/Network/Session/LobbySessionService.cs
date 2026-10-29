using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Services.Multiplayer;
using UnityEngine;

/// <summary>
/// UGSのMultiplayerServiceを用いたルームの作成・検索・参加・退出・Ready属性更新のみを担当するクラス
/// トランスポート設定およびNGO/Relayの接続確立待機はRelayConnectionHandlerから取得
/// </summary>
public class LobbySessionService : ILobbySessionService
{
    private readonly IUnityServicesInitializer _servicesInitializer;
    private readonly RelayConnectionHandler _relayConnectionHandler;
    private readonly Action _onSessionConnected;
    private readonly Action _onSessionLeaving;
    private readonly Action _onUnexpectedDisconnect;

    private ISession _currentSession;
    private CancellationTokenSource _connectionCts;
    private bool _isSessionConnected;
    private bool _isLeavingIntentionally;

    public ISession CurrentSession => _currentSession;

    public event Action OnSessionChanged;

    public LobbySessionService(
        IUnityServicesInitializer servicesInitializer,
        RelayConnectionHandler relayConnectionHandler = null,
        Action onSessionConnected = null,
        Action onSessionLeaving = null,
        Action onUnexpectedDisconnect = null)
    {
        _servicesInitializer = servicesInitializer ?? throw new ArgumentNullException(nameof(servicesInitializer));
        _relayConnectionHandler = relayConnectionHandler ?? new RelayConnectionHandler();
        _onSessionConnected = onSessionConnected;
        _onSessionLeaving = onSessionLeaving;
        _onUnexpectedDisconnect = onUnexpectedDisconnect;
    }

    public async UniTask<bool> CreateRoomSessionAsync(string roomName, int maxPlayers)
    {
        if (!EnsureServicesInitialized()) return false;

        var cancellationToken = BeginConnectionAttempt();

        try
        {
            // Relay接続が完了するまでは一覧に表示されないよう非公開で作成
            var options = new SessionOptions
            {
                Name = roomName,
                MaxPlayers = maxPlayers,
                IsPrivate = true
            }
            .WithRelayNetwork()
            .WithNetworkOptions(new NetworkOptions { RelayProtocol = RelayProtocol.WSS });

            _currentSession = await MultiplayerService.Instance.CreateSessionAsync(options);
            SubscribeSessionEvents(_currentSession);

            // NGOホスト起動およびRelayサーバーとの疎通確立を待機
            if (!await _relayConnectionHandler.StartHostAndVerifyAsync(cancellationToken))
            {
                await LeaveRoomSessionAsync();
                return false;
            }

            await SaveRelayConnectedPropertyAsync();

            // Relay接続が確立したのでルームを公開（ルーム一覧に表示されるようにする）
            var hostSession = _currentSession.AsHost();
            hostSession.IsPrivate = false;
            await hostSession.SavePropertiesAsync();

            CompleteConnectionAttempt("ホストとして接続完了、ルーム一覧に公開しました (WSS)");
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
        if (!EnsureServicesInitialized())
        {
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
        if (!EnsureServicesInitialized()) return false;

        var cancellationToken = BeginConnectionAttempt();

        try
        {
            var joinOptions = new JoinSessionOptions()
                .WithNetworkOptions(new NetworkOptions { RelayProtocol = RelayProtocol.WSS });

            _currentSession = await MultiplayerService.Instance.JoinSessionByIdAsync(targetRoom.Id, joinOptions);
            SubscribeSessionEvents(_currentSession);

            // NGOクライアント起動およびRelay経由での接続確立を待機
            if (!await _relayConnectionHandler.StartClientAndVerifyAsync(cancellationToken))
            {
                await LeaveRoomSessionAsync();
                return false;
            }

            await SaveRelayConnectedPropertyAsync();

            CompleteConnectionAttempt("クライアントとして接続完了 (WSS)");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"ルームに参加失敗: {ex.Message}");
            await LeaveRoomSessionAsync();
            return false;
        }
    }

    public async UniTask LeaveRoomSessionAsync()
    {
        _isLeavingIntentionally = true;
        _isSessionConnected = false;
        CancelPendingConnectionAttempt();

        try
        {
            _onSessionLeaving?.Invoke();

            if (_currentSession != null)
            {
                UnsubscribeSessionEvents(_currentSession);
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
            _relayConnectionHandler.Shutdown();
            _isLeavingIntentionally = false;
        }
    }

    public async UniTask SetReadyStatusAsync(bool isReady)
    {
        if (_currentSession?.CurrentPlayer == null)
        {
            Debug.LogError("セッションまたはプレイヤーが存在しません");
            return;
        }

        try
        {
            var properties = SessionPlayerPropertyHelper.CreateReadyProperties(isReady);
            await SaveCurrentPlayerPropertiesAsync(properties);
        }
        catch (Exception ex)
        {
            Debug.LogError($"準備状態の更新に失敗しました: {ex.Message}");
        }
    }

    private bool EnsureServicesInitialized()
    {
        if (_servicesInitializer.IsInitialized) return true;
        Debug.LogError("Servicesが初期化されていません");
        return false;
    }

    private CancellationToken BeginConnectionAttempt()
    {
        CancelPendingConnectionAttempt();
        _connectionCts = new CancellationTokenSource();
        _isSessionConnected = false;
        _isLeavingIntentionally = false;
        _relayConnectionHandler.ConfigureWebSocketTransport();
        return _connectionCts.Token;
    }

    private void CompleteConnectionAttempt(string logMessage)
    {
        _isSessionConnected = true;
        _onSessionConnected?.Invoke();
        Debug.Log($"[MSM]: {logMessage}");
    }

    private void CancelPendingConnectionAttempt()
    {
        if (_connectionCts == null) return;
        _connectionCts.Cancel();
        _connectionCts.Dispose();
        _connectionCts = null;
    }

    private async UniTask SaveRelayConnectedPropertyAsync()
    {
        var relayProps = SessionPlayerPropertyHelper.CreateRelayConnectedProperties(true);
        await SaveCurrentPlayerPropertiesAsync(relayProps);
    }

    private async UniTask SaveCurrentPlayerPropertiesAsync(Dictionary<string, PlayerProperty> properties)
    {
        if (_currentSession?.CurrentPlayer == null) return;
        _currentSession.CurrentPlayer.SetProperties(properties);
        await _currentSession.SaveCurrentPlayerDataAsync();
    }

    private void SubscribeSessionEvents(ISession session)
    {
        if (session == null) return;
        UnsubscribeSessionEvents(session);
        session.Changed += HandleSessionChanged;
        session.Deleted += HandleSessionDeleted;
        session.RemovedFromSession += HandleRemovedFromSession;
    }

    private void UnsubscribeSessionEvents(ISession session)
    {
        if (session == null) return;
        session.Changed -= HandleSessionChanged;
        session.Deleted -= HandleSessionDeleted;
        session.RemovedFromSession -= HandleRemovedFromSession;
    }

    private void HandleSessionChanged()
    {
        OnSessionChanged?.Invoke();

        // クライアント側でセッションからホストがいなくなった場合を検知
        if (_isSessionConnected && !_isLeavingIntentionally && _currentSession != null && !_currentSession.IsHost)
        {
            var hostId = _currentSession.Host;
            if (string.IsNullOrEmpty(hostId) || _currentSession.GetPlayer(hostId) == null)
            {
                Debug.LogWarning("[MSM]: セッションからホストが退出したことを検知しました。");
                NotifyUnexpectedDisconnect();
            }
        }
    }

    private void HandleSessionDeleted()
    {
        if (_isSessionConnected && !_isLeavingIntentionally)
        {
            Debug.LogWarning("[MSM]: セッションが削除されたことを検知しました。");
            NotifyUnexpectedDisconnect();
        }
    }

    private void HandleRemovedFromSession()
    {
        if (_isSessionConnected && !_isLeavingIntentionally)
        {
            Debug.LogWarning("[MSM]: セッションから切断・除外されたことを検知しました。");
            NotifyUnexpectedDisconnect();
        }
    }

    private void NotifyUnexpectedDisconnect()
    {
        if (!_isSessionConnected || _isLeavingIntentionally) return;
        _isSessionConnected = false;
        _onUnexpectedDisconnect?.Invoke();
    }
}
