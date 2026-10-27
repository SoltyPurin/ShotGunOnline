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
            if (netObj != null && netObj.IsPlayerObject)
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
            // 既にそのクライアント用の PlayerObject が存在する場合はスキップ（重複防止）
            if (client.PlayerObject != null)
            {
                Debug.Log($"[MP_GameManager] ClientId {client.ClientId} のプレイヤーは既にスポーン済みです。");
                RegisterPlayer(client.PlayerObject);
                continue;
            }

            Transform spawnPoint = spawnPoints != null ? spawnPoints.GetSpawnPoint(index) : null;
            Vector3 pos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
            Quaternion rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

            GameObject playerObj = Object.Instantiate(_playerPrefab, pos, rot);
            var netObj = playerObj.GetComponent<NetworkObject>();

            // クライアントの所有権を付与してネットワーク上にスポーン
            netObj.SpawnAsPlayerObject(client.ClientId, destroyWithScene: true);
            RegisterPlayer(netObj);

            Debug.Log($"[MP_GameManager] ClientId {client.ClientId} のプレイヤーをスポーンしました (位置: {pos})");
            index++;
        }
    }
}

