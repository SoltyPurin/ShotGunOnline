using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombTakeDamage : EnemyTakeDamage
{
    [SerializeField,Header("�����v���g�R���̃X�N���v�g")]
    private BombProtocol _bombProtocol = default;
    //�����������ǂ����̃t���O
    private bool _hasExploted = false;

    public override void Start()
    {
        base.Start();
        SetMaxHP(JsonSaver.Instance.EnemyJson.BombHP);
    }

    /// <summary>
    /// �����̃_���[�W��ݒ�
    /// </summary>
    /// <param name="chargeTime">�`���[�W����</param>
    public override void SetExplosionDamgage(float chargeTime)
    {
        if (_hasExploted) return;
        float damage = chargeTime * _explosionMultiplier;
        _enemyHP.Value = (_enemyHP.Value - (int)damage);
        if (_enemyHP.Value <= 0)
        {
            _isDead = true;
            _bombProtocol.BombCircleCheck();
            _hasExploted = true;
            Destroy(this.gameObject);
        }
    }

    /// <summary>
    /// ���S���̃R���[�`��
    /// </summary>
    /// <param name="chargeTime">�`���[�W����</param>
    /// <returns></returns>
    public override IEnumerator DeathProtocol(float chargeTime)
    {
        _isDead = true;
        float timeLeftUntilDeath = chargeTime / _deathTimeMultiplier;
        _playTheSEManager.PlayEnemyDeathSound();
        ComboCounter counter = GameObject.FindWithTag(COMBOCOUNTER_TAGNAME).GetComponent<ComboCounter>();
        //�R���{�J�E���^�[�Ƀv���X
        counter.ComboPlus();
        yield return new WaitForSeconds(timeLeftUntilDeath);
        _bombProtocol.BombCircleCheck();
        _playTheSEManager.PlayMoneyDropSound();
        Instantiate(_deathObject, transform.position, Quaternion.identity);
        Destroy(this.gameObject);

    }

    /// <summary>
    /// �v���C���[�ɐڐG
    /// </summary>
    public void PlayerCollision()
    {
        Destroy(this.gameObject);
    }
    /// <summary>
    /// �����ł͂Ȃ��ʏ�̎��S
    /// </summary>
    public void NormalDeath()
    {
        Destroy(this.gameObject);
    }
}