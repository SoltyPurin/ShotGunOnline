using UnityEngine;
public class SlowEnemyTakeDamage : EnemyTakeDamage
{
    public override void Start()
    {
        base.Start();
        JsonSaver.Instance.LoadAllConfigs(); 
        SetMaxHP(JsonSaver.Instance.EnemyJson.SlowHP);
    }
}
