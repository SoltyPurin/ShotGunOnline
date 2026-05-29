using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class EnemyKnockBack : MonoBehaviour
{
    #region 変数
    [SerializeField,Header("敵の移動スクリプト")]
    protected EnemyMove _enemyMove = default;
    [SerializeField, Header("クリティカルの硬直時間")]
    protected float _critStiffnessTime = 0.3f;
    [SerializeField, Header("ウルトの硬直時間")]
    protected float _ultStiffnessTime = 0.5f;
    protected TrenbleEnemy _trenble = default;
    //吹き飛び可能か
    protected bool _canKnockBack = false;
    public bool CanKnockBack
    {
        get { return _canKnockBack; }
    }
    //Powした値を保存
    protected float _powValue = default;
    //実際に吹き飛ぶ力
    protected float _force = default;
    [SerializeField, Header("吹っ飛ばしの最低値")]
    protected float _minForce = 20;
    [SerializeField, Header("吹っ飛ばしの乗算値")]
    protected float _forceMultiplier = 5;
    [SerializeField, Header("減速する値")]
    protected float _decelerationValue = 0.5f;
    //壁にぶつかったときに減速させる割合
    private float _divisionRatio = 0.8f;
    //吹き飛ぶ方向
    protected Vector2 _blowAwayDirection = default;
    #endregion
    #region 定数
    private readonly string WALLTAG = "Wall";
    #endregion

    private void Start()
    {
        _trenble = GetComponent<TrenbleEnemy>();
    }


    /// <summary>
    /// 吹き飛ぶ方向と加える力を設定するメソッド
    /// </summary>
    /// <param name="playerPos">プレイヤーの座標</param>
    /// <param name="chargeTime">チャージ時間</param>
    /// <param name="isCrit">クリティカル攻撃</param>
    /// <param name="isUlt">ウルト</param>
    public virtual void SetDirectionAndForce(Vector2 playerPos,float chargeTime,bool isCrit,bool isUlt)
    {
        _blowAwayDirection = ((playerPos - (Vector2)this.transform.position) * -1).normalized;
        //チャージ時間の乗を求める
        _powValue = Mathf.Pow(chargeTime, _forceMultiplier);
        //最低6からpowValueまでの間で線形補正を行い、チャージ値ごとの吹き飛ばしをなめらかに
        //敵のノックバックは初速最速で瞬間的に最高速度から抵抗に負けて減速するみたいなシステムにする
        //長距離飛ぶわけではないけど沢山飛ぶわけではない
        //3つプレイヤーに気づかせることでプレイヤーは面白いと感じる
        //最後まで気づかれないのは絶対ダメ
        _force = Mathf.Lerp(_minForce, _powValue, 0.5f);

        if (_trenble != null)
        {
            if (isCrit)
            {
                StartCoroutine(HitStop(_force,_critStiffnessTime));
                _force = 0;
                _enemyMove.EnemyState = EnemyState.knockback;
                _trenble.TrenbleProtocol(_critStiffnessTime);
            }else if (isUlt)
            {
                StartCoroutine(HitStop(_force,_ultStiffnessTime));
                _force = 0;
                _enemyMove.EnemyState = EnemyState.knockback;
                _trenble.TrenbleProtocol(_ultStiffnessTime);
            }
            else if(!isCrit && !isUlt)
            {
                _trenble.NormalTrenbleProtocol(0.1f, chargeTime);
                _canKnockBack = true;
            }
        }

        DamageEffect damageEffect = GetComponentInChildren<DamageEffect>();
        if (damageEffect != null)
        {
            damageEffect.PlayAnim();
        }
    }

    public IEnumerator HitStop(float force,float waitTime)
    {
        yield return new WaitForSecondsRealtime(waitTime);
        _force = force;
        _canKnockBack = true;
    }

    public void SetWarpDirection(Vector2 direction)
    {
        _blowAwayDirection = direction;
    }

    private void FixedUpdate()
    {
        //ノックバック中だったら処理しない
        if (!_canKnockBack)
        {
            return;
        }
        KnockBackMethod();
    }
    /// <summary>
    /// 落下したときに力をゼロにして落とし穴を超えないようにする
    /// </summary>
    public virtual void Fall()
    {
        _force = 0;
    }

    public void CollisionWall()
    {
        _force = 0;

    }

    /// <summary>
    /// ノックバックするメソッド
    /// </summary>
    public virtual void KnockBackMethod()
    {
        //落下中でなければ実行
        if(_enemyMove.EnemyState != EnemyState.fall)
        {
            //力が0以上だったら減速
            if (_force >= 0)
            {
                //吹き飛び力から減算
                _force -= _decelerationValue;
                //ノックバックのステートを継続
                _enemyMove.EnemyState = EnemyState.knockback;
            }
            else
            {
                //ノックバックのフラグをノックバックしてない判定にする
                    _canKnockBack = false;
                //現在のステートを移動ステートに切り替える
                    _enemyMove.EnemyState = EnemyState.move;
            }
        }
        else
        {
            //落下中だったらforceを0にする
            _force=0;

        }
        //実際に吹き飛ばす
        transform.position += (Vector3)_blowAwayDirection * _force * Time.fixedDeltaTime;

    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        bool canDIvisionForce = collision.gameObject.CompareTag(WALLTAG) && _force >= 0;
        if (canDIvisionForce)
        {
            float ratio = 1 - _divisionRatio;
            _force *= ratio;
            _force = Mathf.Clamp(_force, 0, _force);
        }
    }
}
