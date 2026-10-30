using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Multiplayer;
using UnityEngine;

/// <summary>
/// NGO (NetworkManager) と UnityTransport (Relay) の起動・疎通検証・停止のみを担当するクラス
/// LobbySessionService からトランスポート層の制御やポーリング待機ロジックを分離
/// </summary>
public class RelayConnectionHandler
{
    private const float DefaultTimeoutSeconds = 15f;
    private const int ConnectTimeoutMs = 1000;
    private const int MaxConnectAttempts = 15;
    private const float CheckIntervalSeconds = 0.1f;
    private const float ClientListenGracePeriodSeconds = 1.0f;

    private readonly float _timeoutSeconds;

    public RelayConnectionHandler(float timeoutSeconds = DefaultTimeoutSeconds)
    {
        _timeoutSeconds = timeoutSeconds;
    }

    /// <summary>
    /// 指定された RelayProtocol に応じて UnityTransport の WebSocket 設定および接続タイムアウトを設定する
    /// </summary>
    public void ConfigureTransport(RelayProtocol protocol)
    {
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.NetworkConfig?.NetworkTransport is UnityTransport transport)
        {
            transport.UseWebSockets = protocol == RelayProtocol.WSS;
            transport.ConnectTimeoutMS = ConnectTimeoutMs;
            transport.MaxConnectAttempts = MaxConnectAttempts;
        }
    }

    /// <summary>
    /// UnityTransport を WebSocket (WSS) モードに設定する
    /// </summary>
    public void ConfigureWebSocketTransport()
    {
        ConfigureTransport(RelayProtocol.WSS);
    }

    /// <summary>
    /// ホストとして NetworkManager を起動し、Relayサーバーとの双方向接続確立 (Established) を待機する。
    /// UDP等がブロックされている環境でホストが孤立しゴーストロビー化することを防ぎます。
    /// </summary>
    public async UniTask<bool> StartHostAndVerifyAsync(CancellationToken cancellationToken = default)
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            Debug.LogError("[MSM]: NetworkManager.Singleton が存在しません。");
            return false;
        }

        if (!networkManager.IsListening && !networkManager.StartHost())
        {
            Debug.LogError("[MSM]: NetworkManager.StartHost() の実行に失敗しました。");
            return false;
        }

        var isConnected = await WaitForConnectionAsync(isHost: true, cancellationToken);
        if (!isConnected)
        {
            Debug.LogError("[MSM]: Relayホスト接続待機または疎通確認に失敗しました。ロビーを破棄します。");
        }

        return isConnected;
    }

    /// <summary>
    /// クライアントとして NetworkManager を起動し、Relay経由でのホスト接続確立を待機する。
    /// </summary>
    public async UniTask<bool> StartClientAndVerifyAsync(CancellationToken cancellationToken = default)
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            Debug.LogError("[MSM]: NetworkManager.Singleton が存在しません。");
            return false;
        }

        if (!networkManager.IsListening && !networkManager.StartClient())
        {
            Debug.LogError("[MSM]: NetworkManager.StartClient() の実行に失敗しました。");
            return false;
        }

        var isConnected = await WaitForConnectionAsync(isHost: false, cancellationToken);
        if (!isConnected)
        {
            Debug.LogError("[MSM]: Relayクライアント接続待機がタイムアウトまたは失敗しました。");
        }

        return isConnected;
    }

    /// <summary>
    /// NetworkManager が稼働中の場合にシャットダウンする
    /// </summary>
    public void Shutdown()
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager != null && (networkManager.IsListening || networkManager.IsServer || networkManager.IsClient))
        {
            networkManager.Shutdown();
        }
    }

    private async UniTask<bool> WaitForConnectionAsync(bool isHost, CancellationToken cancellationToken)
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null) return false;

        var transport = networkManager.NetworkConfig?.NetworkTransport as UnityTransport;
        float startTime = Time.unscaledTime;

        while (Time.unscaledTime - startTime < _timeoutSeconds)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            float elapsedTime = Time.unscaledTime - startTime;
            var status = isHost
                ? CheckHostConnectionStatus(networkManager, transport)
                : CheckClientConnectionStatus(networkManager, elapsedTime);

            if (status == ConnectionCheckResult.Connected)
            {
                return true;
            }

            if (status == ConnectionCheckResult.Failed)
            {
                return false;
            }

            var isCanceled = await UniTask
                .Delay(TimeSpan.FromSeconds(CheckIntervalSeconds), ignoreTimeScale: true, cancellationToken: cancellationToken)
                .SuppressCancellationThrow();

            if (isCanceled)
            {
                return false;
            }
        }

        Debug.LogError($"[MSM]: Relay接続待機がタイムアウトしました ({_timeoutSeconds}秒)。通信が制限されている可能性があります。");
        return false;
    }

    private static ConnectionCheckResult CheckHostConnectionStatus(NetworkManager networkManager, UnityTransport transport)
    {
        if (!networkManager.IsServer || !networkManager.IsListening)
        {
            return ConnectionCheckResult.Pending;
        }

        if (transport == null)
        {
            return ConnectionCheckResult.Connected;
        }

        var driver = transport.GetNetworkDriver();
        if (!driver.IsCreated)
        {
            return ConnectionCheckResult.Pending;
        }

        var relayStatus = driver.GetRelayConnectionStatus();
        switch (relayStatus)
        {
            case RelayConnectionStatus.Established:
                Debug.Log("[MSM]: ホストとRelayサーバーの双方向接続が確立されました (Established)。");
                return ConnectionCheckResult.Connected;

            case RelayConnectionStatus.NotUsingRelay:
                return ConnectionCheckResult.Connected;

            case RelayConnectionStatus.AllocationInvalid:
                Debug.LogError("[MSM]: Relayサーバーへの接続に失敗しました (AllocationInvalid)。ロビーを破棄します。");
                return ConnectionCheckResult.Failed;

            default:
                return ConnectionCheckResult.Pending;
        }
    }

    private static ConnectionCheckResult CheckClientConnectionStatus(NetworkManager networkManager, float elapsedTime)
    {
        if (networkManager.IsConnectedClient)
        {
            return ConnectionCheckResult.Connected;
        }

        // 接続試行中にリッスンが停止した場合（接続拒否・ハンドシェイク失敗など）
        if (!networkManager.IsListening && elapsedTime > ClientListenGracePeriodSeconds)
        {
            Debug.LogWarning("[MSM]: クライアント接続処理中にリッスンが停止しました。");
            return ConnectionCheckResult.Failed;
        }

        return ConnectionCheckResult.Pending;
    }

    private enum ConnectionCheckResult
    {
        Pending,
        Connected,
        Failed
    }
}
