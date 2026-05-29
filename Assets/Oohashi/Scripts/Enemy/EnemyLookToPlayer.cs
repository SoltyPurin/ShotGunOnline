using UnityEngine;

/// <summary>
/// 敵をプレイヤーのいる向きに変えるスクリプト
/// </summary>
/// 
public class EnemyLookToPlayer : MonoBehaviour
{
    private GameObject _player = default;

    private readonly string PLAYER = "Player";

    [SerializeField, Header("反転させるオブジェクト(スプライト)")]
    protected GameObject _sprite = default;
    [SerializeField]
    private BossStateManagement _stateManagement = default;
    [SerializeField]
    private BossMachPunch _machPunch = default;

    private Vector2 _leftRotation = new Vector3(0, 180f, 0);
    public virtual void Start()
    {
        _player = GameObject.FindWithTag(PLAYER);
    }

    private void FixedUpdate()
    {
        if (this.gameObject.CompareTag("Boss"))
        {
            BossLook();
        }
        else
        {
            EnemyLook();
        }
    }

    public void EnemyLook()
    {
        float direction = (_player.transform.position.x - this.transform.position.x);
        if (direction > 0)
        {
            TurnRight();
        }
        else
        {
            TurnLeft();
        }
    }
    public void BossLook()
    {
        float bossDirection = (_player.transform.position.x - this.transform.position.x);
        bool jumpAttack = _stateManagement._currentState == BossStateManagement.BossState.JumpAtack;
        bool cooldown = _stateManagement._currentState == BossStateManagement.BossState.Cooldown;
        bool punch = _machPunch._lockRotate;

        if (!jumpAttack && !punch && !cooldown)
        {
            if (bossDirection > 0)
            {
                TurnRight();
            }
            else
            {
                TurnLeft();
            }
        }
    }
    public virtual void TurnLeft()
    {
        _sprite.transform.localRotation = Quaternion.Euler(_leftRotation);
    }

    public virtual void TurnRight()
    {
        _sprite.transform.localRotation = Quaternion.Euler(Vector2.zero);
    }

}
