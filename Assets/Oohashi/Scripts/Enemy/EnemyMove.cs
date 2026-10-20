using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum EnemyState
{
    move,
    fall,
    knockback,
    roadKill,
    Wait
}

public class EnemyMove : Unity.Netcode.NetworkBehaviour
{
    protected GameObject _playerObject = default;
    protected readonly string PLAYERTAGNAME = "Player";
    protected Transform _target = default;
    protected NavMesh2DAgent _agent;
    private PlayerStateManager _playerStateManager;

    [SerializeField, Header("移動速度")]
    protected float _moveSpeed = 5f;
    public float MoveSpeed => _moveSpeed;

    [SerializeField, Header("プレイヤーと保つ距離")]
    protected float _keepDistance = 0f;

    protected bool _cantMove = false;

    public EnemyState EnemyState
    {
        get => _enemyState.Value;
        set
        {
            if (IsServer)
            {
                _enemyState.Value = value;
            }
        }
    }

    protected NetworkVariable<EnemyState> _enemyState = new NetworkVariable<EnemyState>(
        EnemyState.move,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    protected Vector2 _direction = default;
    public Vector2 Direction => _direction;

    private bool _isFloating = false;
    public bool IsFloating => _isFloating;

    private Rigidbody2D _rigidBody = default;
    private Vector2 _saveDirection = Vector2.zero;

    [SerializeField, Header("浮遊慣性")]
    private float _inertiaStrangth = 4f;

    // プレイヤーの再探索タイマー（定期的に最寄りプレイヤーをターゲットにする）
    private float _findPlayerTimer = 0f;
    private const float FIND_PLAYER_INTERVAL = 0.5f;

    public virtual void Start()
    {
        _agent = GetComponent<NavMesh2DAgent>();
        _rigidBody = GetComponent<Rigidbody2D>();

        if (!IsServer && _agent != null)
        {
            _agent.enabled = false;
        }
    }

    public void ChangeFloat(bool value)
    {
        _isFloating = value;
    }

    public void RoadKill()
    {
        if (IsServer)
        {
            _enemyState.Value = EnemyState.roadKill;
        }
    }

    protected void FindPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag(PLAYERTAGNAME);
        if (players == null || players.Length == 0) return;

        GameObject nearestPlayer = null;
        float minDistance = float.MaxValue;
        Vector2 curPos = transform.position;

        foreach (GameObject p in players)
        {
            if (p == null || !p.activeInHierarchy) continue;
            float dist = Vector2.Distance(curPos, p.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearestPlayer = p;
            }
        }

        if (nearestPlayer != null)
        {
            _playerObject = nearestPlayer;
            _playerStateManager = _playerObject.GetComponent<PlayerStateManager>();
            _target = _playerObject.transform;
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        _findPlayerTimer += Time.fixedDeltaTime;
        if (_playerObject == null || !_playerObject.activeInHierarchy || _findPlayerTimer >= FIND_PLAYER_INTERVAL)
        {
            _findPlayerTimer = 0f;
            FindPlayer();
            if (_playerObject == null) return;
        }

        _cantMove = _enemyState.Value == EnemyState.knockback || 
                    _enemyState.Value == EnemyState.fall || 
                    _enemyState.Value == EnemyState.Wait;

        if (_cantMove) return;

        Moving();
    }

    public virtual void Moving()
    {
        if (_target == null) return;

        if (_isFloating)
        {
            float t = Time.fixedDeltaTime * _inertiaStrangth;
            _rigidBody.linearVelocity = Vector2.Lerp(_rigidBody.linearVelocity, _saveDirection, t);
        }
        else
        {
            if (_agent != null && _agent.isActiveAndEnabled)
            {
                _agent.destination = _target.position;
            }
            _saveDirection = ((Vector2)_target.position - (Vector2)transform.position).normalized * _moveSpeed;
        }
    }
}
