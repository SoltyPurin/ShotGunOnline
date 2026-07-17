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
    protected  readonly string PLAYERTAGNAME = "Player";
    protected Transform _target = default;
    protected NavMesh2DAgent _agent; //NavMeshAgent2D���g�p���邽�߂̕ϐ�
    private PlayerStateManager _playerStateManager;
    [SerializeField, Header("�������ȊO�ł͕ύX���Ă��Ӗ��Ȃ�")]
    protected float _moveSpeed = 5;//�ړ����x
    public float MoveSpeed
    {
        get { return _moveSpeed; }
    }
    [SerializeField, Header("�v���C���[�Ǝ�鋗���A���b�N�I���ȊO���ς��Ă��Ӗ��Ȃ�")]
    protected float _keepDistance = 0;
    //�ړ��s�\�̃t���O
    protected bool _cantMove = false;
    //���݂̃X�e�[�g
    //protected EnemyState _enemyState = EnemyState.move;
    public EnemyState EnemyState
    {
        get { return _enemyState.Value; }
        set {
            if (IsServer) // サーバー側でのみ書き込みを許可する安全対策
            {
                _enemyState.Value = value;
            }
        }
        //set { _enemyState.Value = value; }//���EnemyTakeDamage���p�������X�N���v�g�B���珑�������Ă�
    }

    protected NetworkVariable<EnemyState> _enemyState = new NetworkVariable<EnemyState>(
        EnemyState.move,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server // 書き込みはサーバーのみ
    );
    //����������
    protected Vector2 _direction = default;
    public Vector2 Direction
    {
        get { return _direction; }
    }
    private bool _isFloating = false;
    public bool IsFloating
    {
        get { return _isFloating; }
    }

    private Rigidbody2D _rigidBody = default;

    private Vector2 _saveDirection = Vector2.zero;

    [SerializeField, Header("�����")]
    private float _inertiaStrangth = 4;

    public virtual void Start()
    {
        _agent = GetComponent<NavMesh2DAgent>(); //agent��NavMeshAgent2D���擾
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
        _enemyState.Value = EnemyState.roadKill;
    }

    protected void FindPlayer()
    {
        // ネットワーク上の全プレイヤーオブジェクトを探す
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject p in players)
        {
            NetworkObject netObj = p.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsLocalPlayer)
            {
                _playerObject = p;
                _playerStateManager = _playerObject.GetComponent<PlayerStateManager>();
                _target = _playerObject.transform;
                break;
            }
        }

    }
    private void FixedUpdate()
    {
        if (!IsServer)
        {
            return;
        }
        if (_playerObject == null)
        {
            Debug.Log("プレイヤーが見当たらない");
            FindPlayer();
            return;
        }
        _cantMove = _enemyState.Value == EnemyState.knockback || _enemyState.Value == EnemyState.fall || _enemyState.Value == EnemyState.Wait;
        if (_cantMove) 
        {
            return;
        }
        //if (this.IsServer)
        //{
            Moving();
        //}
        //else
        //{
        //    Debug.Log("IsServerがfalse");
        //}
    }

    public virtual void Moving()
    {
        Debug.Log("敵動く");
        if(_target == null)
        {
            return;
        }
        if(_isFloating)
        {
            float t = Time.fixedDeltaTime * _inertiaStrangth;
            _rigidBody.linearVelocity = Vector2.Lerp(_rigidBody.linearVelocity,_saveDirection, t);

        }
        else
        {
            if (_agent != null && _agent.isActiveAndEnabled)
            {
                _agent.destination = _target.position;
            }
            _saveDirection = (_target.position - transform.position) * _moveSpeed;
            //_agent.destination = _target.position; //agent�̖ړI�n��target�̍��W�ɂ���
            //_saveDirection = (_target.position - transform.position) * _moveSpeed;
        }

    }
}
