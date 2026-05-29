using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloorOasis : MonoBehaviour
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
    private GameObject[] _floorObjectList;

    private bool _isTaked = false;

    private int _initObservationIndex = 0;

    private void Start()
    {
        _floorObjectList = new GameObject[_floorSpawnValue];
        for(int i = 0; i<_floorSpawnValue; i++)
        {
            _floorObjectList[i]= Instantiate(_soulFloorObject, _floorSpawnPos[i], Quaternion.identity);
            _floorObjectList[i].SetActive(false);
        }
        _floorObjectList[0].SetActive(true);
    }

    private void FixedUpdate()
    {
        _initWaitTime += Time.fixedDeltaTime;
        if (_floorObjectList[_initObservationIndex] == null && !_isTaked)
        {
            _initWaitTime = 0;
            _isTaked = true;
        }
        if (_initWaitTime > _destroyTime)
        {
            if(_floorObjectList[_initObservationIndex] == null)
            {
                _floorObjectList[_initObservationIndex] = Instantiate(_soulFloorObject, _floorSpawnPos[_initObservationIndex], Quaternion.identity);
                _floorObjectList[_initObservationIndex].SetActive(false);
            }
            else
            {
                _floorObjectList[_initObservationIndex].SetActive(false);
            }
            _initObservationIndex++;
            if (_initObservationIndex >= _floorObjectList.Length)
            {
                _initObservationIndex = 0;
            }
            _initWaitTime = 0;
            _floorObjectList[_initObservationIndex].SetActive(true) ;
            _isTaked = false;
        }
    }
}
