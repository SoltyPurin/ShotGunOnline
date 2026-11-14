using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

public class EnemyKnockBack : NetworkBehaviour
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
    //直近の攻撃者のClientId
    protected ulong _attackerClientId = default;
    #endregion
    #region 定数
    private readonly string WALLTAG = "Wall";
    #endregion

    private void Start()
    {
        _trenble = GetComponent<TrenbleEnemy>();
    }
    [ServerRpc(RequireOwnership = false)]
    public void RequestKnockBackServerRpc(Vector2 playerPos, float chargeTime, bool isCrit, bool isUlt, ServerRpcParams rpcParams = default)
    {
        // 攻撃者はローカルで演出を先行再生済みのため、ClientRpcでの二重再生を避けるために記録する
        _attackerClientId = rpcParams.Receive.SenderClientId;
        // サーバー上でノックバックの方向と力を計算・適用する
        SetDirectionAndForce(playerPos, chargeTime, isCrit, isUlt);
    }

    /// <summary>
    /// 吹き飛ぶ方向と加える力を設定するメソッド
    /// </summary>
    /// <param name="playerPos">プレイヤーの座標</param>
    /// <param name="chargeTime">チャージ時間</param>
    /// <param name="isCrit">クリティカル攻撃</param>
    /// <param name="isUlt">ウルト</param>
    public virtual void SetDirectionAndForce(Vector2 playerPos, float chargeTime, bool isCrit, bool isUlt)
    {
        //if (IsServer)
        //{
            _blowAwayDirection = ((playerPos - (Vector2)this.transform.position) * -1).normalized;
            //チャージ時間の乗を求める
            _powValue = Mathf.Pow(chargeTime, _forceMultiplier);
            //最低6からpowValueまでの間で線形補正を行い、チャージ値ごとの吹き飛ばしをなめらかに
            //敵のノックバックは初速最速で瞬間的に最高速度から抵抗に負けて減速するみたいなシステムにする
            //長距離飛ぶわけではないけど沢山飛ぶわけではない
            //3つプレイヤーに気づかせることでプレイヤーは面白いと感じる
            //最後まで気づかれないのは絶対ダメ
            _force = Mathf.Lerp(_minForce, _powValue, 0.5f);

            // 通常ショット時も含め、ノックバック開始時は即座にステートを knockback に変更する
            if (_enemyMove != null)
            {
                _enemyMove.EnemyState = EnemyState.knockback;
            }

            if (_trenble != null)
            {
                if (isCrit)
                {
                    StartCoroutine(HitStop(_force, _critStiffnessTime));
                    _force = 0;
                    _trenble.TrenbleProtocol(_critStiffnessTime);
                }
                else if (isUlt)
                {
                    StartCoroutine(HitStop(_force, _ultStiffnessTime));
                    _force = 0;
                    _trenble.TrenbleProtocol(_ultStiffnessTime);
                }
                else if (!isCrit && !isUlt)
                {
                    _trenble.NormalTrenbleProtocol(0.1f, chargeTime);
                    _canKnockBack = true;
                }
            }
            else
            {
                _canKnockBack = true;
            }

            DamageEffect damageEffect = GetComponentInChildren<DamageEffect>();
            if (damageEffect != null)
            {
                damageEffect.PlayAnim();
            }

            if (IsServer)
            {
                PlayKnockBackEffectsClientRpc(isCrit, isUlt, chargeTime, _attackerClientId);
            }
        //}
    }

    /// <summary>
    /// 全クライアント端末上で敵の震え演出とダメージエフェクトを再生するRPC
    /// </summary>
    [ClientRpc]
    protected void PlayKnockBackEffectsClientRpc(bool isCrit, bool isUlt, float chargeTime, ulong attackerClientId)
    {
        if (IsServer) return; // サーバー（ホスト）側では SetDirectionAndForce 内で既に再生済み

        // クライアント側でも前進移動を即座に停止してノックバック追従に備える
        if (_enemyMove != null)
        {
            _enemyMove.OnKnockBackStarted();
        }

        // 攻撃者本人は ShootRange.PlayLocalHitFeedback で再生済み(ウルトはローカル再生なしのため除外しない)
        if (!isUlt && NetworkManager.LocalClientId == attackerClientId) return;

        if (_trenble != null)
        {
            if (isCrit)
            {
                _trenble.TrenbleProtocol(_critStiffnessTime);
            }
            else if (isUlt)
            {
                _trenble.TrenbleProtocol(_ultStiffnessTime);
            }
            else
            {
                _trenble.NormalTrenbleProtocol(0.1f, chargeTime);
            }
        }

        DamageEffect damageEffect = GetComponentInChildren<DamageEffect>();
        if (damageEffect != null)
        {
            damageEffect.PlayAnim();
        }
    }
    public IEnumerator HitStop(float force, float waitTime)
    {
        if (IsServer)
        {
            yield return new WaitForSecondsRealtime(waitTime);
            _force = force;
            _canKnockBack = true;
        }
    }

    public void SetWarpDirection(Vector2 direction)
    {
        _blowAwayDirection = direction;
    }

    private void FixedUpdate()
    {
        if (IsServer)
        {
            //ノックバック中だったら処理しない
            if (!_canKnockBack)
            {
                return;
            }
            KnockBackMethod();
        }
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
