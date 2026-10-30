using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CollisionDetect : MonoBehaviour
{
    private readonly string PLAYER_TAG = "Player";
    private ReleaseSoul _releaseSoul = default;
    [SerializeField, Header("エフェクト")]
    private GameObject _effect = default;

    private void Start()
    {
        _releaseSoul = GetComponent<ReleaseSoul>(); 
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        GameObject obj = collision.gameObject;
        if (obj.CompareTag(PLAYER_TAG))
        {
            // マルチプレイ時、他人のプレイヤーのすれ違いで誤発火しないようローカルプレイヤーまたはサーバー判定
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkObject playerNetObj = obj.GetComponent<NetworkObject>() ?? obj.GetComponentInParent<NetworkObject>();
                if (playerNetObj != null && !playerNetObj.IsLocalPlayer && !NetworkManager.Singleton.IsServer)
                {
                    return;
                }
            }

            if (_effect != null)
            {
                GameObject effect = Instantiate(_effect);
                effect.transform.position = transform.position;
            }
            if (_releaseSoul != null)
            {
                _releaseSoul.ReleaseProtocol();
            }
        }
    }
}
