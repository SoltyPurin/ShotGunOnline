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
    private GameObject _cachedTargetPlayer;
    private float _retargetTimer = 0f;
    private const float RETARGET_INTERVAL = 0.2f;

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

        Vector2 curPos = transform.position;
        _retargetTimer -= Time.fixedDeltaTime;

        if (_retargetTimer <= 0f || _cachedTargetPlayer == null || !_cachedTargetPlayer.activeInHierarchy)
        {
            _retargetTimer = RETARGET_INTERVAL;
            _cachedTargetPlayer = FindBestTargetPlayer(curPos);
        }

        if (_cachedTargetPlayer != null && Vector2.Distance(curPos, _cachedTargetPlayer.transform.position) <= _chaseRange)
        {
            Vector2 targetPos = _cachedTargetPlayer.transform.position;
            Vector2 direction = (targetPos - curPos).normalized;
            transform.position += (Vector3)direction * _coinMoveSpeed * Time.fixedDeltaTime;
        }
    }

    private GameObject FindBestTargetPlayer(Vector2 curPos)
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag(PLAYER_TAG);
        if (players == null || players.Length == 0) return null;

        GameObject targetPlayer = null;
        float minDistance = float.MaxValue;

        foreach (var p in players)
        {
            if (p == null || !p.activeInHierarchy) continue;

            var netObj = p.GetComponent<NetworkObject>() ?? p.GetComponentInParent<NetworkObject>();
            float dist = Vector2.Distance(curPos, p.transform.position);

            // 自機（ローカルプレイヤー）が範囲内にいれば最優先で吸い寄せる
            if (netObj != null && netObj.IsLocalPlayer && dist <= _chaseRange)
            {
                return p;
            }

            if (dist < minDistance)
            {
                minDistance = dist;
                targetPlayer = p;
            }
        }

        return targetPlayer;
    }
}
