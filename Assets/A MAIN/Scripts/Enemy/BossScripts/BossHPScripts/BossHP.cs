using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ボスの受けたダメージを管理するメソッド
/// </summary>
public class BossHP : EnemyTakeDamage
{
    #region[変数名]
    //---GameObject,Script,Animator等---------------------------------
    [SerializeField, Header("死亡時のムービー")]
    private GameObject _deathTimeline = default;

    [SerializeField, Header("影")]
    private GameObject _shadow = default;

    [SerializeField, Header("発光処理")]
    private HitFlash _hitFlash = default;

    [SerializeField, Header("BossのState管理のスクリプト")]
    private BossStateManagement _stateManagement = default;

    [SerializeField, Header("雑魚敵召喚のスクリプト")]
    private SummonEnemy _summonEnemy = default;

    [SerializeField, Header("BossのAnime管理のスクリプト")]
    private BossAnimeManager _animeManager = default;

    [SerializeField, Header("ボスの攻撃用アニメーション")]
    private Animator _bossAnimator = default;

    [SerializeField, Header("コライダー登録")]
    private CircleCollider2D _collider = default;

    private GameObject[] _enemyObjects = default;
    private GameObject[] _enemyObjects2 = default;
    private GameObject _bossHPGage = default;
    [SerializeField]
    private GameObject _bossEffects = default;

    private PlayBossDeadExplosion _deathExplosion = default;
    private BossHPGageAnimeCon _hPGageAnimeCon = default;

    //---int,floatなどの数値---------------------------------
    [SerializeField, Header("必殺技のダメージ")]
    protected float _ultMultiplier = 1500;

    [SerializeField, Header("落下のダメージ")]
    private int _fallDamage = 1000;

    [SerializeField, Header("岩にぶつかった時のダメージ")]
    private int _stoneDamage = 700;

    public int BossHPVariable
    {
        get { return _enemyHP; }
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
        _deathExplosion = GameObject.FindWithTag("ExplosionManager").GetComponent<PlayBossDeadExplosion>();

        Scene currentScene = SceneManager.GetActiveScene();

        _enemyHP = JsonSaver.Instance.EnemyJson.BossHP;
        _firstHP = _enemyHP;
        _halfFirstHP = _firstHP / 2;
    }

    private void FixedUpdate()
    {
        bool isHPhalf = (_halfFirstHP >= _enemyHP) && (_enemyHP > 0);
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
        _enemyHP = (_enemyHP - (int)damage);
        _playTheSEManager.PlayEnemyDamageSound();
        if (_enemyHP <= 0)
        {
            StartCoroutine(DeathProtocol(chargeTime));
        }
    }
    public override void SetTakeDamege(float chargeTime, PlayerState state)
    {
        _damageAnimator.SetTrigger("Damage");
        _hitFlash.DoFlash();
        if (!_isInvincible)
        {
            _hPGageAnimeCon.HitGageAnime();
            if (state == PlayerState.Ultimate)
            {
                float damage = _ultMultiplier;
                _enemyHP = (_enemyHP - (int)damage);

                if (_stateManagement._currentState != BossStateManagement.BossState.JumpAtack
            && _stateManagement._currentState != BossStateManagement.BossState.Punch)
                {
                    canDieState = true;
                }

                if (_enemyHP <= 0 && canDieState)
                {
                    DeadBossProcess();
                }
            }
            else
            {
                float damage = chargeTime * _damageMultiplier;
                damage = Mathf.Max(damage, _minDamage);
                _enemyHP = (_enemyHP - (int)damage);

                if (_stateManagement._currentState != BossStateManagement.BossState.JumpAtack
            && _stateManagement._currentState != BossStateManagement.BossState.Punch)
                {
                    canDieState = true;
                }

                if (_enemyHP <= 0 && canDieState)
                {
                    DeadBossProcess();
                }
            }
        }

    }

    public override void FallDamage()
    {
        _collider.isTrigger = true;
        _enemyMove.EnemyState = EnemyState.fall;
        _enemyHP -= _fallDamage;
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
        _enemyHP = (_enemyHP - (int)damage);

        if (_stateManagement._currentState != BossStateManagement.BossState.JumpAtack
            && _stateManagement._currentState != BossStateManagement.BossState.Punch)
        {
            canDieState = true;
        }

        if (_enemyHP <= 0 && canDieState)
        {
            DeadBossProcess();
        }
    }


    public override IEnumerator DeathProtocol(float chargeTime)
    {
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
        Instantiate(_deathObject, transform.position, Quaternion.identity);
        _deathExplosion.Explosion(this.transform.position);
        Destroy(this.gameObject);

    }

    public override void ContactStoneMethod()
    {
        float damage = _stoneDamage;
        _enemyHP = (_enemyHP - (int)damage);
        _playTheSEManager.PlayRoadKill();

    }

    private void DeactivateBoss()
    {
        _stateManagement.enabled = false;
        _summonEnemy.enabled = false;
        _bossEffects.SetActive(false);

        _animeManager.ResetAllTriggers();
        _bossAnimator.SetBool("isDead", true);
    }

    private void DeadBossProcess()
    {
        DeactivateBoss();
        _collider.isTrigger = true;
        _deathTimeline.SetActive(true);
        _shadow.SetActive(false);
    }
}
