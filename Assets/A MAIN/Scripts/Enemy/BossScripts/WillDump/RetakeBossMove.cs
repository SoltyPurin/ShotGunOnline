using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RetakeBossMove : EnemyMove
{
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
        Drop //落下中
    }
    protected BossForm _initBossForm = BossForm.First;
    protected BossState _currentState = BossState.Stop;
    public BossState CurrentState
    {
        get { return _currentState; }
    }

    protected float _shotCoolTimer = 2.0f;
    protected bool _canShot = false;
    protected bool _isSecondForm = false;
    [SerializeField, Header("ボスの攻撃用アニメーター")]
    protected Animator _bossAnimator;

    [Header("ボスが接近を行う距離")]
    [SerializeField] protected float _needApproachDistance = 7;

    [Header("近距離攻撃を行う距離")]
    [SerializeField] protected float _jumpAttackDistance = 5;

    [Header("銃弾のオブジェクトを入れる配列")]
    [SerializeField] protected GameObject[] _bulletObject = new GameObject[3];

    [Header("近距離攻撃の当たり判定")]
    [SerializeField] protected GameObject _fallAttackArea;

    [Header("遠距離攻撃のクールタイム")]
    [SerializeField] protected float _shotCoolTime = 2.0f;

    [SerializeField, Header("第二形態のスクリプト")]
    private SecondBossMove _secondBossMOve = default;

    [SerializeField, Header("接触判定のコライダー")]
    protected CircleCollider2D _circleCollider2D = default;

    protected float[] _langeList = { 15, 0, -15 };

    protected float _enemyToPlayerDistance = 0;

    protected CapsuleCollider2D _capsuleCollider2D = default;
    protected virtual void Start()
    {
        _playerObject = GameObject.FindWithTag(PLAYERTAGNAME);//プレイヤーのオブジェクトを探索して代入

        //最初にプレイヤーの方向を計算してみる
        _direction = (_playerObject.transform.position - transform.position).normalized;

    }

    public void ChangeBossForm()
    {
        _initBossForm = BossForm.Second;
        _isSecondForm = true;
    }

    private void FixedUpdate()
    {
        Debug.Log("現在のステートは" + _currentState);
        //第一形態でのみ処理を行う
        if (_initBossForm == BossForm.First)
        {
            StateAction();
        }
    }

    protected void StateAction()
    {
        _enemyToPlayerDistance = Vector2.Distance(_playerObject.transform.position, this.transform.position);
        switch (_currentState)
        {
            case BossState.Stop:

                InStop();

                if (_enemyToPlayerDistance > _needApproachDistance)
                    _currentState = BossState.Approach;
                else if (_enemyToPlayerDistance <= _jumpAttackDistance)
                    _currentState = BossState.JumpAtack;
                else if (_canShot)
                    _currentState = BossState.Shot;

                _fallAttackArea.SetActive(false);
                _bossAnimator.ResetTrigger("IsFall");

                break;//初期状態

            case BossState.Approach:

                Moving();

                if (_canShot)
                {
                    ShotToPlayer();
                }

                _currentState = BossState.Stop;
                break;//接近

            case BossState.Shot:

                if (_canShot)
                {
                    ShotToPlayer();
                }

                if (_enemyToPlayerDistance > _needApproachDistance)
                    _currentState = BossState.Approach;
                else
                    _currentState = BossState.Stop;
                break;//遠距離攻撃

            case BossState.JumpAtack:

                if (!_fallAttackArea.activeSelf)//当たり判定のポジション設定
                {
                    _fallAttackArea.transform.position = new Vector2(transform.position.x,
                        transform.position.y - 3);
                }
                _bossAnimator.SetTrigger("IsFall");
                break;//近距離攻撃

            case BossState.SpecialMove:

                break;

        }

    }
    public void OnAttackEnd()//Animationの終了後にState遷移
    {
        _currentState = BossState.Stop;
    }

    public virtual void InStop()
    {
        _shotCoolTimer += Time.fixedDeltaTime;

        if (_shotCoolTimer >= _shotCoolTime)
            _canShot = true;
        else
            _canShot = false;
    }


    public virtual void ShotToPlayer()
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
            shotBullet.DirectionSetting(this.gameObject,rotatedDir);
        }
        _shotCoolTimer = 0;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        Vector3 start = transform.position;

        // プレイヤーへの基本方向
        Vector3 baseDirection = (_playerObject.transform.position - transform.position).normalized;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(start, start + baseDirection * 3);

        // 各弾の方向（例えば3発の扇形発射）
        for (int i = 0; i < _langeList.Length; i++)
        {
            Vector2 rotatedDir = Quaternion.AngleAxis(_langeList[i], Vector3.forward) * baseDirection;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(start, start + (Vector3)rotatedDir * 3);
        }
    }

    public void JumpAtackToPlayer()
    {
        _capsuleCollider2D.isTrigger = false;
    }

    public void ShowAttackArea()
    {
        _fallAttackArea.SetActive(true);
        _capsuleCollider2D = _fallAttackArea.GetComponent<CapsuleCollider2D>();
        _capsuleCollider2D.isTrigger = true;
    }

}
