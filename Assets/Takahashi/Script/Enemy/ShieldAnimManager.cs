using UnityEngine;

/// <summary>
/// シールド持ち用アニメーション再生スクリプト
/// 【作成者：髙橋英士】
/// </summary>
public class ShieldAnimManager : MonoBehaviour
{
    [SerializeField, Header("スプライト自身のアニメーター")]
    private Animator _animator = default;

    [SerializeField, Header("スクリプト取得")]
    private ArmorMove _armorMove = default;

    private void Start()
    {
        //アニメーターのBoolを初期化
        _animator.SetBool("Charge", false);
        _animator.SetBool("Move", false);
        _animator.SetBool("KnockBack", false);
    }

    private void FixedUpdate()
    {
        //敵のステートによってアニメーションを切り替える
        switch (_armorMove.EnemyState)
        {
            //通常状態の場合、各種アニメーション再生
            case EnemyState.move:
                AnimChange();
                break;

            //ノックバックアニメーション再生
            case EnemyState.knockback:
                _animator.SetBool("KnockBack", true);
                _animator.SetBool("Charge", false);
                _animator.SetBool("Move", false);
                break;

            //落ちてる場合でもノックバックアニメーションを再生
            case EnemyState.fall:
                _animator.SetBool("KnockBack", true);
                _animator.SetBool("Charge", false);
                _animator.SetBool("Move", false);
                break;

            //初期化
            case EnemyState.Wait:
                _animator.SetBool("Charge", false);
                _animator.SetBool("Move", false);
                _animator.SetBool("KnockBack", false);
                break;
        }
    }

    /// <summary>
    /// 通常状態内のステートによってアニメーションを切り替えるメソッド
    /// </summary>
    private void AnimChange()
    {
        switch(_armorMove.InitState)
        {
            //停止状態のアニメーション再生
            case ArmorState.Stop:
                _animator.SetBool("Charge", false);
                _animator.SetBool("Move", false);
                _animator.SetBool("KnockBack", false);
                break;

            //突進状態のアニメーション再生
            case ArmorState.Rush:
                _animator.SetBool("Move", true);
                _animator.SetBool("Charge", false);
                _animator.SetBool("KnockBack", false);
                break;

            //プレイヤーを狙っている時のアニメーション再生
            case ArmorState.Rotate:
                _animator.SetBool("Charge", true);
                _animator.SetBool("Move", false);
                _animator.SetBool("KnockBack", false);
                break;

            //ガード時のアニメーション再生
            case ArmorState.Reservoir:
                _animator.SetBool("Charge", false);
                _animator.SetBool("Move", false);
                _animator.SetBool("KnockBack", false);
                break;
        }
    }
}
