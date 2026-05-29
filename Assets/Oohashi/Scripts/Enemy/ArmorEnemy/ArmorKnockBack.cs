using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmorKnockBack : EnemyKnockBack
{
    [SerializeField,Header("どれくらいの角度を正面とするか")]
    private float _flont = 30;
    [SerializeField, Header("回転体のオブジェクト")]
    private GameObject _rotateChildren = default;

    //ガードしたときにforceに代入する値
    private float _guardKnockbackForce = 8; 
    //攻撃してきた角度を代入するfloat
    private float _angle = default;
    //正面からの攻撃かどうかを判断するフラグ
    private bool _isFromFlont = false;
    public bool IsFromFlont
    {
        get { return _isFromFlont; }
    }
    //seManagerのスクリプト
    private SEManager _seManager = default;

    //最低吹き飛び値
    private float _minBlowAwayValue = 3.0f;

    //線形補正の補正値
    private float _t = 0.5f;

    //乗算する値
    private float _powMultiplicationValue = 5.0f;
    private void Start()
    {
        GameObject seManagerObject = GameObject.FindWithTag("SEManager");
        _seManager = seManagerObject.GetComponent<SEManager>();
        _trenble = GetComponent<TrenbleEnemy>();
    }

    /// <summary>
    /// 吹き飛ぶ方向と力を設定するメソッド、ガードしたか否かで処理が異なる
    /// </summary>
    /// <param name="playerPos">プレイヤーの座標</param>
    /// <param name="chargeTime">チャージ時間</param>
    /// <param name="state">プレイヤーのステート</param>
    public void SetDirectionAndForce(Vector2 playerPos, float chargeTime,PlayerState state, bool isCrit, bool isUlt)
    {
        //吹き飛ぶ方向を計算
        _blowAwayDirection = ((playerPos - (Vector2)this.transform.position) * -1).normalized;
        //攻撃が盾のオブジェクトの正面から見て何度から飛んできたか計算
        _angle = Vector2.Angle((playerPos - (Vector2)this.transform.position), _rotateChildren.transform.up.normalized);
        //攻撃してきた角度が正面判定よりでかい角度だった場合true
        _isFromFlont = _angle <_flont;
        _canKnockBack = true;
        if (_isFromFlont && state == PlayerState.Normal)
        {
            //正面からの攻撃でなおかつ通常ショットだった場合ガード判定の吹き飛ばし
            _force = _guardKnockbackForce;
            _seManager.PlayGuardSound();
        }
        else
        {
            _powValue = Mathf.Pow(chargeTime, _powMultiplicationValue);
            //最低3からpowValueまでの間で線形補正を行い、チャージ値ごとの吹き飛ばしをなめらかに
            _force = Mathf.Lerp(_minBlowAwayValue, _powValue, _t);
        }

        if (_trenble != null)
        {
            if (isCrit)
            {
                Debug.Log("クリティカル");
                StartCoroutine(HitStop(_force, _critStiffnessTime));
                _force = 0;
                _enemyMove.EnemyState = EnemyState.knockback;
                _trenble.TrenbleProtocol(_critStiffnessTime);
            }
            else if (isUlt)
            {
                Debug.Log("ウルト");
                StartCoroutine(HitStop(_force, _ultStiffnessTime));
                _force = 0;
                _enemyMove.EnemyState = EnemyState.knockback;
                _trenble.TrenbleProtocol(_ultStiffnessTime);
            }
            else if (!isCrit && !isUlt)
            {
                Debug.Log("通常");
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

}
