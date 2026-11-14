using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GroundCoin : NetworkBehaviour
{
    private readonly string PLAYER_TAGNAME = "Player";
    private bool _isCollected = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_isCollected) return;

        GameObject colObject = collision.gameObject;
        if (!colObject.CompareTag(PLAYER_TAGNAME)) return;

        SoulKeep soul = colObject.GetComponent<SoulKeep>() ?? colObject.GetComponentInChildren<SoulKeep>() ?? colObject.GetComponentInParent<SoulKeep>();
        if (soul == null) return;

        var playerNetObj = colObject.GetComponent<NetworkObject>() ?? colObject.GetComponentInParent<NetworkObject>();

        // サーバー上で検知した場合
        if (IsServer)
        {
            _isCollected = true;
            soul.AdditionCoin();

            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        else if (playerNetObj != null && playerNetObj.IsLocalPlayer)
        {
            // クライアント自機が接触した場合、サーバーへ回収をリクエスト
            _isCollected = true;
            RequestCollectServerRpc(new NetworkObjectReference(playerNetObj));
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestCollectServerRpc(NetworkObjectReference playerRef)
    {
        if (_isCollected) return;
        _isCollected = true;

        if (playerRef.TryGet(out NetworkObject playerNetObj))
        {
            var soul = playerNetObj.GetComponent<SoulKeep>() ?? playerNetObj.GetComponentInChildren<SoulKeep>();
            if (soul != null)
            {
                soul.AdditionCoin();
            }
        }

        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
