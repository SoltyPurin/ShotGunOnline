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
        Vector2 playerPos = _playerObjcet.transform.position;
        Vector2 initPos = this.transform.position;
        float distance = Vector2.Distance(playerPos,initPos);

        if(distance <= _chaseRange)
        {
            Vector2 direction = (playerPos - initPos).normalized;
            transform.position += (Vector3)direction * _coinMoveSpeed * Time.fixedDeltaTime;
        }

    }

}
