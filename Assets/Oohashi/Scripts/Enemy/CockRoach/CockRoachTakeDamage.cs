using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CockRoachTakeDamage : EnemyTakeDamage
{
    public override void Start()
    {
        base.Start();
        _enemyHP = JsonSaver.Instance.EnemyJson.CockRoachHP;
        if (_hpUI != null)
        {
            //最初にHP表示のバーに最大hpを設定
            _hpUI.Initialize(_enemyHP);
        }

    }
    public override IEnumerator DeathProtocol(float chargeTime)
    {
        //ゴキブリは倒してもコインが増えない
        float timeLeftUntilDeath = chargeTime / _deathTimeMultiplier;
        _playTheSEManager.PlayEnemyDeathSound();
        yield return new WaitForSeconds(timeLeftUntilDeath);
        Instantiate(_deathObject, transform.position, Quaternion.identity);
        Destroy(this.gameObject);
    }

    public override void ContactStoneMethod()
    {
        _playTheSEManager.PlayRoadKill();
        Instantiate(_deathObject, transform.position, Quaternion.identity);
        Destroy(this.gameObject);

    }
}
