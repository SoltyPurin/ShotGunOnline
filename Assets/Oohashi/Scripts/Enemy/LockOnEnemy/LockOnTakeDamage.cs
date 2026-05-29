using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockOnTakeDamage : EnemyTakeDamage
{
    public override void Start()
    {
        base.Start();
        _enemyHP = JsonSaver.Instance.EnemyJson.LockOnHP;
        if (_hpUI != null)
        {
            //Å‰‚ÉHP•\¦‚Ìƒo[‚ÉÅ‘åhp‚ğİ’è
            _hpUI.Initialize(_enemyHP);
        }

    }
}
