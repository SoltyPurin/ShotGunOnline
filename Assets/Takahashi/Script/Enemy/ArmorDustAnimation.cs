using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmorDustAnimation : KnockBackDust
{
    private ArmorMove _move = default;

    private void Awake()
    {
        _move = GetComponent<ArmorMove>();
    }
    public override void FixedUpdate()
    {
        base.FixedUpdate();
         switch( _move.InitState )
        {
            case ArmorState.Rush:
                PlayEffect();
                break;

            default:
                break;
        }
    }
}
