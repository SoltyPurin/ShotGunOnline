using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossHPGageAnimeCon : MonoBehaviour
{
    [SerializeField]
    private Animator _gageAnimator = default;

    public void HitGageAnime()
    {
        _gageAnimator.SetTrigger("HitDamage");
    }
}
