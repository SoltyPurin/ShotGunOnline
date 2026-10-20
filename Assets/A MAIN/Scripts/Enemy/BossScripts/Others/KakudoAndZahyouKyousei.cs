using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KakudoAndZahyouKyousei : MonoBehaviour
{
    private BossStateManagement _state = default;
    private void Awake()
    {
        Transform parent = transform.parent.parent;
        _state = parent.GetComponent<BossStateManagement>();    
    }
    public void RotateReset()
    {
        this.transform.rotation = Quaternion.identity;
    }

    private void Update()
    {
        if(_state.CurrentState != BossStateManagement.BossState.JumpAtack)
        {
            this.transform.localPosition = Vector3.zero;
        }
    }
}
