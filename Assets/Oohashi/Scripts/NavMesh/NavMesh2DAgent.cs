using UnityEngine;
using UnityEngine.AI;

public class NavMesh2DAgent : MonoBehaviour
{
    [SerializeField,Header("移動速度")]
    internal float _moveSpeed = 1.0f;
    [SerializeField,Header("どれくらい近づいたら止まるか")]
    private float _stoppingDistance = 0;

    private EnemyMove _enemyMove = default;

    private float _initNotMoveTime = 0;

    [HideInInspector]//常にUnityエディタから非表示
    private Vector2 trace_area = Vector2.zero;
    public Vector2 destination
    {
        get { return trace_area; }
        set
        {
            trace_area = value;
            Trace(transform.position, value);
        }
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

    private void Trace(Vector2 current, Vector2 target)
    {
        if (Vector2.Distance(current, target) <= _stoppingDistance)
        {
            return;
        }
        if (_enemyMove.IsFloating)
        {
            return;
        }

        // NavMesh に応じて経路を求める
        NavMeshPath path = new NavMeshPath();
        NavMesh.CalculatePath(current, target, NavMesh.AllAreas, path);

        if (path.corners.Length == 0)
        {
            Vector3 direction = ((Vector3)target-this.transform.position).normalized;
            transform.position += direction * _moveSpeed * Time.deltaTime;
            return;
        }


        Vector2 corner = path.corners[0];

        if (path.corners.Length > 1 && Vector2.Distance(current, corner) <= 0.05f)
        {
            corner = path.corners[1];
        }

        transform.position = Vector2.MoveTowards(current, corner, _moveSpeed * Time.deltaTime);
    }

}
