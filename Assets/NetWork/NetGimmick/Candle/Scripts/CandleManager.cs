using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

//このクラスでやること
//感圧板のマネージャーとほぼ同じ
public class CandleManager : NetworkBehaviour
{

    [SerializeField, Header("全部着火させたらそのままにしとく？")]
    private bool _isAllIgnited = false;

    private List<ClearFunctionBaseClass> _clearFunctionObject = new List<ClearFunctionBaseClass>();

    private List<CandleTakeFire> _candleList = new List<CandleTakeFire>();

    // 現在、全てのキャンドルが点火しているか
    private bool _wasAllIgnited = false;

    // 仕掛けがクリア済みか
    private bool _isSolved = false;

    private void Awake()
    {
        _clearFunctionObject.AddRange(GetComponentsInChildren<ClearFunctionBaseClass>());
        _candleList.AddRange(GetComponentsInChildren<CandleTakeFire>());
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
            foreach (CandleTakeFire candle in _candleList)
            {
                candle.IsCandleTakeFire += CheckAllChildState;
            }
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
            foreach (CandleTakeFire candle in _candleList)
            {
                candle.IsCandleTakeFire -= CheckAllChildState;
            }

    }
    private void CheckAllChildState(bool newState)
    {
        if (!IsServer || _isSolved)
        {
            return;
        }

        bool isAllTrue = _candleList.TrueForAll(
            candle => candle != null && candle.IsFireOn
        );

        // 全点火状態になった瞬間
        if (isAllTrue && !_wasAllIgnited)
        {
            _wasAllIgnited = true;

            Debug.Log("全キャンドル点火！");

            if (_isAllIgnited)
            {
                // 全点火維持型
                _isSolved = true;

                foreach (CandleTakeFire candle in _candleList)
                {
                    if (candle != null)
                    {
                        candle.ClearJudgementConfirmed();
                    }
                }

                foreach (ClearFunctionBaseClass gimmick in _clearFunctionObject)
                {
                    if (gimmick != null)
                    {
                        gimmick.Function();
                    }
                }
            }
            else
            {
                // 時間制限型
                foreach (ClearFunctionBaseClass gimmick in _clearFunctionObject)
                {
                    if (gimmick != null)
                    {
                        gimmick.SyncGimmickFunction(true);
                    }
                }
            }
        }
        // 全点火状態が解除された瞬間
        else if (!isAllTrue && _wasAllIgnited)
        {
            _wasAllIgnited = false;

            Debug.Log("全点火状態が解除されました");

            if (!_isAllIgnited)
            {
                foreach (ClearFunctionBaseClass gimmick in _clearFunctionObject)
                {
                    if (gimmick != null)
                    {
                        gimmick.SyncGimmickFunction(false);
                    }
                }
            }
        }
        //if (!IsServer)
        //{
        //    return;
        //}
        //bool isAllTrue = _candleList.TrueForAll(candle => candle.IsFireOn);
        //if (isAllTrue)
        //{
        //    if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsServer)
        //    {
        //        if (_isAllIgnited)
        //        {
        //            foreach (CandleTakeFire candle in _candleList)
        //            {
        //                if (candle != null)
        //                {
        //                    candle.ClearJudgementConfirmed();
        //                }
        //            }
        //            foreach (ClearFunctionBaseClass clFunc in _clearFunctionObject)
        //            {
        //                if (clFunc != null)
        //                {
        //                    clFunc.Function();
        //                }
        //            }
        //        }
        //        else
        //        {
        //            foreach (ClearFunctionBaseClass clFunc in _clearFunctionObject)
        //            {
        //                if (clFunc != null)
        //                {
        //                    clFunc.SyncGimmickFunction(true);
        //                }
        //            }
        //        }
        //    }
        //}

    }
}
