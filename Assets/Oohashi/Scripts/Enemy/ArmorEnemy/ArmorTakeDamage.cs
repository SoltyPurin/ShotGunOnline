using UnityEngine;

public class ArmorTakeDamage : EnemyTakeDamage
{
    [Header("ガードしてるかの判断用にスクリプト設定")]
    [SerializeField] protected ArmorKnockBack _armorKnockbackScript = default;
    [Header("ガードしたときに食らうダメージの乗算用数字")]
    [SerializeField] protected float _guardMultiplier = 250;
    [SerializeField, Header("ガード時のエフェクト")]
    protected Animator _guardEffect = default;

    [SerializeField,Header("ガードしたときの最低ダメージ")]
    protected float _guardMinDamage = 100;
    //ダメージを収容する変数
    protected float _damage = 0;

    /// <summary>
    /// 被弾ダメージ設定、ウルトか通常で処理を変える
    /// </summary>
    /// <param name="chargeTime">チャージ時間</param>
    /// <param name="state">プレイヤーのステート</param>
    public override void SetTakeDamege(float chargeTime,PlayerState state)
    {
        //ウルトだったら即死
        if (state == PlayerState.Ultimate)
        {
            _hpUI.UpdateHP(0);
            StartCoroutine(DeathProtocol(2));
            return;
        }
        //外部の変数を一旦ローカルの変数に逃がす
        bool isGuard = _armorKnockbackScript.IsFromFlont;
        //ガードしていた場合
        if (isGuard) 
        {
            //ガード時のダメージ設定及びガードのエフェクト再生
            _damage = chargeTime * _guardMultiplier;
            _damage = Mathf.Max(_damage, _guardMinDamage);
            _guardEffect.SetTrigger("Damage");
        }
        else
        {
            //ガードしてないときの被弾ダメージ設定及びエフェクト
            _damage = chargeTime * _damageMultiplier;
            _damage = Mathf.Max(_damage, _minDamage);
            _damageAnimator.SetTrigger("Damage");
            _damageMaterial.Damage();
            
        }
        //hpからダメージ分引く
        _enemyHP = (_enemyHP - (int)_damage);
        //HPバーを更新
        _hpUI.UpdateHP(_enemyHP);
        if (_enemyHP <= 0)
        {
            StartCoroutine(DeathProtocol(chargeTime));
        }
    }

}
