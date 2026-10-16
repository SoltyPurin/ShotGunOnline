using UnityEngine;
public class SlowEnemyTakeDamage : EnemyTakeDamage
{
    public override void Start()
    {
        base.Start();
        JsonSaver.Instance.LoadAllConfigs(); 
        _enemyHP.Value = JsonSaver.Instance.EnemyJson.SlowHP;
        if (_hpUI != null)
        {
            //Å‰‚ÉHP•\¦‚Ìƒo[‚ÉÅ‘åhp‚ğİ’è
            _hpUI.Initialize(_enemyHP.Value);
        }

    }
}
