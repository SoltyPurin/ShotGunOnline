using Unity.Netcode;
using UnityEngine;

public class FloorOasis : MonoBehaviour
{
    [SerializeField, Header("消えるまでの時間")]
    private float _destroyTime = 6f;
    private float _initWaitTime = 0;

    [SerializeField, Header("回復床を出す個数")]
    private int _floorSpawnValue = 4;

    [SerializeField, Header("ソウル床を出す座標")]
    private Transform[] _floorSpawnPos;

    [SerializeField, Header("自身のTransform座標からの相対オフセットとして扱うか")]
    private bool _useRelativeOffset = false;

    [SerializeField, Header("ソウル床のプレハブ")]
    private GameObject _soulFloorObject = default;

    private GameObject _currentFloorObject = default;
    private bool _isTaked = false;
    private int _currentObservationIndex = 0;
    private bool _initialFloorSpawned = false;

    private bool IsServerAuthority =>
        NetworkManager.Singleton != null &&
        NetworkManager.Singleton.IsListening &&
        NetworkManager.Singleton.IsServer;

    private void Start()
    {
        TrySpawnInitialFloor();
    }

    private void OnDisable()
    {
        DespawnCurrentFloor();
    }

    private void TrySpawnInitialFloor()
    {
        if (_initialFloorSpawned || !IsServerAuthority) return;

        _initialFloorSpawned = true;
        SpawnNextFloor();
    }

    private Vector2 GetWorldSpawnPosition(int index)
    {
        Vector2 configuredPos = _floorSpawnPos[index].position;
        if (_useRelativeOffset)
        {
            return (Vector2)transform.position + configuredPos;
        }
        return configuredPos;
    }

    private void SpawnNextFloor()
    {
        if (_floorSpawnPos == null || _floorSpawnPos.Length == 0 || _soulFloorObject == null) return;

        Vector2 spawnPos = GetWorldSpawnPosition(_currentObservationIndex);
        _currentFloorObject = Instantiate(_soulFloorObject, spawnPos, Quaternion.identity);

        NetworkObject netObj = _currentFloorObject.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn(); // サーバーから全端末へ同期スポーン
        }
    }

    private void DespawnCurrentFloor()
    {
        if (_currentFloorObject != null && IsServerAuthority)
        {
            NetworkObject prevNetFloor = _currentFloorObject.GetComponent<NetworkObject>();
            if (prevNetFloor != null && prevNetFloor.IsSpawned)
            {
                prevNetFloor.Despawn();
            }
            else
            {
                Destroy(_currentFloorObject);
            }
        }
        _currentFloorObject = null;
        _initialFloorSpawned = false;
    }

    private void FixedUpdate()
    {
        if (!IsServerAuthority) return;

        if (!_initialFloorSpawned)
        {
            TrySpawnInitialFloor();
            return;
        }

        if (_floorSpawnPos == null || _floorSpawnPos.Length == 0 || _soulFloorObject == null) return;

        _initWaitTime += Time.fixedDeltaTime;

        // 踏まれて消えたか制限時間が来た時、床を削除して次の座標へ出す
        if (_currentFloorObject == null || _initWaitTime > _destroyTime)
        {
            if (_currentFloorObject != null)
            {
                NetworkObject prevNetFloor = _currentFloorObject.GetComponent<NetworkObject>();
                if (prevNetFloor != null && prevNetFloor.IsSpawned)
                {
                    prevNetFloor.Despawn();
                }
            }

            _currentObservationIndex++;
            if (_currentObservationIndex >= _floorSpawnPos.Length)
            {
                _currentObservationIndex = 0;
            }

            Vector2 nextPos = GetWorldSpawnPosition(_currentObservationIndex);
            GameObject floor = Instantiate(_soulFloorObject, nextPos, Quaternion.identity);
            _currentFloorObject = floor;
            NetworkObject netFloor = floor.GetComponent<NetworkObject>();
            if (netFloor != null)
            {
                netFloor.Spawn();
            }

            _initWaitTime = 0;
            _isTaked = true;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_floorSpawnPos == null) return;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < _floorSpawnPos.Length; i++)
        {
            Vector2 p = GetWorldSpawnPosition(i);
            Gizmos.DrawWireSphere(p, 0.7f);
        }
    }
}
