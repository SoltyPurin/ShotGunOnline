using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ArmorKnockBack : EnemyKnockBack
{
    [SerializeField, Header("どれくらいの角度を正面とするか")]
    private float _flont = 30;
    [SerializeField, Header("回転の子オブジェクト")]
    private GameObject _rotateChildren = default;

    // ガード時のforce代入値
    private float _guardKnockbackForce = 8; 
    // 当たった角度
    private float _angle = default;
    // 正面からの攻撃かどうかを判断するフラグ
    private bool _isFromFlont = false;
    public bool IsFromFlont
    {
        get { return _isFromFlont; }
    }
    // SEManagerのスクリプト
    private SEManager _seManager = default;

    // 最低吹き飛び値
    private float _minBlowAwayValue = 3.0f;

    // 線形補正の補正値
    private float _t = 0.5f;

    // 乗算値
    private float _powMultiplicationValue = 5.0f;

    private void Start()
    {
        GameObject seManagerObject = GameObject.FindWithTag("SEManager");
        if (seManagerObject != null)
        {
            _seManager = seManagerObject.GetComponent<SEManager>();
        }
        _trenble = GetComponent<TrenbleEnemy>();
    }

    /// <summary>
    /// 基底クラスのRPC経由で呼ばれた場合のオーバーライド
    /// </summary>
    public override void SetDirectionAndForce(Vector2 playerPos, float chargeTime, bool isCrit, bool isUlt)
    {
        SetDirectionAndForce(playerPos, chargeTime, isUlt ? PlayerState.Ultimate : PlayerState.Normal, isCrit, isUlt);
    }

    public override void PredictLocalKnockBack(Vector2 playerPos, float chargeTime, PlayerState state, bool isCrit, bool isUlt)
    {
        if (IsServer || _enemyMove == null) return;

        Vector2 blowDir = ((playerPos - (Vector2)transform.position) * -1f).normalized;
        bool isFromFront = false;
        if (_rotateChildren != null)
        {
            float angle = Vector2.Angle((playerPos - (Vector2)transform.position), _rotateChildren.transform.up.normalized);
            isFromFront = angle < _flont;
        }

        float predictedForce = 0f;
        if (isFromFront && state == PlayerState.Normal && !isUlt)
        {
            predictedForce = _guardKnockbackForce;
        }
        else if (!isCrit && !isUlt)
        {
            float powVal = Mathf.Pow(chargeTime, _powMultiplicationValue);
            predictedForce = Mathf.Lerp(_minBlowAwayValue, powVal, _t);
        }

        _enemyMove.OnKnockBackPredicted(blowDir, predictedForce, _decelerationValue);
    }

    /// <summary>
    /// 吹き飛ぶ方向と力を設定するメソッド、ガード成否で処理が異なる
    /// </summary>
    /// <param name="playerPos">プレイヤーの座標</param>
    /// <param name="chargeTime">チャージ時間</param>
    /// <param name="state">プレイヤーのステート</param>
    public override void SetDirectionAndForce(Vector2 playerPos, float chargeTime, PlayerState state, bool isCrit, bool isUlt)
    {
        // 吹き飛ぶ方向計算
        _blowAwayDirection = ((playerPos - (Vector2)this.transform.position) * -1).normalized;

        if (_rotateChildren != null)
        {
            _angle = Vector2.Angle((playerPos - (Vector2)this.transform.position), _rotateChildren.transform.up.normalized);
            _isFromFlont = _angle < _flont;
        }
        else
        {
            _isFromFlont = false;
        }

        // 通常ショット時も含め、ノックバック開始時は即座にステートを knockback に変更する
        if (_enemyMove != null)
        {
            _enemyMove.EnemyState = EnemyState.knockback;
        }

        if (_isFromFlont && state == PlayerState.Normal && !isUlt)
        {
            // 正面からの攻撃かつ通常ショットの場合はガードの吹き飛び
            _force = _guardKnockbackForce;
            _canKnockBack = true;
        }
        else
        {
            _powValue = Mathf.Pow(chargeTime, _powMultiplicationValue);
            _force = Mathf.Lerp(_minBlowAwayValue, _powValue, _t);

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
        }

        if (IsServer)
        {
            PlayTakeDamageEffectClientRpc(_isFromFlont && state == PlayerState.Normal && !isUlt);
            PlayKnockBackEffectsClientRpc(isCrit, isUlt, chargeTime, _attackerClientId);
        }
    }

    /// <summary>
    /// 被弾エフェクト再生
    /// </summary>
    /// <param name="isGuard">ガード成否</param>
    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    private void PlayTakeDamageEffectClientRpc(bool isGuard)
    {
        if (isGuard && _seManager != null)
        {
            _seManager.PlayGuardSound();
        }

        DamageEffect damageEffect = GetComponentInChildren<DamageEffect>();
        if (damageEffect != null)
        {
            damageEffect.PlayAnim();
        }
    }
}
