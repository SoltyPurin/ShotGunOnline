using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyState
{
    move,
    fall,
    knockback,
    roadKill,
    Wait
}

public class EnemyMove : MonoBehaviour
{
    //�����m�肵�Ă���G�F�������߂Â��Ă���G�ƒe�������Ă���G
    //�݂�Ȉړ����x�͒x�߁A�e�������Ă���G�́A�v���C���[�ƈ��̋��������ێ����悤�Ƃ���B

    //�ϐ��̓C���X�y�N�^�[���珑�������邽�߂����Ďq�I�u�W�F�N�g�ł͋L�q���Ȃ�

    protected GameObject _playerObject = default; //�v���C���[�̃I�u�W�F�N�g�p�̕ϐ�
    protected  readonly string PLAYERTAGNAME = "Player";//�v���C���[�̃^�O�̖��O�̕ϐ�
    //�v���C���[���W������Transform
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
    protected float _keepDistance = 0;//�v���C���[�Ǝ�鋗��
    //�ړ��s�\�̃t���O
    protected bool _cantMove = false;
    //���݂̃X�e�[�g
    protected EnemyState _enemyState = EnemyState.move;
    public EnemyState EnemyState
    {
        get { return _enemyState; }
        set { _enemyState = value; }//���EnemyTakeDamage���p�������X�N���v�g�B���珑�������Ă�
    }
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
        //�v���C���[�̃I�u�W�F�N�g��T�����đ��
        _playerObject = GameObject.FindWithTag(PLAYERTAGNAME);
        _playerStateManager = _playerObject.GetComponent<PlayerStateManager>();
        _target = _playerObject.transform;
        _agent = GetComponent<NavMesh2DAgent>(); //agent��NavMeshAgent2D���擾
        _rigidBody = GetComponent<Rigidbody2D>();
    }

    public void ChangeFloat(bool value)
    {
        _isFloating = value;
    }

    public void RoadKill()
    {
        _enemyState = EnemyState.roadKill;
    }
    private void FixedUpdate()
    {
        _cantMove = _enemyState == EnemyState.knockback || _enemyState == EnemyState.fall || _enemyState == EnemyState.Wait;
        if (_cantMove) //�m�b�N�o�b�N���܂��͗������͈ړ����Ȃ��悤�ɂ���
        {
            return;
        }
        Moving();//�ړ��̃��\�b�h�Ăяo��
    }

    public virtual void Moving()
    {
        if(_target == null)
        {
            return;
        }
        if(_isFloating)
        {
            Debug.Log("��������b�I");
            float t = Time.fixedDeltaTime * _inertiaStrangth;
            _rigidBody.linearVelocity = Vector2.Lerp(_rigidBody.linearVelocity,_saveDirection, t);

        }
        else
        {
            _agent.destination = _target.position; //agent�̖ړI�n��target�̍��W�ɂ���
            _saveDirection = (_target.position - transform.position) * _moveSpeed;
        }

    }
}
