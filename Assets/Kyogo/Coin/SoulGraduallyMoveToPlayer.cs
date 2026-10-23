using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class SoulGraduallyMoveToPlayer : MonoBehaviour
{
    // プレイヤーが範囲内にいるときは徐々に接近する
    [SerializeField, Header("移動速度")]
    private float _coinMoveSpeed = 2f;

    [SerializeField, Header("プレイヤーを追跡する距離")]
    private float _chaseRange = 3.5f;

    [SerializeField, Header("動いているかのフラグ")]
    private bool _isActive = false;

    private readonly string PLAYER_TAG = "Player";
    private NetworkObject _networkObject;

    private void Awake()
    {
        _networkObject = GetComponent<NetworkObject>();
    }

    public void EnableCollider()
    {
        _isActive = true;

        // クライアント側でローカル追従を滑らかに行うため、吸い込み中はNetworkTransformの位置同期競合を抑制
        var netTransform = GetComponent<NetworkTransform>();
        if (netTransform != null)
        {
            netTransform.enabled = false;
        }
    }

    private void FixedUpdate()
    {
        if (!_isActive) return;

        // シーン内の全プレイヤーから最寄りのプレイヤーを探索（ローカルプレイヤーを優先）
        GameObject[] players = GameObject.FindGameObjectsWithTag(PLAYER_TAG);
        if (players == null || players.Length == 0) return;

        GameObject targetPlayer = null;
        float minDistance = float.MaxValue;
        Vector2 curPos = transform.position;

        foreach (var p in players)
        {
            if (p == null || !p.activeInHierarchy) continue;

            var netObj = p.GetComponent<NetworkObject>() ?? p.GetComponentInParent<NetworkObject>();
            float dist = Vector2.Distance(curPos, p.transform.position);

            // 自機（ローカルプレイヤー）が範囲内にいれば最優先で吸い寄せる
            if (netObj != null && netObj.IsLocalPlayer && dist <= _chaseRange)
            {
                targetPlayer = p;
                break;
            }

            if (dist < minDistance)
            {
                minDistance = dist;
                targetPlayer = p;
            }
        }

        if (targetPlayer != null && Vector2.Distance(curPos, targetPlayer.transform.position) <= _chaseRange)
        {
            Vector2 targetPos = targetPlayer.transform.position;
            Vector2 direction = (targetPos - curPos).normalized;
            transform.position += (Vector3)direction * _coinMoveSpeed * Time.fixedDeltaTime;
        }
    }
}
