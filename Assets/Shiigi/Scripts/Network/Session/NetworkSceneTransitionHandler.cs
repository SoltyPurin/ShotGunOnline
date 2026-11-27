using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ホストによるマルチプレイゲーム開始通知（CustomMessage）とネットワークシーン遷移のみを担当するクラス (SRP)。
/// </summary>
public class NetworkSceneTransitionHandler
{
    private const string DefaultGameSceneName = "MP_GameScene";
    private const string GameStartingMessageName = "GameStarting";

    private readonly string _gameSceneName;
    private readonly List<NetworkObject> _despawnBuffer = new();
    private bool _isTransitioning;
    private bool _hasNotifiedGameStarting;

    public event Action OnGameStarting;

    public NetworkSceneTransitionHandler(string gameSceneName = DefaultGameSceneName)
    {
        _gameSceneName = string.IsNullOrWhiteSpace(gameSceneName) ? DefaultGameSceneName : gameSceneName;
    }

    public void SubscribeEvents(NetworkManager networkManager)
    {
        if (networkManager == null) return;

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
        _isTransitioning = false;
        _hasNotifiedGameStarting = false;

        if (networkManager == null) return;

        if (networkManager.SceneManager != null)
        {
            networkManager.SceneManager.OnSceneEvent -= HandleSceneEvent;
        }

        if (networkManager.CustomMessagingManager != null)
        {
            networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(GameStartingMessageName);
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
            StartMultiplayGameAsync(networkManager).Forget();
        }
    }

    private async UniTaskVoid StartMultiplayGameAsync(NetworkManager networkManager)
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

            if (Camera.main != null && Camera.main.TryGetComponent<LobbyCameraFollow>(out var camFollow))
            {
                camFollow.SetTarget(null);
            }

            // シーン遷移前にロビー用のプレイヤーオブジェクトを明示的にデスポーン（再利用バッファ＋値型EnumeratorでゼロGC）
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

            await UniTask.Yield(PlayerLoopTiming.Update);

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
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer) return;

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
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer) return;

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
