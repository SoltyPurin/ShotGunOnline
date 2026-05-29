using UnityEngine;

/// <summary>
/// 基本の敵用アニメーションスクリプト
/// 【作成者：髙橋英士】
/// </summary>
public class EnemyAnimationManager : MonoBehaviour
{
    [SerializeField, Header("スプライト自身のアニメーター")]
    private Animator _animator = default;

    [SerializeField, Header("スクリプト取得")]
    private EnemyMove _enemyMove = default;

    private void Start()
    {
        _animator.SetBool("Move", false);
        _animator.SetBool("KnockBack", false);
    }

    private void FixedUpdate()
    {
        switch(_enemyMove.EnemyState)
        {
            case EnemyState.move:
                _animator.SetBool("Move", true);
                _animator.SetBool("KnockBack", false);
                break;

            case EnemyState.knockback:
                _animator.SetBool("KnockBack", true);
                _animator.SetBool("Move", false);
                break;

            case EnemyState.fall:
                _animator.SetBool("KnockBack", true);
                _animator.SetBool("Move", false);
                break;

            case EnemyState.Wait:
                _animator.SetBool("Move", false);
                _animator.SetBool("KnockBack", false);
                break;
        }
    }
}
