using UnityEngine;

/// <summary>
/// ゴキブリの被ダメージ・死亡管理
/// （ゴキブリは死亡時にコインをドロップしない）
/// </summary>
public class CockRoachTakeDamage : EnemyTakeDamage
{
    public override void Start()
    {
        _shouldDropCoin = false;
        base.Start();
        SetMaxHP(JsonSaver.Instance.EnemyJson.CockRoachHP);
    }
}
