using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ReleaseSoul : NetworkBehaviour
{
    [SerializeField, Header("生成するソウルのオブジェクト")]
    private GameObject _soulObject = default;
    [SerializeField, Header("何個ソウルを出すか")]
    private int _releaseSoulCount = 7;
    [SerializeField, Header("バラまいてから何秒で回収させるか")]
    private float _collectTime = 1;

    private SEManager _se = default;

    private readonly string SEMANAGER_TAG = "SEManager";

    private void Start()
    {
        _se = GameObject.FindWithTag(SEMANAGER_TAG).GetComponent<SEManager>();
    }
    public void ReleaseProtocol()
    {
        if (!IsServer)
        {
            return;
        }
        for (int i = 0; i < _releaseSoulCount; i++)
        {
            GameObject coin = Instantiate(_soulObject, transform.position, Quaternion.identity);
            NetworkObject netCoin = coin.GetComponent<NetworkObject>();
            netCoin.Spawn();
            MoveCoin moveCoin = coin.GetComponent<MoveCoin>();
            if(moveCoin != null )
            {
                //moveCoin.MoveStartClientRpc();
                moveCoin.ChangeWaitTimeMethod(_collectTime);
                Debug.Log("ソウル移動命令実行");
            }
            else
            {
                Debug.Log("移動ソウルが無いよ");
            }
        }

        NetworkObject netObj = this.GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            if (IsServer)
            {
                netObj.Despawn(true); // サーバーで Despawn すれば全クライアントから消える
            }
            else
            {
                // クライアントからはサーバーへ消去依頼の ServerRpc を送る（必要に応じて）
                RequestDestroySoulFloorServerRpc(netObj);
            }
        }
        else if (IsServer)
        {
            Destroy(this.gameObject);
        }

        _se.PlayStepOnSoulFloor();
    }

    [Rpc(SendTo.Server)]
    public void RequestDestroySoulFloorServerRpc(NetworkObjectReference bulletRef)
    {
        if (bulletRef.TryGet(out NetworkObject netObj))
        {
            if (netObj.IsSpawned)
            {
                netObj.Despawn(true);
            }
        }
    }

}
