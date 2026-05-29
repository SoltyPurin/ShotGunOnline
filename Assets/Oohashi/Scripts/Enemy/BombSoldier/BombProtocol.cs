using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class BombProtocol : MonoBehaviour
{
    private bool _isBlowing = false;    //吹っ飛んでるかを判断
    public bool IsBlowing
    {
        set { _isBlowing = value; } //BombKnockBackから書き換える
    }

    [Header("爆発の力")]
    [SerializeField] private float _bombPower = 5f;
    [SerializeField,Header("爆発で敵に与えるダメージ")]
    private float _bombEnemyPower = 2f;
    [SerializeField, Header("爆発で弱点に与えるダメージ")]
    private float _weekDamage = 4;
    [Header("ダメージ処理を行うスクリプト")]
    [SerializeField] private BombTakeDamage _takeDamage = default;
    [SerializeField, Header("実際に拡大させるオブジェクト")]
    private GameObject _scaleObj = default;

    //プレイヤーに触れたときに爆発するまでの猶予時間
    private float _bombWaitTime = 1;

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
        GameObject effectManager = GameObject.FindWithTag(EFFECTTAGNAME);
        _playTheBombEffect = effectManager.GetComponent<PlayTheBombEffect>();
        GameObject seManager = GameObject.FindWithTag(SETAGNAME);
        _playeTheSEManager = seManager.GetComponent<SEManager>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _material = _spriteRenderer.material;
        _agent = GetComponent<NavMesh2DAgent>();
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
        //プレイヤーに接触したときの自爆処理
        if (collision.gameObject.CompareTag(PLAYERTAGNAME))
        {
            _isColPlayer = true;
            _collision = collision.gameObject;
            _isExplosion = true;
            _agent.Stop();
        }
        else
        {
            //吹き飛び中にプレイヤー以外に接触したら爆発
            if (_isBlowing)
            {
                //爆発のメソッド呼び出し
                BombCircleCheck();
                _isExplosion = true;
                //通常の死亡のメソッド呼び出し
                _takeDamage.NormalDeath();

            }
        }

    }

    /// <summary>
    /// プレイヤーに触れた時に入れる演出、すぐに爆発はしない
    /// </summary>
    private void ColPlayerBomb()
    {
        _bombWaitTime -= Time.deltaTime;
        _initTime += Time.deltaTime;
        float speed = 1 / Mathf.Max(_bombWaitTime, 0.1f);
        ScaleChange(speed);

        if (_bombWaitTime <= 0)
        {
            //プレイヤーを吹き飛ばす方向を計算
            //爆発のメソッド呼び出し
            BombCircleCheck();
            _takeDamage.NormalDeath();
        }
    }

    private void ScaleChange(float speed)
    {
        float waveSpeed = 3.0f; // 高いほど速く変化する
        float t = Mathf.InverseLerp(speed, 0, _initTime);
        float scale = 1.0f + Mathf.Sin(t * Mathf.PI * waveSpeed) * 0.5f; // 揺れ幅も調整
        _scaleObj.transform.localScale = Vector3.one * scale;
    }

    /// <summary>
    /// 爆発の判定チェックを行うメソッド
    /// </summary>
    public void BombCircleCheck()
    {
        _playTheBombEffect.BombEffect(transform.position);
        _playeTheSEManager.PlayBombSound();
        //爆弾のパワーで補正値を求める
        float t = Mathf.Clamp01(_bombEnemyPower / 2f);
        //半径を求める
        float radius = Mathf.Lerp(10f, 3f, t);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);

        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject == this.gameObject) continue;
            Vector2 directionToTarget = (hit.transform.position - transform.position).normalized;
            Vector2 forward = Vector2.right;
            float angle = Vector2.Angle(forward, directionToTarget);
            GameObject hitObject = hit.gameObject;
            if (hit.gameObject.CompareTag(BOMBTAGNAME))
            {
                BombTakeDamage bombTakedamage = hitObject.GetComponent<BombTakeDamage>();
                bombTakedamage.NormalDeath();
            }
            else if (hit.gameObject.CompareTag("Player"))
            {
                //プレイヤーに接触したときのダメージ判定を呼び出す
                _takeDamage.PlayerCollision();
                if(_collision != null)
                {
                    Vector3 direction = (_collision.transform.position - this.transform.position).normalized;
                    PlayerDamageKnockBack knockback = _collision.gameObject.GetComponent<PlayerDamageKnockBack>();
                    //吹き飛ばしのメソッド呼び出し
                    knockback.SetDirection(direction, _bombPower, this.gameObject);

                }

            }
            else
            {
                EnemyKnockBack knockback = hitObject.GetComponent<EnemyKnockBack>();
                EnemyTakeDamage takeDamage = hitObject.GetComponent<EnemyTakeDamage>();
                if (takeDamage != null)
                {
                    knockback.SetDirectionAndForce((Vector2)transform.position, _bombEnemyPower, true,false);
                    takeDamage.SetExplosionDamgage(_bombEnemyPower);
                }
                else if (knockback != null)
                {
                    knockback.SetDirectionAndForce((Vector2)transform.position, _bombEnemyPower,true,false);

                }

            }
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
