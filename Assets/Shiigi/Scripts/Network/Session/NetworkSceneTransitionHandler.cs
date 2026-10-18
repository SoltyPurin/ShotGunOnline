using System;
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

    /// <summary>
    /// ホストがゲームを開始し、全クライアントへ通知した上でゲームシーンへ遷移する
    /// </summary>
    public void StartMultiplayGame(NetworkManager networkManager)
    {
        if (networkManager != null && networkManager.IsHost)
        {
            Debug.Log("[MSM] ホストがゲームを開始: 即時通知を送信し、シーン遷移を実行します");

            // ゲーム開始を通知(クライアントを接続中ステータスに更新させる)
            if (networkManager.CustomMessagingManager != null)
            {
                using (var writer = new FastBufferWriter(0, Allocator.Temp))
                {
                    networkManager.CustomMessagingManager.SendNamedMessageToAll(
                        GameStartingMessageName,
                        writer,
                        NetworkDelivery.ReliableSequenced);
                }
            }

            networkManager.SceneManager.LoadScene(_gameSceneName, LoadSceneMode.Single);
        }
    }

    private void OnGameStartingMessageReceived(ulong senderClientId, FastBufferReader reader)
    {
        Debug.Log($"[MSM] ホストからのゲーム開始通知を受信しました (Sender: {senderClientId})");
        OnGameStarting?.Invoke();
    }

    private void HandleSceneEvent(SceneEvent sceneEvent)
    {
        // ホストがゲームシーンの読み込みを開始した瞬間、全クライアントにイベントが届く
        if (sceneEvent.SceneEventType == SceneEventType.Load && sceneEvent.SceneName == _gameSceneName)
        {
            Debug.Log($"[MSM] ゲームシーン読み込み開始を検知しました: {sceneEvent.SceneName}");
            OnGameStarting?.Invoke();
        }
    }
}

