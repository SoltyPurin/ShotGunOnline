using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EnemyShotBullet : MonoBehaviour
{

    protected Vector2 _direction = default;

    private bool _canMove = false;

    [SerializeField] protected float _moveSpeed = 2;

    [SerializeField] protected float _bulletLifeLimitTime = 3;

    private float _lifetime = 0.0f;

    [SerializeField, Header("弾の威力")]
    private float _bulletBlowAway = 2f;

    [SerializeField, Header("弾のスプライト")]
    private GameObject _bullet = default;

    [SerializeField, Header("弾の死亡エフェクト")]
    private GameObject _deathEffect = default;

    private Vector3 _lastPosition = default;
    private bool _hasLastPosition = false;

    /// <summary>
    /// プレイヤーの方向に飛ばすために回転する
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
    }

    public virtual void FixedUpdate()
    {
        if (_canMove)
        {
            transform.position += (Vector3)_direction * _moveSpeed * Time.fixedDeltaTime;
            _lifetime += Time.fixedDeltaTime;
            if (_lifetime >= _bulletLifeLimitTime)
            {
                if (_deathEffect != null)
                {
                    Instantiate(_deathEffect, transform.position, Quaternion.identity);
                }
                Destroy(this.gameObject);
            }
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
            Destroy(this.gameObject);
        }
    }
}
