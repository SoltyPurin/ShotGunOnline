using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KakudoAndZahyouKyousei : MonoBehaviour
{
    public void RotateReset()
    {
        this.transform.rotation = Quaternion.identity;
        this.transform.localPosition = Vector3.zero;
    }
}
