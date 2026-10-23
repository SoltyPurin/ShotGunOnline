using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using Unity.Netcode;
using UnityEngine;

public class MediumArmorTakeDamage : ArmorTakeDamage
{
    #region Serialize�ϐ�
    [SerializeField, Header("��ŋ�炤�_���[�W")]
    private float _stoneTakeDamage = 700;
    [SerializeField, Header("�E���g�ŋ�炤�_���[�W")]
    private float _ultDamage = 1500;
    [SerializeField, Header("�����ŋ�炤�_���[�W")]
    private float _fallTakeDamage = 700;
    [SerializeField, Header("�X�e�[�W�ɖ߂�X�N���v�g")]
    private BossFallToCliffAndHole _bossFall = default;
    #endregion

    #region �萔

    private readonly string DAMAGEEFFECTTRIGERNAME = "Damage";
    private readonly string FALLCOROUTINENAME = "HoleFallMethod";
    #endregion

    #region �ϐ�

    private Vector2 _returnPosition = new Vector2(0, 10);

    private MidiumCameraSW _cameraSW = default;

    #endregion

    public override void Start()
    {
        base.Start();
        _cameraSW = GetComponent<MidiumCameraSW>();
        SetMaxHP(JsonSaver.Instance.EnemyJson.ArmorHP);
    }
    /// <summary>
    /// ���{�X�̓E���g���ȂȂ��̂ŏ�������
    /// </summary>
    /// <param name="chargeTime">�`���[�W����</param>
    /// <param name="state">�v���C���[�̃X�e�[�g</param>
    public override void SetTakeDamege(float chargeTime, PlayerState state)
    {
        //�v���C���[�̃X�e�[�g���E���g��������_���[�W��E���g�_���[�W�ɐ؂�ւ���
        //����̓K�[�h�s�\
        if (state == PlayerState.Ultimate)
        {
            _damage = _ultDamage;
        }
        bool isGuard = _armorKnockbackScript.IsFromFlont;
        //�K�[�h�����ꍇ�̏���
        if (isGuard)
        {
            _damage = chargeTime * _guardMultiplier;
            _damage = Mathf.Max(_damage, _guardMinDamage);
            //_guardEffect.SetTrigger(DAMAGEEFFECTTRIGERNAME);
            TakeDamageEffectClientRpc(true);
        }
        else //���ʂȂǂ���U�������ꍇ�̏���
        {
            _damage = chargeTime * _damageMultiplier;
            _damage = Mathf.Max(_damage, _minDamage);
            //_damageAnimator.SetTrigger(DAMAGEEFFECTTRIGERNAME);
            //_damageMaterial.Damage();
            TakeDamageEffectClientRpc(false);

        }
        //HP����_���[�W�������đ���AHP�o�[��X�V
        _enemyHP.Value = (_enemyHP.Value - (int)_damage);
        _hpUI.UpdateHP(_enemyHP.Value);
        if (_enemyHP.Value <= 0)
        {
            StartCoroutine(DeathProtocol(chargeTime));
        }

    }

    /// <summary>
    /// ��e���̉��o��Đ����郁�\�b�h
    /// </summary>
    /// <param name="isGuard">�K�[�h�������ǂ���</param>
    [ClientRpc]
    private void TakeDamageEffectClientRpc(bool isGuard)
    {
        if (isGuard) {
            _guardEffect.SetTrigger(DAMAGEEFFECTTRIGERNAME);
        }
        else
        {
            _damageAnimator.SetTrigger(DAMAGEEFFECTTRIGERNAME);
            _damageMaterial.Damage();
        }
    }

    /// <summary>
    /// ��ɐڐG�����Ƃ��̃��\�b�h�A�ʏ�ƈႢ�ꌂ�ł͎��ȂȂ�
    /// </summary>
    public override void ContactStoneMethod()
    {
        //�_���[�W�Ɋ�̐ڐG�_���[�W����
        _damage = _stoneTakeDamage;
        //hp����炵��hp�o�[��X�V
        _enemyHP.Value = (_enemyHP.Value - (int)_damage);
        _hpUI.UpdateHP(_enemyHP.Value);
        //�̗͂�0�ɂȂ�����1�b��Ɏ��S�̃R���[�`�����s
        if (_enemyHP.Value <= 0)
        {
            StartCoroutine(DeathProtocol(1));
        }
    }

    /// <summary>
    /// �����̃_���[�W
    /// </summary>
    public override void FallDamage()
    {
        //�X�e�[�g�𗎉����ɂ���
        _enemyMove.EnemyState = EnemyState.fall;
        //hp���痎���_���[�W������đ̗̓o�[��X�V
        _enemyHP.Value -= (int)_fallTakeDamage;
        _hpUI.UpdateHP(_enemyHP.Value);
        //�����̌��ʉ��炷
        //_playTheSEManager.PlayDropSound();
        FallSoundClientRpc();
        //���A�̃R���[�`���N��
        _bossFall.StartCoroutine(FALLCOROUTINENAME);
        //�X�e�[�W�ɖ߂�
        StartCoroutine(ReturnStage());
        if (_enemyHP.Value <= 0)
        {
            StartCoroutine(DeathProtocol(1));
        }
    }

    [ClientRpc]
    private void FallSoundClientRpc()
    {
        _playTheSEManager.PlayDropSound();
    }
    /// <summary>
    /// �X�e�[�W�ɖ߂��Ă���R���[�`��
    /// </summary>
    /// <returns>�A�j���[�V�����̍Đ����ԕ��҂�</returns>
    private IEnumerator ReturnStage()
    {
        yield return new WaitForSeconds(2);
        //�ړ��̃X�e�[�g�ɕύX
        _enemyMove.EnemyState = EnemyState.move;
        //�����n�_�ɖ߂�
        this.transform.position = _returnPosition;

    }

    public override IEnumerator DeathProtocol(float chargeTime)
    {
        _cameraSW.SwitchOff();
        return base.DeathProtocol(chargeTime);
    }

}
