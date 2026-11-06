using UnityEngine;

public class Bullet : MonoBehaviour
{
    private bool _canMove = false; //�e�e���ړ����Ă�悢��
    private Vector3 _moveDirection = default; //�ړ�����
    [Header("�e�e�̑��x")]
    [SerializeField] private float _moveSpeed = 5.0f;//�e�e�̑��x
    [Header("�e�e�̃I�u�W�F�N�g�o�^")]
    [SerializeField] private GameObject _bulletObject = default;
    private float _disableTimer = 0.0f; //���݂̏e�e�����݂��Ă鎞��
    private float _chargeTime = 0f; //�e�e�̐������ԁA����������̓`���[�W���Ԃɂ���ĕϓ�����
    [SerializeField, Header("�ő�˒�")]
    private int _maxRange = 15;
    public float ChargeTime
    {
        set { _chargeTime = value; }//�`���[�W���Ԃɂ���Đ������Ԃ��ς��
    }
    private const float MAXBULLETLIMITTIME = 4;//�ő吶�����ԁA��������`���[�W���Ԃ�����Đ������Ԃ���߂�
    private Vector3 _firstDirection = new Vector3(0, 0, 0);//�E�X�e�B�b�N�̓��͖������ǂ����𔻒f����
    private Vector3 _startPosition = default;

    /// <summary>
    /// �e�e�̌�����ݒ肷�郁�\�b�h
    /// </summary>
    /// <param name="direction">�v���C���[�̌����Ă������</param>
    /// <param name="position">�ˌ���s�����n�_</param>
    public void Init(Vector3 direction, Vector3 position)
    {
        if(_firstDirection == direction)//�X�e�B�b�N�ɓ��͂����Ɍ������ꍇ
        {
            _moveDirection = new Vector3(1, 0, 0);//�E�����ɒe�����
        }
        else
        {
            _moveDirection = direction;//�E�X�e�B�b�N�̓��͕�����ړ������ɑ��
        }
        _startPosition = position;
        transform.position = position;//�v���C���[�̃|�W�V��������X�^�[�g������
        _canMove = true;//�ړ��������
        //���炭����������MAXBULLETLIMITTIME����_chargeTime������V�����ϐ������ēn���K�v������

    }

    private void FixedUpdate()
    {
        if (_canMove)//�ړ����\��
        {
            Moveing(_moveDirection);//�ړ��̃��\�b�h�Ɉړ�������n��
        }
        else
        {
            return;//��������Ȃ���΃��^�[��
        }
    }
    /// <summary>
    /// �e�e�̈ړ��X�N���v�g
    /// </summary>
    /// <param name="direction">����������</param>
    private void Moveing(Vector3 direction)
    {
        float t = Mathf.Clamp01(_chargeTime / 2f);
        float radius = Mathf.Lerp(_maxRange, 5f, t); //�ő勗��
        float initDistance = (transform.position - _startPosition).magnitude;
        //�w������Ɉړ����x���Z���Ď��ԒP�ʂňړ�������
        transform.position += direction * _moveSpeed * Time.fixedDeltaTime;
        if(radius<= initDistance)//���݂̐i�s�����������ő勗���ɂ����珈����s��
        {
            _bulletObject.SetActive(false);//�e�e�̃I�u�W�F�N�g���A�N�e�B�u������
            _canMove = false;//�ړ����������
        }
    }
}
