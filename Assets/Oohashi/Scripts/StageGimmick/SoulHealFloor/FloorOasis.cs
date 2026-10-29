using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class FloorOasis : NetworkBehaviour
{
    [SerializeField, Header("消えるまでの時間")]
    private float _destroyTime = 6f;
    private float _initWaitTime = 0;
    [SerializeField, Header("回復床を出す個数")]
    private int _floorSpawnValue = 4;
    [SerializeField, Header("ソウル床を出す座標")]
    private Vector2[] _floorSpawnPos;

    [SerializeField, Header("ソウル床のプレハブ")]
    private GameObject _soulFloorObject = default;

    private GameObject _currentFloorObject = default;

    private bool _isTaked = false;

    private int _currentObservationIndex = 0;

    public override void OnNetworkSpawn()
    {
        // サーバー（ホスト）のみが出現管理を行う
        if (!IsServer) return;

        SpawnNextFloor();
    }

    private void SpawnNextFloor()
    {
        if (_floorSpawnPos == null || _floorSpawnPos.Length == 0 || _soulFloorObject == null) return;

        Vector2 spawnPos = _floorSpawnPos[_currentObservationIndex];
        _currentFloorObject = Instantiate(_soulFloorObject, spawnPos, Quaternion.identity);

        NetworkObject netObj = _currentFloorObject.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn(); // サーバーから全端末へ同期スポーン
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;
        _initWaitTime += Time.fixedDeltaTime;
        //踏まれて消えたか制限時間が来た時、床を削除する
        if (_currentFloorObject == null || _initWaitTime > _destroyTime)
        {

            if(_currentFloorObject != null)
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

            
            GameObject floor = Instantiate(_soulFloorObject, _floorSpawnPos[_currentObservationIndex],Quaternion.identity);
            _currentFloorObject = floor;
            NetworkObject netFloor = floor.GetComponent<NetworkObject>();
            netFloor.Spawn();

            _initWaitTime = 0;
            _isTaked = true;
        }
        //if (_initWaitTime > _destroyTime)
        //{
        //    if(_floorObjectList[_currentObservationIndex] == null)
        //    {
        //        GameObject floorObj = Instantiate(_soulFloorObject, _floorSpawnPos[_currentObservationIndex], Quaternion.identity);
        //        NetworkObject netFloor = floorObj.GetComponent<NetworkObject>();
        //        _floorObjectList[_currentObservationIndex] =floorObj;
        //        _floorObjectList[_currentObservationIndex].SetActive(false);
        //    }
        //    else
        //    {
        //        _floorObjectList[_currentObservationIndex].SetActive(false);
        //    }
        //    _currentObservationIndex++;
        //    if (_currentObservationIndex >= _floorObjectList.Length)
        //    {
        //        _currentObservationIndex = 0;
        //    }
        //    _initWaitTime = 0;
        //    _floorObjectList[_currentObservationIndex].SetActive(true) ;
        //    _isTaked = false;
        //}
    }
}
