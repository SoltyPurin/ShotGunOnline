using System;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ホストによるマルチプレイゲーム開始通知（CustomMessage）とネットワークシーン遷移のみを担当するクラス (SRP)
/// </summary>
public class NetworkSceneTransitionHandler
{
    private const string DefaultGameSceneName = "MP_GameScene";
    private const string GameStartingMessageName = "GameStarting";

    private readonly string _gameSceneName;

    public event Action OnGameStarting;

    public NetworkSceneTransitionHandler(string gameSceneName = DefaultGameSceneName)
    {
        _gameSceneName = string.IsNullOrWhiteSpace(gameSceneName) ? DefaultGameSceneName : gameSceneName;
    }

    public void SubscribeEvents(NetworkManager networkManager)
    {
        if (networkManager == null) return;

        // NGOシーンイベントの監視（フォールバック用）
        if (networkManager.SceneManager != null)
        {
            networkManager.SceneManager.OnSceneEvent -= HandleSceneEvent;
            networkManager.SceneManager.OnSceneEvent += HandleSceneEvent;
        }

        // CustomMessageの監視（即時性用）
        if (networkManager.CustomMessagingManager != null)
        {
            networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(GameStartingMessageName);
            networkManager.CustomMessagingManager.RegisterNamedMessageHandler(GameStartingMessageName, OnGameStartingMessageReceived);
        }
    }

    public void UnsubscribeEvents(NetworkManager networkManager)
    {
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

    private bool _isTransitioning;

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

            // ゲーム開始をリモートクライアントへ通知（ホスト自身へは送らない）
            if (networkManager.CustomMessagingManager != null)
            {
                using (var writer = new FastBufferWriter(0, Allocator.Temp))
                {
                    foreach (var clientId in networkManager.ConnectedClientsIds)
                    {
                        if (clientId == networkManager.LocalClientId) continue;

                        networkManager.CustomMessagingManager.SendNamedMessage(
                            GameStartingMessageName,
                            clientId,
                            writer,
                            NetworkDelivery.ReliableSequenced);
                    }
                }
            }

            // カメラやアバター管理が破棄中オブジェクトを参照しないよう事前にターゲット解除
            if (Camera.main != null && Camera.main.TryGetComponent<LobbyCameraFollow>(out var camFollow))
            {
                camFollow.SetTarget(null);
            }

            // シーン遷移前にロビー用のプレイヤーオブジェクトを明示的にデスポーンし、
            // NGOのシングルシーン遷移（MoveObjectsToDontDestroyOnLoad）でのサーバー権限Despawn競合を防止する
            if (networkManager.SpawnManager != null)
            {
                var spawnedList = new System.Collections.Generic.List<NetworkObject>(networkManager.SpawnManager.SpawnedObjectsList);
                foreach (var netObj in spawnedList)
                {
                    if (netObj != null && netObj.IsSpawned && netObj.GetComponent<LobbyNetworkPlayer>() != null)
                    {
                        netObj.Despawn(true);
                    }
                }
            }

            // Despawn によるオブジェクト破棄とネットワークキュー処理が完了するまで1フレーム待機してから LoadScene を実行する
            await UniTask.Yield(PlayerLoopTiming.Update);

            if (networkManager != null && networkManager.IsHost && networkManager.SceneManager != null)
            {
                Debug.Log($"[MSM] LoadScene({_gameSceneName}) を実行します");
                networkManager.SceneManager.LoadScene(_gameSceneName, LoadSceneMode.Single);
            }
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    private void OnGameStartingMessageReceived(ulong senderClientId, FastBufferReader reader)
    {
        // サーバー（ホスト）自身はStartMultiplayGameで直接開始しているため無視
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer) return;

        Debug.Log($"[MSM] ホストからのゲーム開始通知を受信しました (Sender: {senderClientId})");
        OnGameStarting?.Invoke();
    }

    private void HandleSceneEvent(SceneEvent sceneEvent)
    {
        // ホストがゲームシーンの読み込みを開始した瞬間、全クライアントにイベントが届く
        // （ホスト自身は二重処理を防ぐためスキップ）
        if (sceneEvent.SceneEventType == SceneEventType.Load && sceneEvent.SceneName == _gameSceneName)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer) return;

            Debug.Log($"[MSM] ゲームシーン読み込み開始を検知しました: {sceneEvent.SceneName}");
            OnGameStarting?.Invoke();
        }
    }
}

