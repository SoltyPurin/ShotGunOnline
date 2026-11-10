using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// ネットワーク上のプレイヤー生成（スポーン）およびスポーン済みプレイヤー一覧の管理のみを担当するクラス (SRP)
/// </summary>
public class NetworkPlayerSpawner
{
    private readonly GameObject _playerPrefab;
    private readonly List<NetworkObject> _players = new();

    public IReadOnlyList<NetworkObject> Players => _players;

    public NetworkPlayerSpawner(GameObject playerPrefab)
    {
        _playerPrefab = playerPrefab;
    }

    /// <summary>
    /// 自端末に出現しているプレイヤーオブジェクトを OwnerClientId の昇順で取得する（ホスト・クライアントどちらでも使用可）
    /// </summary>
    public static List<NetworkObject> CollectSpawnedPlayerObjects(NetworkManager networkManager)
    {
        var result = new List<NetworkObject>();
        var spawnManager = networkManager != null ? networkManager.SpawnManager : null;
        if (spawnManager == null) return result;

        foreach (var netObj in spawnManager.SpawnedObjectsList)
        {
            // 本編用プレイヤー（PlayerStateManager所持）のみを対象とする（ロビー用プレイヤー等の残骸を除外）
            if (netObj != null && netObj.IsPlayerObject && netObj.GetComponent<PlayerStateManager>() != null)
            {
                result.Add(netObj);
            }
        }

        result.Sort((a, b) => a.OwnerClientId.CompareTo(b.OwnerClientId));
        return result;
    }

    public void RegisterPlayer(NetworkObject player)
    {
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

        var clients = networkManager.ConnectedClientsList;
        int index = 0;

        foreach (var client in clients)
        {
            // 本編用プレイヤー（PlayerStateManager所持）が既に存在するか確認
            bool hasValidInGamePlayer = client.PlayerObject != null &&
                                        client.PlayerObject.GetComponent<PlayerStateManager>() != null;

            if (hasValidInGamePlayer)
            {
                Debug.Log($"[MP_GameManager] ClientId {client.ClientId} の本編プレイヤーは既にスポーン済みです。");
                RegisterPlayer(client.PlayerObject);
                continue;
            }

            // ロビー用の古いPlayerObject（LobbyNetworkPlayer等）が残っている場合はデスポーンして除去
            if (client.PlayerObject != null && client.PlayerObject.IsSpawned)
            {
                Debug.Log($"[MP_GameManager] ClientId {client.ClientId} のロビー用プレイヤーをデスポーンします。");
                client.PlayerObject.Despawn(true);
            }

            Transform spawnPoint = spawnPoints != null ? spawnPoints.GetSpawnPoint(index) : null;
            Vector3 pos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
            Quaternion rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

            GameObject playerObj = Object.Instantiate(_playerPrefab, pos, rot);
            var netObj = playerObj.GetComponent<NetworkObject>();

            // クライアントの所有権を付与してネットワーク上にスポーン
            netObj.SpawnAsPlayerObject(client.ClientId, destroyWithScene: true);
            RegisterPlayer(netObj);

            Debug.Log($"[MP_GameManager] ClientId {client.ClientId} の本編プレイヤーをスポーンしました (位置: {pos})");
            index++;
        }
    }
}

