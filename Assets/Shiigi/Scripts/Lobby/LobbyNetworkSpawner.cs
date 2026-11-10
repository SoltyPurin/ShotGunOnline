using Unity.Netcode;
using UnityEngine;

/// <summary>
/// ロビー（集会所）でのネットワークプレイヤー生成を担当するクラス（サーバー/ホスト側のみ動作）。
/// NetworkManager の PlayerPrefab による自動スポーンは使わず、接続したクライアントごとに手動でスポーンします。
/// </summary>
public class LobbyNetworkSpawner : MonoBehaviour
{
    [Header("ロビー用ネットワークプレイヤー（NetworkPrefabsList への登録が必要）")]
    [SerializeField] private NetworkObject _lobbyPlayerPrefab;

    [Header("スポーン地点（未設定時は StageSpawnPoints.Instance を使用）")]
    [SerializeField] private StageSpawnPoints _spawnPoints;

    private NetworkManager _networkManager;

    private void Start()
    {
        _networkManager = NetworkManager.Singleton;
        if (_networkManager == null)
        {
            Debug.LogWarning("[LobbyNetworkSpawner] NetworkManager が見つかりません。");
            return;
        }

        _networkManager.OnServerStarted += HandleServerStarted;
        _networkManager.OnClientConnectedCallback += HandleClientConnected;

        // 既にホストとして起動済みの場合に備えて、未スポーンのクライアント分を生成する
        if (_networkManager.IsServer)
        {
            SpawnForAllClients();
        }
    }

    private void OnDestroy()
    {
        // NetworkManager は DontDestroyOnLoad のため、シーン破棄時に必ず購読を解除する
        if (_networkManager != null)
        {
            _networkManager.OnServerStarted -= HandleServerStarted;
            _networkManager.OnClientConnectedCallback -= HandleClientConnected;
        }
    }

    private void HandleServerStarted()
    {
        SpawnForAllClients();
    }

    private void HandleClientConnected(ulong clientId)
    {
        SpawnFor(clientId);
    }

    private void SpawnForAllClients()
    {
        foreach (var clientId in _networkManager.ConnectedClientsIds)
        {
            SpawnFor(clientId);
        }
    }

    private void SpawnFor(ulong clientId)
    {
        if (_networkManager == null || !_networkManager.IsServer) return;

        if (_lobbyPlayerPrefab == null)
        {
            Debug.LogError("[LobbyNetworkSpawner] _lobbyPlayerPrefab がインスペクターで設定されていません。");
            return;
        }

        if (!_networkManager.ConnectedClients.TryGetValue(clientId, out var client)) return;

        // 重複スポーン防止（ホストは OnServerStarted と OnClientConnectedCallback の両方が来るため）
        if (client.PlayerObject != null) return;

        int index = IndexOfClient(clientId);
        Transform spawnPoint = GetSpawnPointProvider()?.GetSpawnPoint(index);
        Vector3 pos = spawnPoint != null ? spawnPoint.position : Vector3.zero;

        var playerObj = Instantiate(_lobbyPlayerPrefab, pos, Quaternion.identity);

        // destroyWithScene: true → MP_GameScene への遷移時に自動で破棄され、本編のスポーン処理と競合しない
        playerObj.SpawnAsPlayerObject(clientId, destroyWithScene: true);

        Debug.Log($"[LobbyNetworkSpawner] ClientId {clientId} のロビープレイヤーをスポーンしました (位置: {pos})");
    }

    private int IndexOfClient(ulong clientId)
    {
        var ids = _networkManager.ConnectedClientsIds;
        for (int i = 0; i < ids.Count; i++)
        {
            if (ids[i] == clientId) return i;
        }
        return 0;
    }

    private ISpawnPointProvider GetSpawnPointProvider()
    {
        return _spawnPoints != null ? _spawnPoints : StageSpawnPoints.Instance;
    }
}
