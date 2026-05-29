using UnityEngine;

/// <summary>
/// ボスの近接攻撃がアニメーションで管理されてたのでそれに対応させるためだけのクラス
/// （アニメーターがついてるオブジェクトに付けてね）
/// </summary>
public class BossAttackFunction : MonoBehaviour
{
    #region[変数名]
    //---GameObject,Script,Animator等---------------------------------
    [SerializeField, Header("BossのStateを取得")]
    private BossStateManagement _stateManagement = default;
    [SerializeField, Header("JumpAtackを取得")]
    private BossJumpAtack _jumpAtack = default;
    [SerializeField, Header("MachPunchを取得")]
    private BossMachPunch _machPunch = default;
    [SerializeField, Header("Bossのコライダー取得")]
    private Collider2D _bossCollider = default;

    #endregion

    /// <summary>
    /// 近接攻撃全体を終了させるメソッド
    /// </summary>
    public void AttackEnd()
    {
        if (_stateManagement._currentState == BossStateManagement.BossState.JumpAtack)
        {
            _jumpAtack.JumpAttackEnd();
        }
        else if (_stateManagement._currentState == BossStateManagement.BossState.Punch)
        {
            _machPunch.PunchEnd();
        }
    }
    /// <summary>
    /// プレイヤーへの攻撃判定を出現させるメソッド
    /// </summary>

    public void JumpAtackAreaPop()
    {
        _jumpAtack.JumpLand();
    }

    public void JumpAimStart()
    {
        _jumpAtack.AimStart();
    }
    public void JumpAimEnd()
    {
        _jumpAtack.AimEnd();
    }

    public void StartPunch()
    {
        _machPunch.MachPunch();
    }
    public void EndPunchTargeting()
    {
        _machPunch.EndTargeting();
    }
}
