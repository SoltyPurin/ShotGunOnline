using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
public class BossMove : EnemyMove
{
    #region
    public enum BossForm
    {
        First,
        Second
    }
    public enum BossState
    {
        Stop,//次の行動を思考
        Approach,//プレイヤーに接近
        Shot,//遠距離攻撃
        JumpAtack,//近距離攻撃
        SpecialMove,//必殺技
        Cooldown,//攻撃後の後隙
        Stun,//スタン状態
        Drop //落下中
    }
    protected BossForm _initBossForm = BossForm.First;
    protected BossState _currentState = BossState.Cooldown;
    public BossState CurrentState
    {
        get { return _currentState; }
    }

    

    protected float _shotCoolTimer;
    protected float _actionCoolTimer = 0;
    protected bool _isSpecialMovement = false;
    protected bool _isFollow = true;
    protected bool _canShot = false;
    protected bool _canAction = true;
    protected bool _isSecondForm = false;


    [SerializeField, Header("ボスの攻撃用アニメーター")]
    protected Animator _bossAnimator;

    [SerializeField, Header("ボスが接近を行う距離")]
    protected float _needApproachDistance = 7;

    [SerializeField, Header("ボスがJumpAtackを行う距離")]
    protected float _jumpAtackDistance = 7;

    [SerializeField, Header("ボスが遠距離攻撃を行う距離")]
    protected float _shotDistance = 7;

    [SerializeField, Header("銃弾のオブジェクトを入れる配列")]
    protected GameObject[] _bulletObject = new GameObject[3];

    [SerializeField, Header("衝撃波のオブジェクト")]
    protected GameObject _shockWave;

    [SerializeField, Header("着地時の当たり判定オブジェクト")]
    protected GameObject _jumpAtackArea;

    [SerializeField, Header("必殺技の銃弾")]
    protected GameObject _specialMoveBullet;

    [SerializeField, Header("各アクション後の隙")]
    protected float _actionCoolTime = 1.0f;

    [SerializeField, Header("遠距離攻撃のクールタイム")]
    protected float _shotCoolTime = 1.0f;

    [SerializeField, Header("接触判定のコライダー")]
    protected CircleCollider2D _circleCollider2D = default;

    [SerializeField, Header("JumpAtackの回数")]
    protected int _jumpAtackCount;

    private PlayTheBombEffect _playTheBombEffect = default;

    private BossHP _bossHP = default;

    protected int[] _bossHPInts = { 6000, 2000, -10000};
    protected float[] _langeList = { 15, 0, -15 };
    protected float[] _shockLangeList = { 0, 45, 90, 135, 180, -45, -90, -135 };

    private int _bossHPIntsNum = 0;
    protected int _jumpAtackCounter = 0;
    protected float _enemyToPlayerDistance = 0;

    private Coroutine _specialMoveCoroutine;
    private bool _hasTriggeredFall = false;
    protected bool _hasTriggerSpecialMove = false;
    #endregion

    protected virtual void Start()
    {
        _shotCoolTimer = _shotCoolTime;
        _playTheBombEffect = GameObject.FindWithTag("EffectManager").GetComponent<PlayTheBombEffect>();
        _playerObject = GameObject.FindWithTag(PLAYERTAGNAME);//プレイヤーのオブジェクトを探索して代入
        _target = _playerObject.transform;
        //最初にプレイヤーの方向を計算してみる
        _direction = (_playerObject.transform.position - transform.position).normalized;
        _bossHP = gameObject.GetComponent<BossHP>();
        _agent = GetComponent<NavMesh2DAgent>(); //agentにNavMeshAgent2Dを取得
    }

    public void ChangeBossForm()
    {
        _initBossForm = BossForm.Second;
        _isSecondForm = true;
    }

    private void FixedUpdate()
    {
        _enemyToPlayerDistance = Vector2.Distance(_playerObject.transform.position,
            this.transform.position);
        _shotCoolTimer += Time.fixedDeltaTime;//撃った後の後隙時間計測

        // 通常の状態処理
        StateAction();

    }

    protected void StateAction()
    {
        DebugCode();
        float random = Random.value;

        switch (_currentState)
        {
            case BossState.Stop:

                //if (_enemyToPlayerDistance > _shotDistance)
                //{
                //    _currentState = BossState.Shot;
                //}
                //else if (_enemyToPlayerDistance > _needApproachDistance)
                //{
                //    _currentState = BossState.Approach;
                //}

                if (_bossHP.BossHPVariable < _bossHPInts[_bossHPIntsNum])
                {
                    _currentState = BossState.SpecialMove;
                }
                else if (_enemyToPlayerDistance > _needApproachDistance)
                {
                    _currentState = BossState.Approach;
                }

                break;//初期状態

            case BossState.Approach:

                Moving();

                if (_enemyToPlayerDistance <= _jumpAtackDistance)
                {
                    _currentState = BossState.JumpAtack;
                }
                else
                {
                    return;
                }

                break;//接近

            case BossState.Shot:
                ShotToPlayer();
                _currentState = BossState.Cooldown;
                break;//遠距離攻撃

            case BossState.JumpAtack:

                _isFollow = true;

                if (_isFollow)
                {
                    Moving();
                }
                

                if (!_hasTriggeredFall)
                {
                    JumpAtackToPlayer();
                }
                break;//近距離攻撃

            case BossState.SpecialMove:
                Moving();
                SpecialMoveToPlayer();
                break;//必殺技

            case BossState.Cooldown:
                _actionCoolTimer += Time.fixedDeltaTime;//アクション後の後隙時間計測
                if (_actionCoolTimer > _actionCoolTime)
                {
                    _actionCoolTimer = 0;//アクション後の後隙時間リセット
                    _currentState = BossState.Stop;
                }

                _hasTriggeredFall = false;
                break;

            case BossState.Stun:
                StunBoss();
                break;
        }
    }

    public void JumpAtackToPlayer()
    {

        if (!_hasTriggeredFall)
        {
            _bossAnimator.SetTrigger("IsFall");
            _hasTriggeredFall = true;
        }

        if (EnemyState == EnemyState.knockback)
        {
            return;
        }

        //for (int i = 0; i < _shockLangeList.Length; i++)
        //{
        //    _direction = (_playerObject.transform.position - transform.position).normalized;
        //    Vector2 rotatedDir = Quaternion.AngleAxis(_shockLangeList[i], Vector3.forward) * _direction;
        //    EnemyShotBullet shotBullet = Instantiate(_shockWave, transform.position, Quaternion.identity).GetComponent<EnemyShotBullet>();
        //    shotBullet.DirectionSetting(rotatedDir);
        //}

        _circleCollider2D.isTrigger = true;
    }

    public void JumpLand()//着地時の当たり判定発生
    {
        _jumpAtackArea.SetActive(true);
        _isFollow = false;
    }

    public void OnAttackEnd()//Animationの終了後にState遷移
    {
        _hasTriggerSpecialMove = false;
        if (_currentState == BossState.JumpAtack)
        {
            _jumpAtackArea.SetActive(false);
            _jumpAtackCounter++;

            for (int i = 0; i < _shockLangeList.Length; i++)
            {
                _direction = (_playerObject.transform.position - transform.position).normalized;
                Vector2 rotatedDir = Quaternion.AngleAxis(_shockLangeList[i], Vector3.forward) * _direction;
                EnemyShotBullet shotBullet = Instantiate(_shockWave, transform.position, Quaternion.identity).GetComponent<EnemyShotBullet>();
                shotBullet.DirectionSetting(this.gameObject, rotatedDir);
            }
        }
        else if (_currentState == BossState.SpecialMove)
        {
            _currentState = BossState.Cooldown;
        }
        _circleCollider2D.isTrigger = false;
        if (_jumpAtackCounter >= _jumpAtackCount)
        {
            _jumpAtackCounter = 0;
            _currentState = BossState.Cooldown;
        }
        
    }


    public virtual void ShotToPlayer()//通常の遠距離攻撃
    {
        if (EnemyState == EnemyState.knockback)
        {
            return;
        }

        for (int i = 0; i < _bulletObject.Length; i++)
        {
            _direction = (_playerObject.transform.position - transform.position).normalized;
            Vector2 rotatedDir = Quaternion.AngleAxis(_langeList[i], Vector3.forward) * _direction;
            EnemyShotBullet shotBullet = Instantiate(_bulletObject[i], transform.position, Quaternion.identity).GetComponent<EnemyShotBullet>();
            shotBullet.DirectionSetting(this.gameObject, rotatedDir);
        }

        _shotCoolTimer = 0;
    }

    public void SpecialMoveToPlayer()//必殺技発動
    {
        if (!_hasTriggerSpecialMove)
        {
            _bossAnimator.SetTrigger("IsSpecialMove");
            _hasTriggerSpecialMove = true;
            if (_bossHPIntsNum < _bossHPInts.Length)
            {
                _bossHPIntsNum++;
            }
        }

        #region
        //if (!_isDoingSpecialMove)
        //{
        //    _isDoingSpecialMove = true;
        //    _bossAnimator.SetTrigger("IsSpecialMove");
        //    if (_specialMoveCoroutine != null)
        //    {
        //        StopCoroutine(_specialMoveCoroutine);
        //    }
        //    _specialMoveCoroutine = StartCoroutine(ResetSpecialMovement());

        //}
        #endregion
    }
    private IEnumerator ResetSpecialMovement()
    {
        yield return new WaitForSeconds(7);
        _isSpecialMovement = false;
        _hasTriggerSpecialMove = false;
        _specialMoveCoroutine = null;
    }

    public void SpecialChargeReady()//必殺技発射
    {
        _direction = (_playerObject.transform.position - transform.position).normalized;
        Vector2 shotPosition = new Vector2(transform.position.x - 8, transform.position.y);
        EnemyShotBullet shotBullet = Instantiate(_specialMoveBullet, shotPosition, Quaternion.identity).GetComponent<EnemyShotBullet>();
        shotBullet.transform.localScale = new Vector3(8, 8, 1);
        SpriteRenderer renderer = shotBullet.GetComponent<SpriteRenderer>();
        renderer.color = Color.red; // 赤にする
        shotBullet.DirectionSetting(this.gameObject, _direction);

    }

    public void StunBoss()//スタン状態
    {
        _bossAnimator.SetTrigger("IsStun");
    }

    public void DebugCode()
    {
        print(_bossHPInts[_bossHPIntsNum]);
        print(_currentState);
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.gameObject.CompareTag("MirrorBullet"))
            return;

        if (_currentState == BossState.SpecialMove)
        {
            //print("当たった");

            if (_specialMoveCoroutine != null)
            {
                StopCoroutine(_specialMoveCoroutine);
                _specialMoveCoroutine = null;
            }

            _isSpecialMovement = false;

            _currentState = BossState.Stun;
            _playTheBombEffect.BombEffect(this.transform.position);
        }
    }
}
