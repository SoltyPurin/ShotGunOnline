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

    [SerializeField, Header("弾の威力")]
    private float _bulletBlowAway = 2f;

    [SerializeField, Header("弾のスプライト")]
    protected GameObject _bullet = default;

    [SerializeField, Header("弾の死亡エフェクト")]
    private GameObject _deathEffect = default;

    protected bool _isDestroyed = false;

    private Vector3 _lastPosition = default;
    private bool _hasLastPosition = false;

    private NetworkObjectReference _pendingPlayerRef;
    private Vector2 _pendingDirection;
    private bool _hasPendingRpc = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // Spawn前にDirectionSettingが呼ばれていた場合、Spawn完了時に全クライアントへRPCを送信
        if (IsServer && _hasPendingRpc)
        {
            _hasPendingRpc = false;
            SetDirectionClientRpc(_pendingPlayerRef, _pendingDirection);
        }
    }

    /// <summary>
    /// プレイヤーの方向に飛ばすために回転する（サーバー上で呼び出し、全クライアントへ同期）
    /// </summary>
    /// <param name="playerObj"></param>
    /// <param name="bulletDirection"></param>
    public virtual void DirectionSetting(GameObject playerObj, Vector2 bulletDirection)
    {
        _direction = bulletDirection;
        float angle = Mathf.Atan2(bulletDirection.y, bulletDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        if (_bullet != null)
        {
            _bullet.transform.localRotation = Quaternion.identity;
        }
        _canMove = true;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            if (!IsServer) return;

            NetworkObject netObj = GetComponent<NetworkObject>();
            NetworkObject playerNetObj = playerObj != null ? playerObj.GetComponent<NetworkObject>() : null;
            NetworkObjectReference pRef = playerNetObj != null ? playerNetObj : default;

            if (netObj != null && netObj.IsSpawned)
            {
                SetDirectionClientRpc(pRef, bulletDirection);
            }
            else
            {
                _pendingPlayerRef = pRef;
                _pendingDirection = bulletDirection;
                _hasPendingRpc = true;
            }
        }
    }

    [ClientRpc]
    protected virtual void SetDirectionClientRpc(NetworkObjectReference playerRef, Vector2 bulletDirection)
    {
        _direction = bulletDirection;
        float angle = Mathf.Atan2(bulletDirection.y, bulletDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        if (_bullet != null)
        {
            _bullet.transform.localRotation = Quaternion.identity;
        }
        _canMove = true;
    }

    public virtual void FixedUpdate()
    {
        if (_isDestroyed || !_canMove)
        {
            return;
        }

        transform.position += (Vector3)_direction * _moveSpeed * Time.fixedDeltaTime;
        _lifetime += Time.fixedDeltaTime;
        if (_lifetime >= _bulletLifeLimitTime)
        {
            if (_deathEffect != null)
            {
                Instantiate(_deathEffect, transform.position, Quaternion.identity);
            }

            DestroyProtocol();
        }
    }

    protected virtual void LateUpdate()
    {
        // クライアント側（ホスト/サーバー以外）ではNetworkTransformによる同期移動中、向きを実際の移動方向に合わせて補正する
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsServer)
        {
            NetworkObject netObj = GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                if (_hasLastPosition)
                {
                    Vector3 delta = transform.position - _lastPosition;
                    if (delta.sqrMagnitude > 0.0001f)
                    {
                        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                        transform.rotation = Quaternion.Euler(0f, 0f, angle);
                        if (_bullet != null)
                        {
                            _bullet.transform.localRotation = Quaternion.identity;
                        }
                    }
                }
                _lastPosition = transform.position;
                _hasLastPosition = true;
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
            // マルチプレイ時、他人のプレイヤーには当たり判定を行わない（各クライアント側で自機の被弾のみ判定）
            NetworkObject netObj = collision.GetComponent<NetworkObject>();
            if (netObj != null && !netObj.IsOwner)
            {
                return;
            }

            PlayerDamageKnockBack damageKnockBack = collision.GetComponent<PlayerDamageKnockBack>();
            if (damageKnockBack != null)
            {
                damageKnockBack.SetDirection(_direction, _bulletBlowAway, this.gameObject);
            }
            if (_deathEffect != null)
            {
                Instantiate(_deathEffect, transform.position, Quaternion.identity);
            }
            DestroyProtocol();
        }
    }

    /// <summary>
    /// 弾の消滅処理（サーバーならDespawn、クライアントならServerRpc要求、画面上は即座に無効化）
    /// </summary>
    public virtual void DestroyProtocol()
    {
        if (_isDestroyed)
        {
            return;
        }
        _isDestroyed = true;

        DisableBulletLocal();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            if (IsServer)
            {
                DisableBulletClientRpc();
            }

            NetworkObject netObj = GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                if (IsServer)
                {
                    netObj.Despawn(true);
                }
                else
                {
                    RequestDestroyBulletServerRpc(netObj);
                }
                return;
            }
        }

        Destroy(this.gameObject);
    }

    [ClientRpc]
    private void DisableBulletClientRpc()
    {
        DisableBulletLocal();
    }

    protected virtual void DisableBulletLocal()
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

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestDestroyBulletServerRpc(NetworkObjectReference bulletRef)
    {
        if (bulletRef.TryGet(out NetworkObject netObj))
        {
            if (netObj != null && netObj.IsSpawned)
            {
                netObj.Despawn(true);
            }
        }
        else if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }
}
