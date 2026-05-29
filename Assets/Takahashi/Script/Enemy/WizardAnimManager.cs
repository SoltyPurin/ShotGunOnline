using UnityEngine;

/// <summary>
/// Wizardのアニメーション再生用スクリプト
/// 【作成者：髙橋英士】
/// </summary>
public class WizardAnimManager : MonoBehaviour
{
    [SerializeField, Header("スプライト自身のアニメーター")]
    private Animator _animator = default;

    [SerializeField, Header("スクリプト取得")]
    private KeepDistanceMove _keepDistanceMove = default;

    private void Start()
    {
        //アニメーターのBool初期化
        _animator.SetBool("Move", false);
        _animator.SetBool("KnockBack", false);
    }

    private void FixedUpdate()
    {
        //敵のステートによってアニメーションを切り替え
        switch(_keepDistanceMove.EnemyState)
        {
            //通常時のアニメーション再生
            case EnemyState.move:
                AnimChange();
                break;

            //ノックバック時のアニメーション再生
            case EnemyState.knockback:
                _animator.SetBool("KnockBack", true);
                _animator.SetBool("Move", false);
                break;

            //落下時のアニメーション再生
            case EnemyState.fall:
                _animator.SetBool("KnockBack", true);
                _animator.SetBool("Move", false);
                break;

            //初期化
            case EnemyState.Wait:
                _animator.SetBool("Move", false);
                _animator.SetBool("KnockBack", false);
                break;
        }
    }

    /// <summary>
    /// 通常時内でのステートによってアニメーション切り替え
    /// </summary>
    private void AnimChange()
    {
        switch (_keepDistanceMove.InitState)
        {
            //停止時のアニメーション再生
            case MoveState.Stop:
                _animator.SetBool("Move", false);
                _animator.SetBool("KnockBack", false);
                break;

            //接近時のアニメーション再生
            case MoveState.Approach:
                _animator.SetBool("Move", true);
                _animator.SetBool("KnockBack", false);
                break;

            //後退時のアニメーション再生
            case MoveState.Leave:
                _animator.SetBool("Move", true);
                _animator.SetBool("KnockBack", false);
                break;
        }
    }
}
