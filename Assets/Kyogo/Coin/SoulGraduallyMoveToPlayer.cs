using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoulGraduallyMoveToPlayer : MonoBehaviour
{
    //プレイヤーが範囲内にいるときはゆっくり接近する
    [SerializeField, Header("移動速度")]
    private float _coinMoveSpeed = 1f;
    [SerializeField, Header("プレイヤーを追跡する距離")]
    private float _chaseRange = 3f;
    [SerializeField,Header("移動していいかのフラグ")]
    private bool _isActive = false;

    private readonly string PLAYER_TAG = "Player";

    private GameObject _playerObjcet = default;

    public void EnableCollider()
    {
        _isActive = true;
        _playerObjcet = GameObject.FindWithTag(PLAYER_TAG);
    }

    private void FixedUpdate()
    {
        if (!_isActive)
        {
            return;
        }
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _chaseRange);
        Transform nearestPlayer = null;
        float minDistance = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag(PLAYER_TAG))
            {
                float dist = Vector2.Distance(transform.position, hit.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearestPlayer = hit.transform;
                }
            }
        }

        // 一番近いプレイヤーに向かって吸い寄せられる
        if (nearestPlayer != null)
        {
            Vector2 direction = (nearestPlayer.position - transform.position).normalized;
            transform.position += (Vector3)direction * _coinMoveSpeed * Time.fixedDeltaTime;
        }
        //if(_playerObjcet == null)
        //{
        //    _playerObjcet = GameObject.FindWithTag("Player");
        //    if(_playerObjcet == null)
        //    {
        //        return;
        //    }
        //}
        //Vector2 playerPos = _playerObjcet.transform.position;
        //Vector2 initPos = this.transform.position;
        //float distance = Vector2.Distance(playerPos,initPos);

        //if(distance <= _chaseRange)
        //{
        //    Vector2 direction = (playerPos - initPos).normalized;
        //    transform.position += (Vector3)direction * _coinMoveSpeed * Time.fixedDeltaTime;
        //}

    }

}
