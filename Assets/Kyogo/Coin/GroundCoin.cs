using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GroundCoin : NetworkBehaviour
{
    private readonly string PLAYER_TAGNAME = "Player";
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;
        GameObject colObject = collision.gameObject;

        if (colObject.CompareTag(PLAYER_TAGNAME))
        {
            SoulKeep soul = colObject.GetComponent<SoulKeep>();
            if ((soul != null))
            {
                soul.AdditionCoin();
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
                        RequestDestroySoulServerRpc(netObj);
                    }
                }
                else if (IsServer)
                {
                    Destroy(this.gameObject);
                }

                //Destroy(this.gameObject);
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void RequestDestroySoulServerRpc(NetworkObjectReference bulletRef)
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
