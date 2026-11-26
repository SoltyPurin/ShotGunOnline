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
    private const int MaxRoomNameLength = 32;
    private const int MinPlayers = 2;
    private const int MaxPlayers = 8;
    private static readonly TimeSpan LeaveTimeout = TimeSpan.FromSeconds(5);

    private readonly IUnityServicesInitializer _servicesInitializer;
    private readonly RelayConnectionHandler _relayConnectionHandler;
    private readonly Action _onSessionConnected;
    private readonly Action _onSessionLeaving;
    private readonly Action _onUnexpectedDisconnect;

    private ISession _currentSession;
    private CancellationTokenSource _connectionCts;
    private bool _isSessionConnected;
    private bool _isLeavingIntentionally;
    private bool _isConnecting;
    private bool _isLeaving;

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

    public async UniTask<bool> CreateRoomSessionAsync(string roomName, int maxPlayers, RelayProtocol protocol = RelayProtocol.DTLS)
    {
        if (!EnsureServicesInitialized()) return false;
        if (_isConnecting || _isLeaving)
        {
            Debug.LogWarning("[MSM]: 既に接続または退出処理が進行中のため、ルーム作成をスキップしました。");
            return false;
        }

        // 入力値の検証（不正な名前・人数でのAPI呼び出しや想定外の人数でのゲーム開始を防止）
        roomName = roomName?.Trim();
        if (string.IsNullOrEmpty(roomName))
        {
            Debug.LogWarning("[MSM]: ルーム名が空のため、ルーム作成をスキップしました。");
            return false;
        }
        if (roomName.Length > MaxRoomNameLength)
        {
            roomName = roomName.Substring(0, MaxRoomNameLength);
        }
        maxPlayers = Mathf.Clamp(maxPlayers, MinPlayers, MaxPlayers);

        if (_currentSession != null)
        {
            Debug.LogWarning("[MSM]: 既にルームに参加中のため、ルーム作成をスキップしました。");
            return false;
        }

        _isConnecting = true;
        var cancellationToken = BeginConnectionAttempt(protocol);

        try
        {
            // Relay接続が完了するまでは一覧に表示されないよう非公開で作成し、公開プロパティにプロトコルを記録
            var options = new SessionOptions
            {
                Name = roomName,
                MaxPlayers = maxPlayers,
                IsPrivate = true,
                SessionProperties = SessionPlayerPropertyHelper.CreateSessionProperties(protocol, isInGame: false)
            }
            .WithRelayNetwork()
            .WithNetworkOptions(new NetworkOptions { RelayProtocol = protocol });

            var createdSession = await MultiplayerService.Instance.CreateSessionAsync(options);

            // 作成待機中に退出（キャンセル）された場合、作成されたセッションが孤立しないよう即座に退出する
            if (cancellationToken.IsCancellationRequested)
            {
                await LeaveOrphanSessionAsync(createdSession);
                return false;
            }

            _currentSession = createdSession;
            SubscribeSessionEvents(_currentSession);

            // NGOホスト起動およびRelayサーバーとの疎通確立を待機
            if (!await _relayConnectionHandler.StartHostAndVerifyAsync(cancellationToken))
            {
                await LeaveRoomSessionAsync();
                return false;
            }

            if (cancellationToken.IsCancellationRequested || _currentSession == null)
            {
                return false;
            }

            CompleteConnectionAttempt($"ホストとして接続完了 ({protocol})");

            await SaveRelayConnectedPropertyAsync();

            if (cancellationToken.IsCancellationRequested || _currentSession == null)
            {
                return false;
            }

            // Relay接続が確立したのでルームを公開（ルーム一覧に表示されるようにする）
            var hostSession = _currentSession.AsHost();
            hostSession.IsPrivate = false;
            await hostSession.SavePropertiesAsync();

            Debug.Log($"[MSM]: ルーム一覧に公開しました ({protocol})");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"ルーム作成失敗: {ex.Message}");
            await LeaveRoomSessionAsync();
            return false;
        }
        finally
        {
            _isConnecting = false;
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
        if (targetRoom == null || string.IsNullOrEmpty(targetRoom.Id))
        {
            Debug.LogWarning("[MSM]: 参加対象のルーム情報が不正なため、ルーム参加をスキップしました。");
            return false;
        }
        if (_isConnecting || _isLeaving)
        {
            Debug.LogWarning("[MSM]: 既に接続または退出処理が進行中のため、ルーム参加をスキップしました。");
            return false;
        }
        if (_currentSession != null)
        {
            Debug.LogWarning("[MSM]: 既にルームに参加中のため、ルーム参加をスキップしました。");
            return false;
        }

        // 試合中・ロック中のルームへ途中参加すると、プレイヤーが生成されない／開始同期が崩れるため参加させない
        if (targetRoom.IsLocked || SessionPlayerPropertyHelper.IsSessionInGame(targetRoom))
        {
            Debug.LogWarning("[MSM]: 試合中またはロック中のルームのため、参加をスキップしました。");
            return false;
        }

        _isConnecting = true;
        // ルーム一覧情報（ISessionInfo）の公開プロパティから通信プロトコルを自動取得
        var protocol = SessionPlayerPropertyHelper.GetSessionRelayProtocol(targetRoom);
        var cancellationToken = BeginConnectionAttempt(protocol);

        try
        {
            var joinOptions = new JoinSessionOptions()
                .WithNetworkOptions(new NetworkOptions { RelayProtocol = protocol });

            var joinedSession = await MultiplayerService.Instance.JoinSessionByIdAsync(targetRoom.Id, joinOptions);

            // 参加待機中に退出（キャンセル）された場合、参加したセッションが孤立しないよう即座に退出する
            if (cancellationToken.IsCancellationRequested)
            {
                await LeaveOrphanSessionAsync(joinedSession);
                return false;
            }

            _currentSession = joinedSession;
            SubscribeSessionEvents(_currentSession);

            // NGOクライアント起動およびRelay経由での接続確立を待機
            if (!await _relayConnectionHandler.StartClientAndVerifyAsync(cancellationToken))
            {
                await LeaveRoomSessionAsync();
                return false;
            }

            if (cancellationToken.IsCancellationRequested || _currentSession == null)
            {
                return false;
            }

            // 接続直後からネットワークイベントを購読し、UGSプロパティ保存中のゲーム開始・切断を取りこぼさないようにする
            CompleteConnectionAttempt($"クライアントとして接続完了 ({protocol})");

            await SaveRelayConnectedPropertyAsync();

            return _currentSession != null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"ルームに参加失敗: {ex.Message}");
            await LeaveRoomSessionAsync();
            return false;
        }
        finally
        {
            _isConnecting = false;
        }
    }

    public async UniTask LeaveRoomSessionAsync()
    {
        if (_isLeaving) return;
        _isLeaving = true;
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
                    // 通信断などでAPIが応答しない場合に退出処理（およびエラーダイアログ表示）が無期限に停止しないようタイムアウトを設ける
                    await _currentSession.LeaveAsync().AsUniTask().Timeout(LeaveTimeout, DelayType.Realtime);
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
            _isLeaving = false;
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

    public async UniTask UpdatePlayerNameAsync(string playerName)
    {
        if (_currentSession?.CurrentPlayer == null) return;

        try
        {
            var properties = SessionPlayerPropertyHelper.CreatePlayerNameProperties(playerName);
            await SaveCurrentPlayerPropertiesAsync(properties);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[MSM]: プレイヤー名のセッション反映に失敗しました: {ex.Message}");
        }
    }

    public async UniTask UpdatePlayerSkinIdAsync(int skinId)
    {
        if (_currentSession?.CurrentPlayer == null) return;

        try
        {
            var properties = SessionPlayerPropertyHelper.CreateSkinIdProperties(skinId);
            await SaveCurrentPlayerPropertiesAsync(properties);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[MSM]: スキンIDのセッション反映に失敗しました: {ex.Message}");
        }
    }

    private bool EnsureServicesInitialized()
    {
        if (_servicesInitializer.IsInitialized) return true;
        Debug.LogError("Servicesが初期化されていません");
        return false;
    }

    private CancellationToken BeginConnectionAttempt(RelayProtocol protocol)
    {
        CancelPendingConnectionAttempt();
        _connectionCts = new CancellationTokenSource();
        _isSessionConnected = false;
        _isLeavingIntentionally = false;
        _relayConnectionHandler.ConfigureTransport(protocol);
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
        var initialProps = SessionPlayerPropertyHelper.CreateInitialConnectedPlayerProperties(
            PlayerProfileStore.PlayerName,
            PlayerSkinSync.LocalPlayerSkinId);
        await SaveCurrentPlayerPropertiesAsync(initialProps);
    }

    /// <summary>
    /// 接続処理がキャンセルされた後に作成・参加が完了してしまったセッションから退出し、NGOも停止する
    /// </summary>
    private async UniTask LeaveOrphanSessionAsync(ISession orphanSession)
    {
        Debug.LogWarning("[MSM]: 接続処理がキャンセルされたため、作成・参加済みのセッションから退出します。");
        try
        {
            if (orphanSession != null)
            {
                await orphanSession.LeaveAsync().AsUniTask().Timeout(LeaveTimeout, DelayType.Realtime);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[MSM]: 孤立セッションの退出に失敗しました: {ex.Message}");
        }
        finally
        {
            if (_currentSession == null)
            {
                _relayConnectionHandler.Shutdown();
            }
        }
    }

    public async UniTask UpdateSessionGameStatusAsync(bool isInGame)
    {
        if (_currentSession == null || !_currentSession.IsHost) return;

        try
        {
            var hostSession = _currentSession.AsHost();
            var value = isInGame
                ? SessionPlayerPropertyHelper.GameStatusInGameValue
                : SessionPlayerPropertyHelper.GameStatusWaitingValue;
            hostSession.SetProperty(
                SessionPlayerPropertyHelper.GameStatusPropertyKey,
                new SessionProperty(value, VisibilityPropertyOptions.Public));
            // 試合中は新規参加をロックし、途中参加によるプレイヤー未生成・開始同期崩れを防止する
            hostSession.IsLocked = isInGame;
            await hostSession.SavePropertiesAsync();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[MSM]: ゲーム状態の更新に失敗しました: {ex.Message}");
        }
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
