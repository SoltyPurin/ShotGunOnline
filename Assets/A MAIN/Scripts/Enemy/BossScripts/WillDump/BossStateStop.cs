using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossStateStop : MonoBehaviour
{
    #region
    public enum BossState
    {
        Stop,//次の行動を思考
        Approach,//プレイヤーに接近
        Punch,//遠距離攻撃
        JumpAtack,//近距離攻撃
        SpecialMove,//必殺技
        Cooldown,//攻撃後の後隙
        Stun,//スタン状態
        Drop //落下中
    }
    internal BossState _currentState = BossState.Cooldown;
    public BossState CurrentState
    {
        get { return _currentState; }
    }

    [SerializeField, Header("ボスが接近を行う距離")]
    protected float _needApproachDistance = 7;
    [SerializeField, Header("各アクション後の隙")]
    protected float _actionCoolTime = 1.0f;
    [SerializeField, Header("ボスがJumpAtackを行う距離")]
    protected float _jumpAtackDistance = 7;
    [SerializeField] private Animator _bossAnimator;

    AnimatorStateInfo _stateInfo = default;

    protected int[] _bossHPInts = { 10000, 6000, 2000, -10000 };
    internal float _enemyToPlayerDistance = 0;
    internal int _bossHPIntsNum = 0;
    internal bool _aimFall = true;
    internal bool _isPunch = true;
    private bool _hasTriggeredSpecialMove = false;
    private bool _changed = false;
    private bool _hasDecidedNextAction = false;
    private float _randamRange = 0;
    private BossHP _bossHP = default;
    private BossAnimeManager _bossAtackAnimeManager = default;
    private GameObject _playerObject = default;

    #endregion

    private void Start()
    {
        _bossHP = gameObject.GetComponent<BossHP>();
        _playerObject = GameObject.FindGameObjectWithTag("Player");
        _bossAtackAnimeManager = GetComponent<BossAnimeManager>();
    }
    private void FixedUpdate()
    {
        _enemyToPlayerDistance = Vector2.Distance(_playerObject.transform.position,
          this.transform.position);
    }
    public void DecideNextAction()
    {

        if (!_hasDecidedNextAction)
        {
            RandamAtackLange();

            if (_enemyToPlayerDistance > _needApproachDistance)
            {
                _bossAtackAnimeManager.PlayWalk();
            }
            else if (_enemyToPlayerDistance <= _jumpAtackDistance)
            {

                if (_randamRange < 0.5f)
                {
                    _bossAtackAnimeManager.PlayJumpAtack();
                }
                else
                {
                    _bossAtackAnimeManager.PlayMachPunch();
                }
            }

            _hasDecidedNextAction = true;
        }


    }

    private void RandamAtackLange()
    {
        _randamRange = Random.value;
    }
}
