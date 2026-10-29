using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockOnTakeDamage : EnemyTakeDamage
{
    public override void Start()
    {
        base.Start();
        SetMaxHP(JsonSaver.Instance.EnemyJson.LockOnHP);
    }
}
