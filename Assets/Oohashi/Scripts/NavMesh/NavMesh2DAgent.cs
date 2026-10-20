using UnityEngine;
using UnityEngine.AI;

public class NavMesh2DAgent : Unity.Netcode.NetworkBehaviour
{
    [SerializeField, Header("移動速度")]
    internal float _moveSpeed = 1.0f;

    [SerializeField, Header("どれくらい近づいたら止まるか")]
    private float _stoppingDistance = 0f;

    [SerializeField, Header("経路再計算インターバル(秒)")]
    private float _repathInterval = 0.15f;

    private EnemyMove _enemyMove = default;

    private Vector2 trace_area = Vector2.zero;
    private NavMeshPath _path;
    private int _currentCornerIndex = 0;
    private float _repathTimer = 0f;
    private Vector2 _lastTargetPos = Vector2.positiveInfinity;

    /// <summary>
    /// クライアント側でもローカルに移動計算を行うか(雑魚敵のローカルシミュレーション用)
    /// </summary>
    public bool LocalSimulation { get; set; } = false;
    private bool CanSimulate => IsServer || LocalSimulation;

    public Vector2 destination
    {
        get { return trace_area; }
        set
        {
            trace_area = value;
            if (CanSimulate)
            {
                UpdateMovement(value);
            }
        }
    }

    private void Awake()
    {
        _path = new NavMeshPath();
    }

    private void Start()
    {
        _enemyMove = GetComponent<EnemyMove>();
    }

    public void Stop()
    {
        _moveSpeed = 0;
    }

    public bool SetDestination(Vector2 target)
    {
        destination = target;
        return true;
    }

    /// <summary>
    /// FixedUpdateサイクルで呼び出される移動処理
    /// </summary>
    private void UpdateMovement(Vector2 target)
    {
        if (!CanSimulate) return;
        if (_enemyMove != null && _enemyMove.IsFloating) return;

        Vector2 current = transform.position;
        if (Vector2.Distance(current, target) <= _stoppingDistance)
        {
            return;
        }

        _repathTimer += Time.fixedDeltaTime;

        // 一定時間経過、または目的地が大きく動いた時のみNavMesh経路を再計算（CPU負荷＆振動防止）
        if (_repathTimer >= _repathInterval || Vector2.Distance(_lastTargetPos, target) > 1.0f || _path.corners.Length == 0)
        {
            _repathTimer = 0f;
            _lastTargetPos = target;
            NavMesh.CalculatePath(current, target, NavMesh.AllAreas, _path);
            _currentCornerIndex = 0;

            // corners[0] は現在地付近のため、corners[1] があれば最初からそこを目指す
            if (_path.corners.Length > 1 && Vector2.Distance(current, _path.corners[0]) <= 0.2f)
            {
                _currentCornerIndex = 1;
            }
        }

        // 経路が見つからなかった、または直線で進める場合のフォールバック
        if (_path.corners == null || _path.corners.Length == 0)
        {
            Vector2 directDir = (target - current).normalized;
            transform.position += (Vector3)directDir * _moveSpeed * Time.fixedDeltaTime;
            return;
        }

        // 現在目指している経由地点（Corner）
        if (_currentCornerIndex < _path.corners.Length)
        {
            Vector2 targetCorner = _path.corners[_currentCornerIndex];
            float distToCorner = Vector2.Distance(current, targetCorner);

            // 経由地に近づいたら次の経由地へ
            if (distToCorner <= 0.2f && _currentCornerIndex < _path.corners.Length - 1)
            {
                _currentCornerIndex++;
                targetCorner = _path.corners[_currentCornerIndex];
            }

            // FixedDeltaTime で均一に移動（Time.deltaTimeの混入によるガタつきを防止）
            transform.position = Vector2.MoveTowards(current, targetCorner, _moveSpeed * Time.fixedDeltaTime);
        }
        else
        {
            transform.position = Vector2.MoveTowards(current, target, _moveSpeed * Time.fixedDeltaTime);
        }
    }
}
