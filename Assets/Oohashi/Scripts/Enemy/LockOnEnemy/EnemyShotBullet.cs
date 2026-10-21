using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EnemyShotBullet : NetworkBehaviour
{

    protected Vector2 _direction = default;

    protected bool _canMove = false;

    [SerializeField] protected float _moveSpeed = 2;

    [SerializeField] protected float _bulletLifeLimitTime = 3;

    private float _lifetime = 0.0f;

    [SerializeField, Header("銃弾の吹っ飛び")]
    private float _bulletBlowAway = 2f;

    [SerializeField, Header("銃弾のスプライト")]
    private GameObject _bullet = default;

    [SerializeField, Header("弾消滅時のエフェクト")]
    private GameObject _deathEffect = default;

    // 破棄処理中かどうかを判定するフラグ
    protected bool _isDestroyed = false;

    ///// <summary>
    ///// プレイヤーのいる方向に飛ばすために回転させる
    ///// </summary>
    ///// <param name="bulletDirection"></param>
    //[ClientRpc]
    //public virtual void DirectionSettingClientRpc(GameObject playerObj,Vector2 bulletDirection)
    //{
    //    _direction = bulletDirection;
    //    float angle = Mathf.Atan2(bulletDirection.y, bulletDirection.x) * Mathf.Rad2Deg;
    //    _bullet.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    //    _canMove = true;
    //}

    /// <summary>
    /// サーバー上で呼び出し、全端末（クライアント含む）へ方向と移動許可（_canMove）を同期する
    /// </summary>
    public virtual void DirectionSetting(GameObject playerObj, Vector2 bulletDirection)
    {
        if (!IsServer) return;

        NetworkObject playerNetObj = playerObj != null ? playerObj.GetComponent<NetworkObject>() : null;
        SetDirectionClientRpc(playerNetObj != null ? playerNetObj : default, bulletDirection);
    }

    [ClientRpc]
    protected virtual void SetDirectionClientRpc(NetworkObjectReference playerRef, Vector2 bulletDirection)
    {
        _direction = bulletDirection;
        _canMove = true;

        if (_bullet != null)
        {
            float angle = Mathf.Atan2(bulletDirection.y, bulletDirection.x) * Mathf.Rad2Deg;
            _bullet.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
    public virtual void FixedUpdate()
    {
        if (_isDestroyed || !_canMove)
        {
            return;
        }
        if (_canMove)
        {
            transform.position += (Vector3)_direction * _moveSpeed * Time.fixedDeltaTime;
            _lifetime += Time.fixedDeltaTime;
            if(_lifetime >= _bulletLifeLimitTime)
            {
                Instantiate(_deathEffect, transform.position, Quaternion.identity);
                //GetComponent<NetworkObject>().Despawn(true);
                //Destroy(this.gameObject);
                DestroyProtocol();
            }
        }
    }

    public virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (_isDestroyed)
        {
            return;
        }
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerDamageKnockBack damageKnockBack = collision.GetComponent<PlayerDamageKnockBack>();
            damageKnockBack.SetDirection(_direction, _bulletBlowAway, this.gameObject);
            Instantiate(_deathEffect, transform.position, Quaternion.identity);
            DestroyProtocol();
        }
    }

    public virtual void DestroyProtocol()
    {
        if (_isDestroyed)
        {
            return;
        }
        _isDestroyed = true;

        DisableBulletLocal();
        DisableBulletClientRpc();
        Debug.Log("銃弾削除呼び出し");
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
        else
        {
            Destroy(this.gameObject);
        }
    }

    /// <summary>
    /// 全クライアント端末上で当たり判定と見た目を即座にオフにする RPC
    /// </summary>
    [ClientRpc]
    private void DisableBulletClientRpc()
    {
        DisableBulletLocal();
    }

    /// <summary>
    /// ローカル端末上の当たり判定・移動・スプライトを即座に停止する
    /// </summary>
    private void DisableBulletLocal()
    {
        _isDestroyed = true;
        _canMove = false;

        Collider2D[] cols = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D col in cols)
        {
            col.enabled = false;
        }

        if (_bullet != null)
        {
            _bullet.SetActive(false);
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
