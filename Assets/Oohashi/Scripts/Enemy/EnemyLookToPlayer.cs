using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 敵をプレイヤーのいる向きに変えるスクリプト
/// </summary>
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
        _player = FindNearestPlayer();
        if (_player == null) return;

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

    private GameObject FindNearestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag(PLAYER);
        GameObject nearestPlayer = null;
        float minDistance = float.MaxValue;
        foreach (GameObject p in players)
        {
            if (p == null) continue;
            float dist = Vector2.Distance(transform.position, p.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearestPlayer = p;
            }
        }
        return nearestPlayer;
    }

    public void BossLook()
    {
        if (_player == null || !_player.activeInHierarchy)
        {
            _player = FindNearestPlayer();
            if (_player == null) return;
        }

        if (_stateManagement == null) return;

        bool jumpAttack = _stateManagement._currentState == BossStateManagement.BossState.JumpAtack;
        bool cooldown = _stateManagement._currentState == BossStateManagement.BossState.Cooldown;
        bool punch = _machPunch != null && _machPunch._lockRotate;
        bool drop = _stateManagement._currentState == BossStateManagement.BossState.Drop;

        if (!jumpAttack && !punch && !cooldown && !drop)
        {
            float bossDirection = (_player.transform.position.x - this.transform.position.x);
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
        if (_sprite != null)
        {
            _sprite.transform.localRotation = Quaternion.Euler(_leftRotation);
        }
    }

    public virtual void TurnRight()
    {
        if (_sprite != null)
        {
            _sprite.transform.localRotation = Quaternion.Euler(Vector2.zero);
        }
    }
}
