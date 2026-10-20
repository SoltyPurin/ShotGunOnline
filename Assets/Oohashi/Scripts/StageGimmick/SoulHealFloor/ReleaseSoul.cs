using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ReleaseSoul : MonoBehaviour
{
    [SerializeField, Header("ソウルのオブジェクト")]
    private GameObject _soulObject = default;
    [SerializeField, Header("出すソウルの数")]
    private int _releaseSoulCount = 7;
    [SerializeField, Header("出生まれてから何秒で消えるか")]
    private float _collectTime = 1;

    private SEManager _se = default;
    private readonly string SEMANAGER_TAG = "SEManager";

    private void Start()
    {
        GameObject seObj = GameObject.FindWithTag(SEMANAGER_TAG);
        if (seObj != null)
        {
            _se = seObj.GetComponent<SEManager>();
        }
    }

    public void ReleaseProtocol()
    {
        for (int i = 0; i < _releaseSoulCount; i++)
        {
            if (_soulObject == null) break;

            GameObject coin = Instantiate(_soulObject, transform.position, Quaternion.identity);

            // NetworkObject が付いている場合はネットワークスポーン
            var netObj = coin.GetComponent<NetworkObject>();
            if (netObj != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                if (!netObj.IsSpawned)
                {
                    netObj.Spawn();
                }
            }

            MoveCoin moveCoin = coin.GetComponent<MoveCoin>();
            if (moveCoin != null)
            {
                moveCoin.MoveStartClientRpc();
                moveCoin.ChangeWaitTimeMethod(_collectTime);
            }
        }

        if (_se != null)
        {
            _se.PlayStepOnSoulFloor();
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
}
