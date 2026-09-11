using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FatSlowTakeDamage : EnemyTakeDamage
{
    public override void Start()
    {
        base.Start();
        _enemyHP.Value = JsonSaver.Instance.EnemyJson.FatHP;
        if (_hpUI != null)
        {
            //Å‰‚ÉHP•\¦‚Ìƒo[‚ÉÅ‘åhp‚ğİ’è
            _hpUI.Initialize(_enemyHP.Value);
        }

    }
}
