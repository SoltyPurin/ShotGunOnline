using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    //生存してるかどうか
    private bool _isAlive = true;

    [Header("プレイヤーの移動速度")]
    [SerializeField] private float _playerMoveSpeed = 10f;

    [SerializeField, Header("プレイヤーの移動速度の最大値")]
    private float _playerMoveSpeedMaxLimit = 30;

    private float _originPlayerMoveSpeed = default;

    private Gamepad _gamePad;//コントローラー取得

    [Header("rigidBody取得")]
    [SerializeField] private Rigidbody2D _rigidbody = default;

    [SerializeField] private PlayerKnockBack _playerKnockBack = default;

    [SerializeField] private PlayerStateManager _playerStateManger = default;

    [SerializeField, Header("プレイヤ―アニメ管理取得")]
    private PlayerAnimation _playerAnimation = default;

    [SerializeField, Header("チャージ量を取得する")]
    private InputPlayerShot _inputPlayerShot = default;

    [SerializeField, Header("最低移動速度")]
    private float _minMoveSpeed = 0.1f;
    private float _chargeMoveMultiplier = 1;

    [SerializeField, Header("どれくらい滑らせるか")]
    private float _inertiaStrangth = 10f;

    private bool _isFloating = false;
    public bool IsFloating
    {
        set { _isFloating = value; }
    }

    private Vector2 _saveDirection = Vector2.zero;
    public Vector2 SaveDirection
    {
        set { _saveDirection = value; } 
    }

    //フェード中かどうかを判断する
    private bool _isFadeing = false;
    public bool IsFadeing
    {
        set { _isFadeing = value; }
    }

    private bool _canPlayWait = true;

    private void Start()
    {
        if(Gamepad.current == null)//コントローラーがない場合はリターン
        {
            return;
        }
        _gamePad = Gamepad.current;//現在のコントローラーを代入
        _originPlayerMoveSpeed = _playerMoveSpeed;
    }

    public void DeathMethod()
    {
        _isAlive = false;
    }

    private void FixedUpdate()
    {
        //死亡判定に入ったら操作を一切無効化
        if (!_isAlive)
        {
            return;
        }
        bool isStateFall = _playerStateManger.PlayerState == PlayerState.Fall;
        bool isStateKnockBack = _playerStateManger.PlayerState == PlayerState.KnockBack;
        bool isStateDamageKnockBack = _playerStateManger.PlayerState == PlayerState.DamageKnockBack;
        bool isStateMovie = _playerStateManger.PlayerState == PlayerState.Movie;
        bool cantMoveState = isStateFall || isStateKnockBack || isStateDamageKnockBack || isStateMovie;
        if (cantMoveState)
        {
            return ; //落下中は操作不可能にするため早期リターン
        }
        if (_inputPlayerShot.IsPushShotButton)
        {
            CalcMoveMultiplier(_inputPlayerShot.ChargeValue);
        }
        else
        {
            _chargeMoveMultiplier = 1;
        }
        //フェード中は移動させない
        if(_isFadeing)
        {
            return;
        }
        Move();//移動のメソッド呼び出し
    }


    private void Move()
    {
        if(_gamePad == null)
        {
            return;
        }
        //左スティック取得
        Vector2 input = _gamePad.leftStick.ReadValue();
        //Vector3型に変換
        Vector2 moveDirection = new Vector3(input.x, input.y);
        float limit = Mathf.Clamp(_playerMoveSpeed * _chargeMoveMultiplier,
                                  _playerMoveSpeed, _playerMoveSpeedMaxLimit);


        ////ベロシティを移動方向*スピードで直接変更
        Vector2 targetVelocity = moveDirection * _playerMoveSpeed * _chargeMoveMultiplier;
        if(moveDirection != Vector2.zero)
        {
            _saveDirection = _rigidbody.linearVelocity;
        }

        if (_isFloating)
        {
            float t = Time.fixedDeltaTime * _inertiaStrangth;
            _rigidbody.linearVelocity = Vector2.Lerp(_rigidbody.linearVelocity, targetVelocity,t);
            if(moveDirection == Vector2.zero)
            {
                _rigidbody.linearVelocity = Vector2.Lerp(_rigidbody.linearVelocity,_saveDirection,t);
            }
        }
        else
        {
            _rigidbody.linearVelocity = targetVelocity;
        }

        if (input.x != 0 || input.y != 0)
        {
            _playerKnockBack.IsInputLeftStick = true;
            _playerAnimation.Run(Mathf.Max(Mathf.Abs(input.x), Mathf.Abs(input.y)));
            _canPlayWait = true;
        }
        else
        {
            if(_canPlayWait)
            {
                _playerAnimation.Wait();
                _canPlayWait = false;
            }
        }
    }

    /// <summary>
    /// チャージ量に応じて速度を下げるスクリプト
    /// </summary>
    /// <param name="chargeValue">チャージを入力してる時間</param>
    private void CalcMoveMultiplier(float chargeValue)
    {
        float t = Mathf.Clamp01(chargeValue / 2); // maxChargeが2.0など
        _chargeMoveMultiplier = _chargeMoveMultiplier = Mathf.Lerp(1.0f, _minMoveSpeed, t);
    }

    /// <summary>
    /// コンボのご褒美として速度を上げるメソッド
    /// </summary>
    /// <param name="bonusValue">速度に乗算する値</param>
    public void BonusSet(float bonusValue)
    {
        _playerMoveSpeed = _originPlayerMoveSpeed;
        _playerMoveSpeed *= bonusValue;
    }

    /// <summary>
    /// ご褒美終了、速度を戻すメソッド
    /// </summary>
    public void ResetMoveSpeed()
    {
        _playerMoveSpeed = _originPlayerMoveSpeed;
    }
}
