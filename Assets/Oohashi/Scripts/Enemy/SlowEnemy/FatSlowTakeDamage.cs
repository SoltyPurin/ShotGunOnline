using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FatSlowTakeDamage : EnemyTakeDamage
{
    public override void Start()
    {
        base.Start();
        SetMaxHP(JsonSaver.Instance.EnemyJson.FatHP);
    }
}
