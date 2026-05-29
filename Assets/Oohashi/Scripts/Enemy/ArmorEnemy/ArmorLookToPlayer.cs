using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmorLookToPlayer : EnemyLookToPlayer
{
    private ArmorMove _armorMove = default;
    public override void Start()
    {
        base.Start();
        _armorMove = GetComponent<ArmorMove>();
    }
    public override void TurnLeft()
    {
        if(_armorMove.InitState == ArmorState.Stop)
        {
            return;
        }

        base.TurnLeft();
    }

    public override void TurnRight()
    {
        if (_armorMove.InitState == ArmorState.Stop)
        {
            return;
        }

        base.TurnRight();
    }
}
