using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ReleaseSoul : NetworkBehaviour
{
    [SerializeField, Header("ソウルのオブジェクト")]
    private GameObject _soulObject = default;
    [SerializeField, Header("出すソウルの数")]
    private int _releaseSoulCount = 7;
    [SerializeField, Header("出生まれてから何秒で消えるか")]
    private float _collectTime = 1;

    private SEManager _se = default;
    private readonly string SEMANAGER_TAG = "SEManager";
    private bool _isReleased = false;
    private bool _isServerReleased = false;

    private void Start()
    {
        ResolveSE();
    }

    private void ResolveSE()
    {
        if (_se == null)
        {
            GameObject seObj = GameObject.FindWithTag(SEMANAGER_TAG);
            if (seObj != null)
            {
                _se = seObj.GetComponent<SEManager>();
            }
        }
    }

    public void ReleaseProtocol()
    {
        if (_isReleased) return;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            if (IsServer)
            {
                ExecuteReleaseOnServer();
            }
            else
            {
                _isReleased = true;
                RequestReleaseServerRpc();
            }
        }
        else
        {
            ExecuteReleaseOffline();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestReleaseServerRpc()
    {
        ExecuteReleaseOnServer();
    }

    private void ExecuteReleaseOnServer()
    {
        if (_isServerReleased) return;
        _isServerReleased = true;
        _isReleased = true;

        PlayStepSEClientRpc();

        for (int i = 0; i < _releaseSoulCount; i++)
        {
            if (_soulObject == null) break;

            GameObject coin = Instantiate(_soulObject, transform.position, Quaternion.identity);

            // NetworkObject が付いている場合はサーバー側でネットワークスポーン
            var netObj = coin.GetComponent<NetworkObject>();
            if (netObj != null && !netObj.IsSpawned)
            {
                netObj.Spawn();
            }

            MoveCoin moveCoin = coin.GetComponent<MoveCoin>();
            if (moveCoin != null)
            {
                moveCoin.ChangeWaitTimeMethod(_collectTime);
            }
        }

        var floorNetObj = GetComponent<NetworkObject>();
        if (floorNetObj != null && floorNetObj.IsSpawned)
        {
            floorNetObj.Despawn();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [ClientRpc]
    private void PlayStepSEClientRpc()
    {
        ResolveSE();
        if (_se != null)
        {
            _se.PlayStepOnSoulFloor();
        }
    }

    private void ExecuteReleaseOffline()
    {
        if (_isReleased) return;
        _isReleased = true;

        for (int i = 0; i < _releaseSoulCount; i++)
        {
            if (_soulObject == null) break;

            GameObject coin = Instantiate(_soulObject, transform.position, Quaternion.identity);
            MoveCoin moveCoin = coin.GetComponent<MoveCoin>();
            if (moveCoin != null)
            {
                moveCoin.ChangeWaitTimeMethod(_collectTime);
            }
        }

        ResolveSE();
        if (_se != null)
        {
            _se.PlayStepOnSoulFloor();
        }

        Destroy(gameObject);
    }
}
