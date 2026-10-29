using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CockRoachTakeDamage : EnemyTakeDamage
{
    public override void Start()
    {
        base.Start();
        SetMaxHP(JsonSaver.Instance.EnemyJson.CockRoachHP);
    }
    public override IEnumerator DeathProtocol(float chargeTime)
    {
        //�S�L�u���͓|���Ă�R�C���������Ȃ�
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
