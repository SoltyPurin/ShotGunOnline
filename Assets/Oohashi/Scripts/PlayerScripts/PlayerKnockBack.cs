using Unity.Netcode;
using UnityEngine;

public class PlayerKnockBack : NetworkBehaviour
{
    //[Header("���W�b�h�{�f�B��ݒ�")]
    //[SerializeField] private Rigidbody2D _rigidbody = default;
    [Header("�v���C���[�̃I�u�W�F�N�g")]
    [SerializeField] private GameObject _playerObject = default;

    [Header("�X�N���v�g��ݒ�")]
    [SerializeField] private PlayerMove _playerMove = default;

    [SerializeField, Header("�X�e�[�g����肷��X�N���v�g")]
    private PlayerStateManager _playerState = default;

    [SerializeField,Header("�v���C���[�̃A�j���[�V�����Đ��X�N���v�g")]
    private PlayerAnimation _animation = default;

    private Rigidbody2D _rigidBody = default;

    private NetworkVariable<bool> _isKnockBacking = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner); //�m�b�N�o�b�N����

    public bool IsKnockBacking
    {
        get { return _isKnockBacking.Value;}
    }

    private Vector3 _minusDiretion = default; //���Ă�����Ƃ͋t����������

    private float _powValue = default; //�`���[�W���Ԃ̏搔������

    private  NetworkVariable<float> _force = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );//���`�␳�����l������

    private bool _isInputLeftStick = false; //���X�e�B�b�N����͂��Ă邩�ǂ���

    //�萔
    private readonly float KNOCKBACK_FORCE = 5.0f;


    public bool IsInputLeftStick
    {
        set { _isInputLeftStick = value; }
    }

    private void Start()
    {
        _rigidBody = GetComponent<Rigidbody2D>();
    }

    public void SetDirection(Vector3 direction,float chargeTime)
    {
        if (!IsOwner || !IsSpawned)
        {
            return;
        }
        //���݌��Ă�����Ƌt�̕�����o��
        _minusDiretion = (direction * -1).normalized;
        //�`���[�W���Ԃ̏����߂�
        _powValue = Mathf.Pow(chargeTime, 5f);
        //�Œ�1����powValue�܂ł̊ԂŐ��`�␳��s���A�`���[�W�l���Ƃ̐�����΂���Ȃ߂炩��
        _force.Value = Mathf.Lerp(3, _powValue, 0.5f);

        _isKnockBacking.Value = true;
        //���Ă�����Ɣ��Ε����ɗ͂������
        //������т��x�����߃t���t���Ɍ�����
    }

    public void Fall()
    {
        if (!IsOwner || !IsSpawned)
        {
            return;
        }

        _force.Value = 0;
    }

    public void TakeDamage()
    {
        if (!IsOwner || !IsSpawned)
        {
            return;
        }

        _force.Value = 0;
    }


    private void FixedUpdate()
    {
        if (!IsOwner || !IsSpawned)
        {
            return;
        }

        if (!_isKnockBacking.Value)
        {
            return;
        }
        if(_playerState.PlayerState == PlayerState.DamageKnockBack)
        {
            return;
        }
        KnockBacking();
    }

    private void Update()
    {
        // ��e�m�b�N�o�b�N���͔�e�A�j����D��
        if (_playerState != null && _playerState.PlayerState == PlayerState.DamageKnockBack) return;

        //�����m�b�N�o�b�N���̂݃A�j���[�V������K�p����
        if (_isKnockBacking.Value)
        {
            if (_animation != null)
            {
                if (_force.Value >= KNOCKBACK_FORCE)
                {
                    _animation.KnockBack();
                }

            }
        }
    }

    private void KnockBacking()
    {
        //���Ă�����Ɣ��Ε����ɗ͂������
        transform.position += _minusDiretion * _force.Value * Time.fixedDeltaTime;
        _playerMove.SaveDirection = _minusDiretion * _force.Value;

        if(_force.Value >= KNOCKBACK_FORCE)
        {
            _animation.KnockBack();
        }

        // 秒単位の減衰率 (0.02s換算で 元の 0.5f -> 25f/s, 0.2f -> 10f/s)
        float decayRate = _isInputLeftStick ? 25f : 10f;
        _force.Value -= decayRate * Time.fixedDeltaTime;

        if (_force.Value <= 0f)
        {
            _force.Value = 0f;
            _animation.Wait();
            _isKnockBacking.Value = false;
        }
    }

}
