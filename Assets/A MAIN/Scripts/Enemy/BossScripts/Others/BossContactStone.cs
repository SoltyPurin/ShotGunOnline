using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossContactStone : EnemyContactStone
{
    private PlayTheBombEffect _playTheBombEffect = default;
    private SEManager _seManager = default;

    private void Start()
    {
        GameObject effectManager = GameObject.FindWithTag("EffectManager");
        _playTheBombEffect = effectManager.GetComponent<PlayTheBombEffect>();
        GameObject seManagerObject = GameObject.FindWithTag("SEManager");
        _seManager = seManagerObject.GetComponent<SEManager>();
    }
    public override void ContactStoneMethod(GameObject collision)
    {
        SetKnockBackStone stoneKnockBack = collision.GetComponent<SetKnockBackStone>();
        if (stoneKnockBack.State == StoneState.KnockBack)
        {
            _enemyKnockBack.SetDirectionAndForce(collision.transform.position, 2,false,false);
            _takeDamage.ContactStoneMethod();
            _playTheBombEffect.BreakEffect(transform.position);
            _seManager.PlayRoadKill();
            Destroy(collision.gameObject);
        }
    }
}
