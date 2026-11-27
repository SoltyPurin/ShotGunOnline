using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// ネットワーク上のプレイヤー生成（スポーン）、識別ルール、およびスポーン済みプレイヤー一覧の管理を担当するクラス (SRP)。
/// </summary>
public class NetworkPlayerSpawner
{
    public const string LobbyPlayerObjectName = "NetWork Player Variant (Lobby)";
    private const string LobbyPlayerMarker = "(Lobby)";

    private readonly GameObject _playerPrefab;
    private readonly List<NetworkObject> _players = new();

    public IReadOnlyList<NetworkObject> Players => _players;

    public NetworkPlayerSpawner(GameObject playerPrefab)
    {
        _playerPrefab = playerPrefab;
    }

    /// <summary>
    /// 指定された NetworkObject がロビー用プレイヤーかどうかを判定する（識別ロジックの一元化）
    /// </summary>
    public static bool IsLobbyPlayerObject(NetworkObject netObj)
    {
        if (netObj == null) return false;
        return netObj.TryGetComponent<LobbyNetworkPlayer>(out _) ||
               netObj.gameObject.name.IndexOf(LobbyPlayerMarker, StringComparison.Ordinal) >= 0;
    }

    /// <summary>
    /// 指定された NetworkObject がインゲーム本編用プレイヤーかどうかを判定する
    /// </summary>
    public static bool IsInGamePlayerObject(NetworkObject netObj)
    {
        return netObj != null &&
               netObj.IsPlayerObject &&
               !IsLobbyPlayerObject(netObj) &&
               netObj.TryGetComponent<PlayerStateManager>(out _);
    }

    /// <summary>
    /// 自端末に出現している本編プレイヤーオブジェクトを OwnerClientId の昇順で取得する（ホスト・クライアントどちらでも使用可）
    /// </summary>
    public static List<NetworkObject> CollectSpawnedPlayerObjects(NetworkManager networkManager)
    {
        var result = new List<NetworkObject>();
        var spawnManager = networkManager != null ? networkManager.SpawnManager : null;
        if (spawnManager == null) return result;

        // HashSet<NetworkObject> の foreach は値型 Enumerator のためアロケーションなし
        foreach (var netObj in spawnManager.SpawnedObjectsList)
        {
            if (IsInGamePlayerObject(netObj))
            {
                result.Add(netObj);
            }
        }

        result.Sort(static (a, b) => a.OwnerClientId.CompareTo(b.OwnerClientId));
        return result;
    }

    public void RegisterPlayer(NetworkObject player)
    {
        _players.RemoveAll(static p => p == null || !p.IsSpawned);
        if (player != null && !_players.Contains(player))
        {
            _players.Add(player);
        }
    }

    public void UnregisterPlayer(NetworkObject player)
    {
        _players.Remove(player);
    }

    /// <summary>
    /// ホスト/サーバー側で全接続クライアントのプレイヤーをステージ固有のスポーン地点に生成する
    /// </summary>
    public void SpawnAllPlayers(NetworkManager networkManager, ISpawnPointProvider spawnPoints)
    {
        if (networkManager == null || !networkManager.IsServer) return;

        if (_playerPrefab == null)
        {
            Debug.LogError("[MP_GameManager] _playerPrefab がインスペクターで設定されていません！");
            return;
        }

        _players.RemoveAll(static p => p == null || !p.IsSpawned);

        var clients = networkManager.ConnectedClientsList;
        int index = 0;

        for (int i = 0; i < clients.Count; i++)
        {
            var client = clients[i];
            int spawnIndex = index++;

            if (IsInGamePlayerObject(client.PlayerObject))
            {
                Debug.Log($"[MP_GameManager] ClientId {client.ClientId} の本編プレイヤーは既にスポーン済みです。");
                RegisterPlayer(client.PlayerObject);
                continue;
            }

            if (client.PlayerObject != null && client.PlayerObject.IsSpawned)
            {
                Debug.Log($"[MP_GameManager] ClientId {client.ClientId} のロビー用プレイヤーをデスポーンします。");
                client.PlayerObject.Despawn(true);
            }

            Transform spawnPoint = spawnPoints != null ? spawnPoints.GetSpawnPoint(spawnIndex) : null;
            Vector3 pos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
            Quaternion rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

            GameObject playerObj = UnityEngine.Object.Instantiate(_playerPrefab, pos, rot);
            if (!playerObj.TryGetComponent<NetworkObject>(out var netObj))
            {
                Debug.LogError("[MP_GameManager] _playerPrefab に NetworkObject コンポーネントが付いていません！");
                UnityEngine.Object.Destroy(playerObj);
                continue;
            }

            netObj.SpawnAsPlayerObject(client.ClientId, destroyWithScene: true);
            RegisterPlayer(netObj);

            Debug.Log($"[MP_GameManager] ClientId {client.ClientId} の本編プレイヤーをスポーンしました (位置: {pos})");
        }
    }
}
