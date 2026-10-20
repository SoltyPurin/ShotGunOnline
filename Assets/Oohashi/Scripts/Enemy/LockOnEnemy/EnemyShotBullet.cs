using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EnemyShotBullet : NetworkBehaviour
{

    protected Vector2 _direction = default;

    private bool _canMove = false;

    [SerializeField] protected float _moveSpeed = 2;

    [SerializeField] protected float _bulletLifeLimitTime = 3;

    private float _lifetime = 0.0f;

    [SerializeField, Header("銃弾の吹っ飛び")]
    private float _bulletBlowAway = 2f;

    [SerializeField, Header("銃弾のスプライト")]
    private GameObject _bullet = default;

    [SerializeField, Header("弾消滅時のエフェクト")]
    private GameObject _deathEffect = default;

    /// <summary>
    /// プレイヤーのいる方向に飛ばすために回転させる
    /// </summary>
    /// <param name="bulletDirection"></param>
    public virtual void DirectionSetting(GameObject playerObj,Vector2 bulletDirection)
    {
        _direction = bulletDirection;
        float angle = Mathf.Atan2(bulletDirection.y, bulletDirection.x) * Mathf.Rad2Deg;
        _bullet.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        _canMove = true;
    }
    public virtual void FixedUpdate()
    {
        if (_canMove)
        {
            transform.position += (Vector3)_direction * _moveSpeed * Time.fixedDeltaTime;
            _lifetime += Time.fixedDeltaTime;
            if(_lifetime >= _bulletLifeLimitTime)
            {
                Instantiate(_deathEffect, transform.position, Quaternion.identity);
                GetComponent<NetworkObject>().Despawn(true);
                //Destroy(this.gameObject);
            }
        }
    }

    public virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerDamageKnockBack damageKnockBack = collision.GetComponent<PlayerDamageKnockBack>();
            damageKnockBack.SetDirection(_direction, _bulletBlowAway, this.gameObject);
            Instantiate(_deathEffect, transform.position, Quaternion.identity);
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
                    RequestDestroyBulletServerRpc(netObj);
                }
            }
            else if (IsServer)
            {
                Destroy(this.gameObject);
            }
            //Destroy(this.gameObject);
        }
    }

    [Rpc(SendTo.Server)]
    public void RequestDestroyBulletServerRpc(NetworkObjectReference bulletRef)
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
