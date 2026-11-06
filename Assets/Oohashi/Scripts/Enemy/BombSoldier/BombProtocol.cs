using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BombProtocol : NetworkBehaviour
{
    private bool _isBlowing = false;    //吹っ飛んでるかを判断
    public bool IsBlowing
    {
        set { _isBlowing = value; } //BombKnockBackから書き換える
    }

    [Header("爆発の力")]
    [SerializeField] private float _bombPower = 5f;
    [SerializeField, Header("爆発で敵に与えるダメージ")]
    private float _bombEnemyPower = 2f;
    [SerializeField, Header("爆発で弱点に与えるダメージ")]
    private float _weekDamage = 4;
    [Header("ダメージ処理を行うスクリプト")]
    [SerializeField] private BombTakeDamage _takeDamage = default;
    [SerializeField, Header("実際に拡大させるオブジェクト")]
    private GameObject _scaleObj = default;

    //プレイヤーに触れたときに爆発するまでの猶予時間
    private float _bombWaitTime = 1f;

    private float _initTime = 0;

    private bool _isColPlayer = false;

    private GameObject _collision = default;

    private SpriteRenderer _spriteRenderer = default;

    private NavMesh2DAgent _agent = default;

    private Material _material = default;

    private PlayTheBombEffect _playTheBombEffect = default;

    private SEManager _playeTheSEManager = default;

    private readonly string PLAYERTAGNAME = "Player";

    private readonly string EFFECTTAGNAME = "EffectManager";

    private readonly string SETAGNAME = "SEManager";

    private readonly string BOMBTAGNAME = "Bomb";

    private bool _isExplosion = false;
    public bool IsExplosion
    {
        get { return _isExplosion; }
    }

    private void Start()
    {
        FindManagers();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (_spriteRenderer != null)
        {
            _material = _spriteRenderer.material;
        }
        _agent = GetComponent<NavMesh2DAgent>();
        if (_takeDamage == null)
        {
            _takeDamage = GetComponent<BombTakeDamage>();
        }
    }

    private void FindManagers()
    {
        if (_playTheBombEffect == null)
        {
            GameObject effectManager = GameObject.FindWithTag(EFFECTTAGNAME);
            if (effectManager != null)
            {
                _playTheBombEffect = effectManager.GetComponent<PlayTheBombEffect>();
            }
        }
        if (_playeTheSEManager == null)
        {
            GameObject seManager = GameObject.FindWithTag(SETAGNAME);
            if (seManager != null)
            {
                _playeTheSEManager = seManager.GetComponent<SEManager>();
            }
        }
    }

    private void Update()
    {
        if (_isColPlayer)
        {
            ColPlayerBomb();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_isExplosion) return;

        //プレイヤーに接触したときの自爆処理
        if (collision.gameObject.CompareTag(PLAYERTAGNAME))
        {
            if (_isColPlayer) return;

            if (IsServer)
            {
                StartSelfDestruct();
            }
            else
            {
                // クライアント側で衝突を検知した場合、ServerRpcでサーバーへ自爆開始を要求
                RequestStartSelfDestructServerRpc();
            }
        }
        else
        {
            //吹き飛び中にプレイヤー以外に接触したら爆発
            if (_isBlowing)
            {
                if (IsServer)
                {
                    _isExplosion = true;
                    BombCircleCheck();
                    if (_takeDamage != null)
                    {
                        _takeDamage.NormalDeath();
                    }
                }
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void RequestStartSelfDestructServerRpc()
    {
        StartSelfDestruct();
    }

    private void StartSelfDestruct()
    {
        if (_isColPlayer) return;
        _isColPlayer = true;
        _isExplosion = true;
        if (_agent != null)
        {
            _agent.Stop();
        }

        // 全クライアントに自爆カウントダウン演出の開始を通知
        StartSelfDestructClientRpc();
    }

    [ClientRpc]
    private void StartSelfDestructClientRpc()
    {
        _isColPlayer = true;
        _isExplosion = true;
        if (_agent != null)
        {
            _agent.Stop();
        }
    }

    /// <summary>
    /// プレイヤーに触れた時に入れる演出、すぐに爆発はしない
    /// </summary>
    private void ColPlayerBomb()
    {
        _bombWaitTime -= Time.deltaTime;
        _initTime += Time.deltaTime;
        float speed = 1f / Mathf.Max(_bombWaitTime, 0.1f);
        ScaleChange(speed);

        if (_bombWaitTime <= 0f)
        {
            _isColPlayer = false; // 重複実行防止
            // 爆発判定・ダメージ・破棄はサーバーでのみ実行
            if (IsServer)
            {
                BombCircleCheck();
                if (_takeDamage != null)
                {
                    _takeDamage.NormalDeath();
                }
            }
        }
    }

    private void ScaleChange(float speed)
    {
        if (_scaleObj == null) return;
        float waveSpeed = 3.0f; // 高いほど速く変化する
        float t = Mathf.InverseLerp(speed, 0f, _initTime);
        float scale = 1.0f + Mathf.Sin(t * Mathf.PI * waveSpeed) * 0.5f; // 揺れ幅も調整
        _scaleObj.transform.localScale = Vector3.one * scale;
    }

    /// <summary>
    /// 爆発の判定チェックを行うメソッド（サーバーのみ実行）
    /// </summary>
    public void BombCircleCheck()
    {
        if (!IsServer) return;

        // 全クライアントに爆発エフェクトとSEの再生を通知（現在座標を渡す）
        BombEffectClientRpc(transform.position);

        //爆弾のパワーで補正値を求める
        float t = Mathf.Clamp01(_bombEnemyPower / 2f);
        //半径を求める
        float radius = Mathf.Lerp(10f, 3f, t);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);

        foreach (Collider2D hit in hits)
        {
            if (hit == null || hit.gameObject == this.gameObject) continue;
            GameObject hitObject = hit.gameObject;
            if (hitObject.CompareTag(BOMBTAGNAME))
            {
                BombTakeDamage bombTakedamage = hitObject.GetComponent<BombTakeDamage>();
                if (bombTakedamage != null)
                {
                    bombTakedamage.NormalDeath();
                }
            }
            else if (hitObject.CompareTag(PLAYERTAGNAME))
            {
                Vector3 direction = (hitObject.transform.position - this.transform.position).normalized;
                PlayerDamageKnockBack knockback = hitObject.GetComponent<PlayerDamageKnockBack>();
                if (knockback != null)
                {
                    //吹き飛ばしのメソッド呼び出し
                    knockback.SetDirection(direction, _bombPower, this.gameObject);
                }
            }
            else
            {
                EnemyKnockBack knockback = hitObject.GetComponent<EnemyKnockBack>();
                EnemyTakeDamage takeDamage = hitObject.GetComponent<EnemyTakeDamage>();
                if (takeDamage != null && knockback != null)
                {
                    knockback.SetDirectionAndForce((Vector2)transform.position, _bombEnemyPower, true, false);
                    takeDamage.SetExplosionDamgage(_bombEnemyPower);
                }
                else if (knockback != null)
                {
                    knockback.SetDirectionAndForce((Vector2)transform.position, _bombEnemyPower, true, false);
                }
            }
        }
    }

    [ClientRpc]
    private void BombEffectClientRpc(Vector3 explosionPos)
    {
        FindManagers();

        if (_playTheBombEffect != null)
        {
            _playTheBombEffect.BombEffect(explosionPos);
        }

        if (_playeTheSEManager != null)
        {
            _playeTheSEManager.PlayBombSound();
        }
    }

    private void OnDrawGizmos()
    {
        float t = Mathf.Clamp01(_bombEnemyPower / 2f);
        float radius = Mathf.Lerp(10f, 3f, t);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
