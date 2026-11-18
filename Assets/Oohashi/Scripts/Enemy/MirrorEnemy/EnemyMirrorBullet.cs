using Unity.Netcode;
using UnityEngine;

public class EnemyMirrorBullet : EnemyShotBullet
{
    private bool _isMirroring = false;
    private float _chargeTime = default;
    private float _baseMoveSpeed = 2f;
    private ulong _mirrorOwnerClientId = ulong.MaxValue;

    [SerializeField, Header("弾の内部HP")]
    protected float _bulletHP = 0.1f;

    [SerializeField, Header("吹き飛ばしの乗算値")]
    private float _forceMultiplier = 4;

    private readonly string PLAYERTAGNAME = "Player";
    private readonly string STONETAGNAME = "Stone";
    private readonly string ARMORTAGNAME = "Armor";
    private readonly string MEDIUMARMORTAGNAME = "MediumArmor";
    private readonly string BOSSTAGNAME = "Boss";

    protected override void Awake()
    {
        base.Awake();
        _baseMoveSpeed = _moveSpeed;
    }

    /// <summary>
    /// 撃った時に銃弾の内部HPを減算し、ネットワーク全体へ反射状態を同期する
    /// </summary>
    /// <param name="chargeTime">チャージ時間</param>
    /// <param name="playerDirection">プレイヤーの見てる方向、反射に使う</param>
    public virtual void DecBulletHP(float chargeTime, Vector2 playerDirection)
    {
        Vector2 normDir = playerDirection.sqrMagnitude > 0.0001f ? playerDirection.normalized : Vector2.right;

        if (!IsSpawned)
        {
            _bulletHP -= chargeTime;
            if (_bulletHP <= 0f)
            {
                Mirror(chargeTime, normDir);
            }
            return;
        }

        if (IsServer)
        {
            ApplyBulletDamageServer(chargeTime, normDir, NetworkManager.ServerClientId);
        }
        else
        {
            // クライアント側でも反射ラグを感じさせないようローカル即時予測反映し、サーバーへ確定要求を送る
            if (_bulletHP - chargeTime <= 0f && !_isMirroring)
            {
                Mirror(chargeTime, normDir);
            }
            RequestDecBulletHPServerRpc(chargeTime, normDir);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestDecBulletHPServerRpc(float chargeTime, Vector2 playerDirection, RpcParams rpcParams = default)
    {
        ApplyBulletDamageServer(chargeTime, playerDirection, rpcParams.Receive.SenderClientId);
    }

    private void ApplyBulletDamageServer(float chargeTime, Vector2 playerDirection, ulong attackerClientId)
    {
        if (_isDestroyed) return;

        _bulletHP -= chargeTime;
        if (_bulletHP <= 0f)
        {
            _mirrorOwnerClientId = attackerClientId;
            Mirror(chargeTime, playerDirection);
            SyncMirrorClientRpc(chargeTime, playerDirection, _moveSpeed);
        }
    }

    [ClientRpc]
    private void SyncMirrorClientRpc(float chargeTime, Vector2 playerDirection, float syncedSpeed)
    {
        if (IsServer) return;
        _isMirroring = true;
        _direction = playerDirection;
        _bulletLifeLimitTime = 3f;
        _moveSpeed = syncedSpeed;
        _chargeTime = chargeTime;
        _canMove = true;
        ResetLifetime();

        float angle = Mathf.Atan2(playerDirection.y, playerDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        if (_bullet != null)
        {
            _bullet.transform.localRotation = Quaternion.identity;
        }
    }

    /// <summary>
    /// 反射のメソッド
    /// </summary>
    /// <param name="chargeTime">チャージ時間</param>
    /// <param name="playerDirection">プレイヤーの見てる方向</param>
    public void Mirror(float chargeTime, Vector2 playerDirection)
    {
        _isMirroring = true;
        _direction = playerDirection;
        _bulletLifeLimitTime = 3f;
        ResetLifetime();

        float powValue = Mathf.Pow(chargeTime, _forceMultiplier);
        float force = Mathf.Lerp(6f, powValue, 0.5f);
        _moveSpeed = _baseMoveSpeed * force;
        _chargeTime = chargeTime;
        _canMove = true;

        float angle = Mathf.Atan2(playerDirection.y, playerDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        if (_bullet != null)
        {
            _bullet.transform.localRotation = Quaternion.identity;
        }
    }

    public override void OnTriggerEnter2D(Collider2D collision)
    {
        if (_isDestroyed) return;

        // 反射前は通常の敵弾としてプレイヤーへの被弾判定のみ行う
        if (!_isMirroring)
        {
            base.OnTriggerEnter2D(collision);
            return;
        }

        // 反射後はプレイヤー自身に誤爆しないように除外し、敵・岩へのダメージ確定はサーバー（またはオフライン）で行う
        if (collision.CompareTag(PLAYERTAGNAME))
        {
            return;
        }

        if (IsSpawned && !IsServer)
        {
            return;
        }

        NetworkObject parentNetObj = collision.GetComponentInParent<NetworkObject>();
        GameObject hitObject = parentNetObj != null ? parentNetObj.gameObject : collision.gameObject;

        if (hitObject.CompareTag(STONETAGNAME))
        {
            StoneBlowAway(hitObject, _chargeTime);
        }
        else if (hitObject.CompareTag(ARMORTAGNAME) || hitObject.CompareTag(MEDIUMARMORTAGNAME))
        {
            ArmorHitObject(hitObject, _chargeTime);
        }
        else if (hitObject.CompareTag(BOSSTAGNAME))
        {
            BossHitObject(hitObject, _chargeTime);
        }
        else
        {
            NormalHitObject(hitObject, _chargeTime);
        }
    }

    private void ApplyMirrorDamageWithAttacker(EnemyTakeDamage takeDamage, float chargeTime)
    {
        if (takeDamage == null) return;
        if (IsSpawned && IsServer && _mirrorOwnerClientId != ulong.MaxValue)
        {
            takeDamage.ResolveAttacker(_mirrorOwnerClientId);
        }
        takeDamage.SetMirrorDamage(chargeTime);
    }

    private void StoneBlowAway(GameObject hitObject, float chargeTime)
    {
        SetKnockBackStone stoneBlowAway = hitObject.GetComponent<SetKnockBackStone>();
        if (stoneBlowAway != null)
        {
            stoneBlowAway.SetDirectionAndForce((Vector2)transform.position, chargeTime, false, false);
            DestroyProtocol();
        }
    }

    private void BossHitObject(GameObject hitObject, float chargeTime)
    {
        BossHP bossHP = hitObject.GetComponent<BossHP>();
        BossKnockBack knockback = hitObject.GetComponent<BossKnockBack>();
        if (knockback != null && bossHP != null)
        {
            knockback.SetDirectionAndForce((Vector2)transform.position, chargeTime, false, false);
            ApplyMirrorDamageWithAttacker(bossHP, chargeTime);
            DestroyProtocol();
        }
    }

    private void ArmorHitObject(GameObject hitObject, float chargeTime)
    {
        ArmorKnockBack armorKnockback = hitObject.GetComponent<ArmorKnockBack>();
        ArmorTakeDamage armorTakeDamage = hitObject.GetComponent<ArmorTakeDamage>();
        if (armorKnockback != null && armorTakeDamage != null)
        {
            armorKnockback.SetDirectionAndForce((Vector2)transform.position, chargeTime, PlayerState.Normal, false, false);
            ApplyMirrorDamageWithAttacker(armorTakeDamage, chargeTime);
            DestroyProtocol();
        }
    }

    private void NormalHitObject(GameObject hitObject, float chargeTime)
    {
        EnemyKnockBack knockback = hitObject.GetComponent<EnemyKnockBack>();
        EnemyTakeDamage takeDamage = hitObject.GetComponent<EnemyTakeDamage>();
        if (knockback != null && takeDamage != null)
        {
            knockback.SetDirectionAndForce((Vector2)transform.position, chargeTime, false, false);
            ApplyMirrorDamageWithAttacker(takeDamage, chargeTime);
            DestroyProtocol();
        }
    }
}
