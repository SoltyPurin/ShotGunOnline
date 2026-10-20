using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// �{�X�̎󂯂��_���[�W��Ǘ����郁�\�b�h
/// </summary>
public class BossHP : EnemyTakeDamage
{
    #region[�ϐ���]
    //---GameObject,Script,Animator��---------------------------------
    [SerializeField, Header("���S���̃��[�r�[")]
    private GameObject _deathTimeline = default;

    [SerializeField, Header("�e")]
    private GameObject _shadow = default;

    [SerializeField, Header("��������")]
    private HitFlash _hitFlash = default;

    [SerializeField, Header("Boss��State�Ǘ��̃X�N���v�g")]
    private BossStateManagement _stateManagement = default;

    [SerializeField, Header("�G���G�����̃X�N���v�g")]
    private SummonEnemy _summonEnemy = default;

    [SerializeField, Header("Boss��Anime�Ǘ��̃X�N���v�g")]
    private BossAnimeManager _animeManager = default;

    [SerializeField, Header("�{�X�̍U���p�A�j���[�V����")]
    private Animator _bossAnimator = default;

    [SerializeField, Header("�R���C�_�[�o�^")]
    private CircleCollider2D _collider = default;

    private GameObject[] _enemyObjects = default;
    private GameObject[] _enemyObjects2 = default;
    private GameObject _bossHPGage = default;
    [SerializeField]
    private GameObject _bossEffects = default;

    private PlayBossDeadExplosion _deathExplosion = default;
    private BossHPGageAnimeCon _hPGageAnimeCon = default;

    //---int,float�Ȃǂ̐��l---------------------------------
    [SerializeField, Header("�K�E�Z�̃_���[�W")]
    protected float _ultMultiplier = 1500;

    [SerializeField, Header("�����̃_���[�W")]
    private int _fallDamage = 1000;

    [SerializeField, Header("��ɂԂ��������̃_���[�W")]
    private int _stoneDamage = 700;

    public int BossHPVariable
    {
        get { return _enemyHP.Value; }
    }

    private float _firstHP = default;
    private float _halfFirstHP = default;


    //---bool------------------------------------------------
    internal bool _secondForm = false;
    internal bool _isInvincible = true;
    private bool _isAnimeGot = false;
    private bool canDieState = false;

    #endregion

    public override void Start()
    {
        base.Start();

        Scene currentScene = SceneManager.GetActiveScene();

        SetMaxHP(JsonSaver.Instance.EnemyJson.BossHP);
        _firstHP = JsonSaver.Instance.EnemyJson.BossHP;
        _halfFirstHP = _firstHP / 2;

        _isInvincible = false;
    }

    private void FixedUpdate()
    {
        bool isHPhalf = (_halfFirstHP >= _enemyHP.Value) && (_enemyHP.Value > 0);
        bool isStopState = (_stateManagement._currentState == BossStateManagement.BossState.Stop);


        if (isHPhalf && !_secondForm && isStopState)
        {
            _secondForm = true;
        }

        if (!_isAnimeGot)
        {
            _bossHPGage = GameObject.FindGameObjectWithTag("BossHPGageUI");
            _hPGageAnimeCon = _bossHPGage.GetComponent<BossHPGageAnimeCon>();
            _isAnimeGot = true;
        }
        else
        {
            return;
        }

    }

    public override void SetMirrorDamage(float chargeTime)
    {
        float damage = chargeTime * _damageMultiplier;
        damage = Mathf.Max(damage, _minDamage);
        _enemyHP.Value = (_enemyHP.Value - (int)damage);
        _playTheSEManager.PlayEnemyDamageSound();
        if (_enemyHP.Value <= 0)
        {
            StartCoroutine(DeathProtocol(chargeTime));
        }
    }
    public override void SetTakeDamege(float chargeTime, PlayerState state)
    {
        Debug.Log("��e�֐��Ăяo��");
        if (!IsServer) //�I�ȊO�͑����^�[��
        {
            return;
        }
        if (_isInvincible)//���G��ԂȂ瑦���^�[��
        {
            return;
        }
        Debug.Log("�{�X��e");
        BossTakeDamageEffectClientRpc();
            //_damageAnimator.SetTrigger("Damage");
            //_hitFlash.DoFlash();
            //    _hPGageAnimeCon.HitGageAnime();
                if (state == PlayerState.Ultimate)
                {
                    float damage = _ultMultiplier;
                    _enemyHP.Value = (_enemyHP.Value - (int)damage);

                    if (_stateManagement._currentState != BossStateManagement.BossState.JumpAtack
                && _stateManagement._currentState != BossStateManagement.BossState.Punch)
                    {
                        canDieState = true;
                    }

                    if (_enemyHP.Value <= 0 && canDieState)
                    {
                        DeadBossProcess();
                    }
                }
                else
                {
                    float damage = chargeTime * _damageMultiplier;
                    damage = Mathf.Max(damage, _minDamage);
                    _enemyHP.Value = (_enemyHP.Value - (int)damage);

                    if (_stateManagement._currentState != BossStateManagement.BossState.JumpAtack
                && _stateManagement._currentState != BossStateManagement.BossState.Punch)
                    {
                        canDieState = true;
                    }

                    if (_enemyHP.Value <= 0 && canDieState)
                    {
                        DeadBossProcess();
                    }
            Debug.Log("�c��̗�" + _enemyHP);
                }
    }


    [ClientRpc]
    private void BossTakeDamageEffectClientRpc()
    {
        _damageAnimator.SetTrigger("Damage");
        _hitFlash.DoFlash();
        _hPGageAnimeCon.HitGageAnime();
    }
    public override void FallDamage()
    {
        _collider.isTrigger = true;
        _enemyMove.EnemyState = EnemyState.fall;
        _enemyHP.Value -= _fallDamage;
        if (this.gameObject.activeInHierarchy)
        {
            _playTheSEManager.PlayDropSound();
            StartCoroutine(ReturnStage());
        }
    }

    private IEnumerator ReturnStage()
    {
        yield return new WaitForSeconds(2);
        _collider.isTrigger = false;
        _enemyMove.EnemyState = EnemyState.move;
    }

    public override void SetExplosionDamgage(float chargeTime)
    {
        float damage = _explosionMultiplier;
        _enemyHP.Value = (_enemyHP.Value - (int)damage);

        if (_stateManagement._currentState != BossStateManagement.BossState.JumpAtack
            && _stateManagement._currentState != BossStateManagement.BossState.Punch)
        {
            canDieState = true;
        }

        if (_enemyHP.Value <= 0 && canDieState)
        {
            DeadBossProcess();
        }
    }


    public override IEnumerator DeathProtocol(float chargeTime)
    {
        if(_coinKeep == null)
        {
            AssurePlayerReference();
        }
        float timeLeftUntilDeath = chargeTime / _deathTimeMultiplier;
        yield return new WaitForSeconds(timeLeftUntilDeath);
        _playTheSEManager.PlayBossDeathSound();
        _coinKeep.AdditionCoin();
        _enemyObjects = GameObject.FindGameObjectsWithTag("Enemy");
        _enemyObjects2 = GameObject.FindGameObjectsWithTag("Armor");
        foreach (GameObject enemy in _enemyObjects)
        {
            GameObject.Destroy(enemy);
        }
        foreach (GameObject enemy2 in _enemyObjects2)
        {
            GameObject.Destroy(enemy2);
        }
        DeathObjectClientRpc();
        // �l�b�g���[�N�I�u�W�F�N�g�̏ꍇ�� Despawn�A���[�J���̏ꍇ�� Destroy
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [ClientRpc]
    private void DeathObjectClientRpc()
    {
        _deathExplosion = GameObject.FindWithTag("ExplosionManager").GetComponent<PlayBossDeadExplosion>();
        Instantiate(_deathObject, transform.position, Quaternion.identity);
        _deathExplosion.Explosion(this.transform.position);
    }

    public override void ContactStoneMethod()
    {
        float damage = _stoneDamage;
        _enemyHP.Value = (_enemyHP.Value - (int)damage);
        _playTheSEManager.PlayRoadKill();

    }
    [ClientRpc]
    private void DeactivateBossClientRpc()
    {
        _stateManagement.enabled = false;
        _summonEnemy.enabled = false;
        _bossEffects.SetActive(false);

        _animeManager.ResetAllTriggers();
        _bossAnimator.SetBool("isDead", true);
    }

    private void DeadBossProcess()
    {
        DeactivateBossClientRpc();
        _collider.isTrigger = true;
        if (_deathTimeline != null)
        {
            _deathTimeline.SetActive(true);
        }
        _shadow.SetActive(false);
        StartCoroutine(DeathProtocol(1.0f));
    }
}
