using Unity.Netcode;
using UnityEngine;

public class SyncSpawner : NetworkBehaviour
{
    [SerializeField]
    private GameObject _resultSyncPrefab;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        GameObject obj = Instantiate(_resultSyncPrefab);
        obj.GetComponent<NetworkObject>().Spawn();

        //ZombiResultSync sync = obj.GetComponent<ZombiResultSync>();
        //sync.SendZombieScore();
    }
}
