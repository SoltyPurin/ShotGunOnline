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
    protected float _findPlayerTimer = 0f;
    protected const float FIND_PLAYER_INTERVAL = 0.5f;

    [SerializeField, Header("クライアントで敵の移動をローカル計算する(雑魚敵向け)")]
    private bool _localSimulation = false;
    // ローカルシミュレーションを実際に行っているか
    protected bool _localSimulationActive = false;
    private EnemyNetworkTransform _enemyNetworkTransform = default;

    // サーバー位置とのずれがこの距離を超えたら徐々に補正する
    private const float CLIENT_CORRECT_DISTANCE = 1.5f;
    // サーバー位置とのずれがこの距離を超えたら即座に合わせる
    private const float CLIENT_SNAP_DISTANCE = 5f;
    private const float CLIENT_CORRECT_RATE = 4f;
    // サーバー制御中(ノックバック等)にサーバー位置へ追従する速さ (急峻なノックバックに追従するため高速化)
    private const float CLIENT_FOLLOW_RATE = 45f;

    // クライアント側でローカル先行でノックバック中と判定されているか
    private bool _isClientKnockbackPending = false;
    private float _clientKnockbackTimer = 0f;
    private const float CLIENT_KNOCKBACK_TIMEOUT = 0.5f;
    private const float CLIENT_PREDICTION_MAX_DURATION = 0.15f;

    // クライアント先行ノックバック移動予測（RTT中の前進食い込み・手応え遅延を解消）
    private bool _hasKnockbackPrediction = false;
    private float _predictionElapsed = 0f;
    private Vector2 _predictedKnockbackDir = Vector2.zero;
    private float _predictedKnockbackForce = 0f;
    private float _predictedKnockbackDecel = 0.5f;
    private static readonly RaycastHit2D[] _predictWallHits = new RaycastHit2D[4];

    // 直前フレームでサーバー制御中だったか（復帰スナップ用）
    private bool _wasServerControlled = false;

    public virtual void Start()
    {
        _agent = GetComponent<NavMesh2DAgent>();
        _rigidBody = GetComponent<Rigidbody2D>();

        _enemyNetworkTransform = GetComponent<EnemyNetworkTransform>();
        // ローカルシミュレーションは、EnemyNetworkTransformが付いている場合のみ有効
        _localSimulationActive = !IsServer && _localSimulation && _enemyNetworkTransform != null;

        if (_localSimulationActive)
        {
            _enemyNetworkTransform.LocalSimulation = true;
            if (_agent != null)
            {
                _agent.LocalSimulation = true;
            }
        }
        else if (!IsServer && _agent != null)
        {
            _agent.enabled = false;
        }
    }

    /// <summary>
    /// クライアント側でノックバックが開始された瞬間に呼び出し、前進移動を即座に停止してサーバー位置追従へ移行させる
    /// </summary>
    public void OnKnockBackStarted()
    {
        if (IsServer) return;
        _isClientKnockbackPending = true;
        _clientKnockbackTimer = CLIENT_KNOCKBACK_TIMEOUT;
        if (_rigidBody != null)
        {
            _rigidBody.linearVelocity = Vector2.zero;
        }
    }

    /// <summary>
    /// 攻撃したクライアント端末上でRTT待機なしにノックバック初速・硬直を即時予測反映する
    /// </summary>
    public void OnKnockBackPredicted(Vector2 blowDirection, float initialForce, float deceleration)
    {
        if (IsServer) return;
        OnKnockBackStarted();
        _hasKnockbackPrediction = true;
        _predictionElapsed = 0f;
        _predictedKnockbackDir = blowDirection;
        _predictedKnockbackForce = Mathf.Max(0f, initialForce);
        _predictedKnockbackDecel = Mathf.Max(0.05f, deceleration);
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

    /// <summary>
    /// クライアント側のローカルシミュレーション。
    /// 通常移動中はローカルで移動し、サーバー位置と大きくずれたときのみ補正する。
    /// ノックバックや落下などサーバーが位置を確定する状態では、サーバー位置に追従する。
    /// </summary>
    private void ClientSimulate()
    {
        Vector2 serverPos = _enemyNetworkTransform.ServerPosition;
        Vector3 curPos = transform.position;

        // クライアント側の先行ノックバックタイマー更新
        if (_isClientKnockbackPending)
        {
            _clientKnockbackTimer -= Time.fixedDeltaTime;
            // サーバーから正式にknockbackステートが届いたか、タイムアウトしたら先行フラグを解除
            if (_enemyState.Value == EnemyState.knockback || _clientKnockbackTimer <= 0f)
            {
                _isClientKnockbackPending = false;
                _hasKnockbackPrediction = false;
            }
        }

        bool isKnockback = _enemyState.Value == EnemyState.knockback || _isClientKnockbackPending;
        bool serverControlled = _enemyState.Value != EnemyState.move || _isFloating || isKnockback;

        if (serverControlled)
        {
            _wasServerControlled = true;

            // サーバー側がノックバック処理を開始するまでのRTT（約50〜120ms）の間は、
            // 古い前進中のServerPositionに引っ張られずローカル予測速度（またはヒットストップ静止）を適用する
            if (_isClientKnockbackPending && _hasKnockbackPrediction && _predictionElapsed < CLIENT_PREDICTION_MAX_DURATION)
            {
                _predictionElapsed += Time.fixedDeltaTime;
                if (_predictedKnockbackForce > 0f)
                {
                    float stepDist = _predictedKnockbackForce * Time.fixedDeltaTime;
                    int hitCount = Physics2D.Raycast(curPos, _predictedKnockbackDir, ContactFilter2D.noFilter, _predictWallHits, stepDist + 0.4f);
                    for (int i = 0; i < hitCount; i++)
                    {
                        Collider2D col = _predictWallHits[i].collider;
                        if (col != null && !col.isTrigger && col.CompareTag("Wall"))
                        {
                            _predictedKnockbackForce *= 0.2f;
                            stepDist = Mathf.Max(0f, _predictWallHits[i].distance - 0.4f);
                            break;
                        }
                    }

                    Vector2 nextPos = (Vector2)curPos + _predictedKnockbackDir * stepDist;
                    transform.position = new Vector3(nextPos.x, nextPos.y, curPos.z);
                    _predictedKnockbackForce = Mathf.Max(0f, _predictedKnockbackForce - _predictedKnockbackDecel);
                }
                return;
            }

            // ノックバック中などは高速でサーバー位置に食らいつく
            float rate = isKnockback ? CLIENT_FOLLOW_RATE : 25f;
            float follow = 1f - Mathf.Exp(-rate * Time.fixedDeltaTime);
            Vector2 followed = Vector2.Lerp(curPos, serverPos, follow);
            transform.position = new Vector3(followed.x, followed.y, curPos.z);
            return;
        }

        // サーバー制御（ノックバック等）から通常移動に復帰した瞬間は、中途半端なズレを残さないようサーバー到達位置にスナップ
        if (_wasServerControlled)
        {
            _wasServerControlled = false;
            transform.position = new Vector3(serverPos.x, serverPos.y, curPos.z);
            curPos = transform.position;
        }

        _findPlayerTimer += Time.fixedDeltaTime;
        if (_playerObject == null || !_playerObject.activeInHierarchy || _findPlayerTimer >= FIND_PLAYER_INTERVAL)
        {
            _findPlayerTimer = 0f;
            FindPlayer();
            if (_playerObject == null) return;
        }

        Moving();

        curPos = transform.position;
        float error = Vector2.Distance(curPos, serverPos);
        if (error > CLIENT_SNAP_DISTANCE)
        {
            transform.position = new Vector3(serverPos.x, serverPos.y, curPos.z);
        }
        else if (error > CLIENT_CORRECT_DISTANCE)
        {
            float correct = 1f - Mathf.Exp(-CLIENT_CORRECT_RATE * Time.fixedDeltaTime);
            Vector2 corrected = Vector2.Lerp(curPos, serverPos, correct);
            transform.position = new Vector3(corrected.x, corrected.y, curPos.z);
        }
    }

    public virtual void FixedUpdate()
    {
        if (!IsServer)
        {
            if (_localSimulationActive)
            {
                ClientSimulate();
            }
            return;
        }

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
