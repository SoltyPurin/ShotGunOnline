using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombTakeDamage : EnemyTakeDamage
{
    [SerializeField,Header("爆発プロトコルのスクリプト")]
    private BombProtocol _bombProtocol = default;
    //爆発したかどうかのフラグ
    private bool _hasExploted = false;

    public override void Start()
    {
        base.Start();
        _enemyHP = JsonSaver.Instance.EnemyJson.BombHP;
        if (_hpUI != null)
        {
            //最初にHP表示のバーに最大hpを設定
            _hpUI.Initialize(_enemyHP);
        }

    }

    /// <summary>
    /// 爆発のダメージを設定
    /// </summary>
    /// <param name="chargeTime">チャージ時間</param>
    public override void SetExplosionDamgage(float chargeTime)
    {
        if (_hasExploted) return;
        float damage = chargeTime * _explosionMultiplier;
        _enemyHP = (_enemyHP - (int)damage);
        if (_enemyHP <= 0)
        {
            _isDead = true;
            _bombProtocol.BombCircleCheck();
            _hasExploted = true;
            Destroy(this.gameObject);
        }
    }

    /// <summary>
    /// 死亡時のコルーチン
    /// </summary>
    /// <param name="chargeTime">チャージ時間</param>
    /// <returns></returns>
    public override IEnumerator DeathProtocol(float chargeTime)
    {
        _isDead = true;
        float timeLeftUntilDeath = chargeTime / _deathTimeMultiplier;
        _playTheSEManager.PlayEnemyDeathSound();
        ComboCounter counter = GameObject.FindWithTag(COMBOCOUNTER_TAGNAME).GetComponent<ComboCounter>();
        //コンボカウンターにプラス
        counter.ComboPlus();
        yield return new WaitForSeconds(timeLeftUntilDeath);
        _bombProtocol.BombCircleCheck();
        _playTheSEManager.PlayMoneyDropSound();
        Instantiate(_deathObject, transform.position, Quaternion.identity);
        Destroy(this.gameObject);

    }

    /// <summary>
    /// プレイヤーに接触
    /// </summary>
    public void PlayerCollision()
    {
        Destroy(this.gameObject);
    }
    /// <summary>
    /// 爆発ではない通常の死亡
    /// </summary>
    public void NormalDeath()
    {
        Destroy(this.gameObject);
    }
}