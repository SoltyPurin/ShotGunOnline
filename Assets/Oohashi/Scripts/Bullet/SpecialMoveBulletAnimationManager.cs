using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpecialMoveBulletAnimationManager : MonoBehaviour
{
    [SerializeField]
    private SpecialMoveMirrorBullet _specialMoveMirrorBullet = default;
    [SerializeField]
    private Collider2D _bulletCollider = default;
    private Animator _specialMoveBulletAnimator;

    private void Start()
    {
        _specialMoveBulletAnimator = GetComponent<Animator>();
    }

    public void ChangeAnime()
    {
        _specialMoveBulletAnimator.SetBool("Idle", true);
        _specialMoveMirrorBullet.enabled = true;
        _bulletCollider.enabled = true;

    }
    public void DestroyBall()
    {
        Destroy(this.gameObject);
    }
}
