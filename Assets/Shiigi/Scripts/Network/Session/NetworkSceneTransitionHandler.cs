using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ホストによるゲーム開始通知とネットワークシーン遷移を制御する。
/// </summary>
public class NetworkSceneTransitionHandler : IDisposable
{
    private const string DefaultGameSceneName = "MP_GameScene";
    private const string GameStartingMessageName = "GameStarting";

    private readonly string _gameSceneName;
    private readonly List<NetworkObject> _despawnBuffer = new();
    private NetworkManager _networkManager;
    private CancellationTokenSource _transitionCts;
    private bool _isTransitioning;
    private bool _hasNotifiedGameStarting;

    public event Action OnGameStarting;

    public NetworkSceneTransitionHandler(string gameSceneName = DefaultGameSceneName)
    {
        _gameSceneName = string.IsNullOrWhiteSpace(gameSceneName) ? DefaultGameSceneName : gameSceneName;
    }

    public void Dispose()
    {
        CancelPendingTransition();
        UnsubscribeEvents(_networkManager);
    }

    private void CancelPendingTransition()
    {
        if (_transitionCts == null) return;
        _transitionCts.Cancel();
        _transitionCts.Dispose();
        _transitionCts = null;
    }

    public void SubscribeEvents(NetworkManager networkManager)
    {
        if (networkManager == null) return;

        _networkManager = networkManager;
        _isTransitioning = false;
        _hasNotifiedGameStarting = false;

        if (networkManager.SceneManager != null)
        {
            networkManager.SceneManager.OnSceneEvent -= HandleSceneEvent;
            networkManager.SceneManager.OnSceneEvent += HandleSceneEvent;
        }

        if (networkManager.CustomMessagingManager != null)
        {
            networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(GameStartingMessageName);
            networkManager.CustomMessagingManager.RegisterNamedMessageHandler(GameStartingMessageName, OnGameStartingMessageReceived);
        }
    }

    public void UnsubscribeEvents(NetworkManager networkManager)
    {
        CancelPendingTransition();
        _isTransitioning = false;
        _hasNotifiedGameStarting = false;

        var target = networkManager != null ? networkManager : _networkManager;
        _networkManager = null;
        if (target == null) return;

        if (target.SceneManager != null)
        {
            target.SceneManager.OnSceneEvent -= HandleSceneEvent;
        }

        if (target.CustomMessagingManager != null)
        {
            target.CustomMessagingManager.UnregisterNamedMessageHandler(GameStartingMessageName);
        }
    }

    /// <summary>
    /// ホストがゲームを開始し、全クライアントへ通知した上でゲームシーンへ遷移する
    /// </summary>
    public void StartMultiplayGame(NetworkManager networkManager)
    {
        if (networkManager != null && networkManager.IsHost && !_isTransitioning)
        {
            _isTransitioning = true;
            CancelPendingTransition();
            _transitionCts = new CancellationTokenSource();
            StartMultiplayGameAsync(networkManager, _transitionCts.Token).Forget();
        }
    }

    private async UniTaskVoid StartMultiplayGameAsync(NetworkManager networkManager, CancellationToken ct)
    {
        try
        {
            Debug.Log("[MSM] ホストがゲームを開始: 即時通知を送信し、シーン遷移を実行します");

            if (networkManager.CustomMessagingManager != null)
            {
                using var writer = new FastBufferWriter(0, Allocator.Temp);
                var clientIds = networkManager.ConnectedClientsIds;
                for (int i = 0; i < clientIds.Count; i++)
                {
                    ulong clientId = clientIds[i];
                    if (clientId == networkManager.LocalClientId) continue;

                    networkManager.CustomMessagingManager.SendNamedMessage(
                        GameStartingMessageName,
                        clientId,
                        writer,
                        NetworkDelivery.ReliableSequenced);
                }
            }

            var mainCam = Camera.main;
            if (mainCam != null && mainCam.TryGetComponent<LobbyCameraFollow>(out var camFollow))
            {
                camFollow.SetTarget(null);
            }

            // シーン遷移前にロビー用のプレイヤーオブジェクトをデスポーン
            if (networkManager.SpawnManager != null)
            {
                _despawnBuffer.Clear();
                foreach (var netObj in networkManager.SpawnManager.SpawnedObjectsList)
                {
                    if (netObj != null && netObj.IsSpawned && NetworkPlayerSpawner.IsLobbyPlayerObject(netObj))
                    {
                        _despawnBuffer.Add(netObj);
                    }
                }

                for (int i = 0; i < _despawnBuffer.Count; i++)
                {
                    var netObj = _despawnBuffer[i];
                    if (netObj != null && netObj.IsSpawned)
                    {
                        netObj.Despawn(true);
                    }
                }
                _despawnBuffer.Clear();
            }

            bool canceled = await UniTask.Yield(PlayerLoopTiming.Update, ct).SuppressCancellationThrow();
            if (canceled)
            {
                _isTransitioning = false;
                return;
            }

            if (networkManager != null && networkManager.IsHost && networkManager.SceneManager != null)
            {
                Debug.Log($"[MSM] LoadScene({_gameSceneName}) を実行します");
                var status = networkManager.SceneManager.LoadScene(_gameSceneName, LoadSceneMode.Single);
                if (status != SceneEventProgressStatus.Started)
                {
                    Debug.LogWarning($"[MSM] LoadScene({_gameSceneName}) の開始に失敗しました: {status}");
                    _isTransitioning = false;
                }
            }
            else
            {
                _isTransitioning = false;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MSM] シーン遷移処理中に例外が発生しました: {ex.Message}");
            _isTransitioning = false;
        }
    }

    private void OnGameStartingMessageReceived(ulong senderClientId, FastBufferReader reader)
    {
        var nm = _networkManager != null ? _networkManager : NetworkManager.Singleton;
        if (nm != null && nm.IsServer) return;

        if (senderClientId != NetworkManager.ServerClientId)
        {
            Debug.LogWarning($"[MSM] ホスト以外からのゲーム開始通知を破棄しました (Sender: {senderClientId})");
            return;
        }

        Debug.Log($"[MSM] ホストからのゲーム開始通知を受信しました (Sender: {senderClientId})");
        NotifyGameStartingOnce();
    }

    private void HandleSceneEvent(SceneEvent sceneEvent)
    {
        if (sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted && sceneEvent.SceneName == _gameSceneName)
        {
            _isTransitioning = false;
        }

        if (sceneEvent.SceneEventType == SceneEventType.Load && sceneEvent.SceneName == _gameSceneName)
        {
            var nm = _networkManager != null ? _networkManager : NetworkManager.Singleton;
            if (nm != null && nm.IsServer) return;

            Debug.Log($"[MSM] ゲームシーン読み込み開始を検知しました: {sceneEvent.SceneName}");
            NotifyGameStartingOnce();
        }
    }

    private void NotifyGameStartingOnce()
    {
        if (_hasNotifiedGameStarting) return;
        _hasNotifiedGameStarting = true;
        OnGameStarting?.Invoke();
    }
}
