using UnityEditor;
using UnityEngine;
/// <summary>
/// ボスの行動管理用のスクリプト
/// お触り厳禁
/// </summary>
public class BossStateManagement : EnemyMove
{
    #region[変数名]
    //---GameObject,Script,Animator等---------------------------------
    /*攻撃系の処理*/
    private BossJumpAtack _jumpAtack = default;
    private BossMachPunch _machPunch = default;

    /*その他の処理*/
    private BossHP _bossHP = default;
    private BossAnimeManager _bossAtackAnimeManager = default;
    private BossJumpAtackShake _mainCameraShake = default;
    private SecondFormStart _changeSecondForm = default;

    /*その他の参照*/
    [SerializeField, Header("ボスの攻撃用アニメーション")]
    private Animator _bossAnimator = default;

    public GameObject Player
    {
        get { return _playerObject; }
    }

    private GameObject _mainCamera = default;


    //---int,floatなどの数値---------------------------------
    [SerializeField, Header("ボスが接近を行う距離")]
    protected float _needApproachDistance = 7;
    [SerializeField, Header("各アクション後の隙")]
    protected float _actionCoolTime = 1.0f;
    [SerializeField, Header("ボスが攻撃を行う距離")]
    protected float _attackDistance = 7;
    [SerializeField, Header("ボスがJumpAtackを行う距離")]
    protected float _jumpAttackDistance = 7;


    internal float _enemyToPlayerDistance = 0;

    protected float _actionCoolTimer = 0;


    //どの攻撃をするか決めるランダム値
    private int _randamRange = 0;
    [SerializeField, Header("ジャンプ攻撃の出現率（重み）")]
    private int _jumpWeight = 3;
    [SerializeField, Header("パンチ攻撃の出現率（重み）")]
    private int _punchWeight = 7;


    private int _jumpAttackCnt = 0;
    private int _punchCnt = 0;
    [SerializeField]
    private int _canAttackTimes = 0;


    //---stringなどの文字列---------------------------------


    //---bool------------------------------------------------
    private bool _hasTriggeredSpecialMove = false;
    private bool _hasTriggeredFall = false;
    internal bool _aimFall = true;
    internal bool _isPunch = true;
    internal bool _changed = false;
    internal bool _hasDecidedNextAction = false;

    //---enum------------------------------------------------
    public enum BossState
    {
        Stop,//次の行動を思考
        Approach,//プレイヤーに接近
        Punch,//遠距離攻撃
        JumpAtack,//近距離攻撃
        Cooldown,//攻撃後の後隙
        Drop //落下中
    }
    internal BossState _currentState = BossState.Stop;
    public BossState CurrentState
    {
        get { return _currentState; }
    }

    #endregion

    public override void Start()
    {
        base.Start();

        _bossHP = gameObject.GetComponent<BossHP>();

        //agentにNavMeshAgent2Dを取得
        _agent = GetComponent<NavMesh2DAgent>();

        //カメラ取得
        _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        _mainCameraShake = _mainCamera.GetComponent<BossJumpAtackShake>();

        //Bossの攻撃用Animationを取得
        _bossAtackAnimeManager = GetComponent<BossAnimeManager>();

        //JumpAtack処理のスクリプトを取得
        _jumpAtack = GetComponent<BossJumpAtack>();

        //MachPunch処理のスクリプトを取得
        _machPunch = GetComponent<BossMachPunch>();

        //第二形態移行時処理のスクリプトを取得
        _changeSecondForm = GetComponent<SecondFormStart>();
    }
    public void FixedUpdate()
    {

        _enemyToPlayerDistance = Vector2.Distance(_playerObject.transform.position,
           this.transform.position);

        BossStateChange();

        if (_bossHP._secondForm && !_changed)
        {
            _changed = true;
            _changeSecondForm.ToBESecondForm();
            _currentState = BossState.Stop;
            _hasDecidedNextAction = false;
        }

        if (_playerObject == null)
        {
            _playerObject = GameObject.FindWithTag(PLAYERTAGNAME);
        }
    }

    protected void BossStateChange()
    {

        switch (_currentState)
        {
            //初期状態
            case BossState.Stop:
                if (!_hasDecidedNextAction)
                {

                    if (_enemyToPlayerDistance > _needApproachDistance)
                    {
                        _hasDecidedNextAction = true;
                        _bossAtackAnimeManager.PlayWalk();
                        _currentState = BossState.Approach;
                    }
                    else if (_enemyToPlayerDistance <= _needApproachDistance)
                    {
                        RandamAtackLange();

                        if (_randamRange < _jumpWeight)
                        {
                            _hasDecidedNextAction = true;
                            _bossAtackAnimeManager.PlayJumpAtack();
                            _currentState = BossState.JumpAtack;
                        }
                        else
                        {
                            _hasDecidedNextAction = true;
                            _bossAtackAnimeManager.PlayMachPunch();
                            _currentState = BossState.Punch;
                        }
                    }
                }

                break;

            //接近
            case BossState.Approach:
                Moving();
                if (_enemyToPlayerDistance <= _needApproachDistance)
                {

                    if (_randamRange <= _jumpWeight)
                    {
                        _bossAtackAnimeManager.StopWalk();
                        _bossAtackAnimeManager.PlayJumpAtack();
                        _currentState = BossState.JumpAtack;
                    }
                    else
                    {
                        _bossAtackAnimeManager.StopWalk();
                        _bossAtackAnimeManager.PlayMachPunch();
                        _currentState = BossState.Punch;
                    }
                }

                break;

            //遠距離攻撃
            case BossState.Punch:

                _jumpWeight = 7;
                _punchWeight = 3;

                if (_isPunch)
                {
                    _machPunch.PunchToPlayer();
                }
                break;

            //近距離攻撃
            case BossState.JumpAtack:

                _jumpWeight = 3;
                _punchWeight = 7;

                if (!_aimFall)
                    _jumpAtack.MoveToJumpAtackPosition();
                if (!_hasTriggeredFall)
                {
                    _jumpAtack.JumpAtackToPlayer();
                    _hasTriggeredFall = true;
                }

                break;

            case BossState.Cooldown:
                if (_mainCameraShake._beShake != false)
                {
                    _mainCameraShake._beShake = false;
                }


                if (_hasTriggeredFall)
                {
                    _hasTriggeredFall = false;
                }

                if (_hasTriggeredSpecialMove)
                {
                    _hasTriggeredSpecialMove = false;
                }
                //アクション後の後隙時間計測
                _actionCoolTimer += Time.fixedDeltaTime;

                if (_actionCoolTimer > _actionCoolTime)
                {
                    //アクション後の後隙時間リセット
                    _actionCoolTimer = 0;
                    _hasDecidedNextAction = false;
                    transform.rotation = new Quaternion(transform.rotation.x,
                        transform.rotation.y,
                        0,
                        1);
                    _currentState = BossState.Stop;
                }

                break;
        }
    }

    private void RandamAtackLange()
    {
        int total = _jumpWeight + _punchWeight;
        _randamRange = Random.Range(0, total);
    }


}
