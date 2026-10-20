using UnityEngine;

/// <summary>
/// ボスの各攻撃アニメーションイベントから呼び出される統括クラス
/// （アニメーションイベントはクライアントでも発火するため、サーバー側でのみ処理を実行する）
/// </summary>
public class BossAttackFunction : MonoBehaviour
{
    #region[変数名]
    //---GameObject,Script,Animator等---------------------------------
    [SerializeField, Header("BossのState管理")]
    private BossStateManagement _stateManagement = default;
    [SerializeField, Header("JumpAtack管理")]
    private BossJumpAtack _jumpAtack = default;
    [SerializeField, Header("MachPunch管理")]
    private BossMachPunch _machPunch = default;
    [SerializeField, Header("Bossのコライダー")]
    private Collider2D _bossCollider = default;

    #endregion

    private void Start()
    {
        if (_stateManagement == null)
        {
            _stateManagement = GetComponentInParent<BossStateManagement>();
        }
        if (_jumpAtack == null)
        {
            _jumpAtack = GetComponentInParent<BossJumpAtack>();
        }
        if (_machPunch == null)
        {
            _machPunch = GetComponentInParent<BossMachPunch>();
        }
    }

    /// <summary>
    /// 各攻撃終了メソッド
    /// </summary>
    public void AttackEnd()
    {
        if (_stateManagement != null && !_stateManagement.IsServer) return;

        if (_stateManagement._currentState == BossStateManagement.BossState.JumpAtack)
        {
            if (_jumpAtack != null) _jumpAtack.JumpAttackEnd();
        }
        else if (_stateManagement._currentState == BossStateManagement.BossState.Punch)
        {
            if (_machPunch != null) _machPunch.PunchEnd();
        }
    }

    /// <summary>
    /// ジャンプ着地時の判定・衝撃波発生（※サーバーのみClientRpcを送信）
    /// </summary>
    public void JumpAtackAreaPop()
    {
        // サーバーからのみ ClientRpc を送信する（クライアント側で呼ぶと例外発生）
        if (_stateManagement != null && _stateManagement.IsServer)
        {
            if (_jumpAtack != null) _jumpAtack.JumpLandClientRpc();
        }
    }

    public void JumpAimStart()
    {
        if (_stateManagement != null && _stateManagement.IsServer)
        {
            if (_jumpAtack != null) _jumpAtack.AimStart();
        }
    }

    public void JumpAimEnd()
    {
        if (_stateManagement != null && _stateManagement.IsServer)
        {
            if (_jumpAtack != null) _jumpAtack.AimEnd();
        }
    }

    public void StartPunch()
    {
        if (_stateManagement != null && _stateManagement.IsServer)
        {
            if (_machPunch != null) _machPunch.MachPunch();
        }
    }

    public void EndPunchTargeting()
    {
        if (_stateManagement != null && _stateManagement.IsServer)
        {
            if (_machPunch != null) _machPunch.EndTargeting();
        }
    }
}