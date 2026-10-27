using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : Unity.Netcode.NetworkBehaviour
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

    private Vector2 _moveDirection;
    private Vector2 _input;

    //フェード中かどうかを判断する
    private bool _isFadeing = false;
    public bool IsFadeing
    {
        set { _isFadeing = value; }
    }

    private bool _canPlayWait = true;

    //操作してる側から書き込める変数
    private NetworkVariable<float> _netAnimSpeed = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );
    private void Start()
    {
        if (Gamepad.current != null)
        {
            _gamePad = Gamepad.current;//現在のコントローラーを代入
        }
        _originPlayerMoveSpeed = _playerMoveSpeed;
    }

    public override void OnNetworkSpawn()
    {
        if (_rigidbody == null)
        {
            _rigidbody = GetComponent<Rigidbody2D>();
        }

        // クライアント側では、他プレイヤー（ホストなど）の Rigidbody2D を Kinematic にする
        if (!IsServer && !IsOwner && _rigidbody != null)
        {
            _rigidbody.bodyType = RigidbodyType2D.Kinematic;
            _rigidbody.linearVelocity = Vector2.zero;
        }

        if (IsOwner)
        {
            // 開始ムービー・待機中等の場合はMovieState（操作ロック）を維持し、それ以外ならNormalStateにする
            bool isOpeningCutscene = false;
            if (MP_GameManager.Instance != null)
            {
                var s = MP_GameManager.Instance.CurrentState;
                if (s == MP_GameManager.GameState.WaitingForPlayers || s == MP_GameManager.GameState.OpeningMovie)
                {
                    isOpeningCutscene = true;
                }
            }

            if (_playerStateManger != null)
            {
                if (isOpeningCutscene)
                {
                    _playerStateManger.MovieState();
                }
                else
                {
                    _playerStateManger.NormalState();
                }
            }

            // メインカメラを探して自機を追従ターゲットに設定する
            var cam = Camera.main != null ? Camera.main.GetComponent<FollowingCameraToBurrel>() : null;
            if (cam != null)
            {
                cam.SetTargetPlayer(this.gameObject);
            }
        }
    }

    public void DeathMethod()
    {
        _isAlive = false;
    }

    private void Update()
    {
        // 被弾ノックバック中、またはショットガンの反動ノックバック中は歩き/待機アニメの適用を一時停止
        bool isDamageKb = _playerStateManger != null && _playerStateManger.PlayerState == PlayerState.DamageKnockBack;
        bool isShotKb = _playerKnockBack != null && _playerKnockBack.IsKnockBacking;

        if (isDamageKb || isShotKb)
        {
            return;
        }
        //結局アップデートで常に値を計算し続けてアニメーションを再生させる処理にした
        float animSpeed = _netAnimSpeed.Value;
        if (animSpeed > 0.01f)
        {
            if (_playerAnimation != null)
            {
                _playerAnimation.Run(animSpeed);
            }
            _canPlayWait = true;
        }
        else
        {
            if (_canPlayWait)
            {
                if (_playerAnimation != null)
                {
                    _playerAnimation.Wait();
                }
                _canPlayWait = false;
            }
        }
    }
    private void FixedUpdate()
    {
        //死亡判定に入ったら操作を一切無効化
        if (!_isAlive)
        {
            return;
        }
        bool isStateFall = _playerStateManger != null && _playerStateManger.PlayerState == PlayerState.Fall;
        bool isStateKnockBack = _playerStateManger != null && _playerStateManger.PlayerState == PlayerState.KnockBack;
        bool isStateDamageKnockBack = _playerStateManger != null && _playerStateManger.PlayerState == PlayerState.DamageKnockBack;
        bool isStateMovie = _playerStateManger != null && _playerStateManger.PlayerState == PlayerState.Movie;
        bool cantMoveState = isStateFall || isStateKnockBack || isStateDamageKnockBack || isStateMovie;
        if (cantMoveState)
        {
            return; //落下中・ムービー中は操作不可能にするため早期リターン
        }
        //フェード中は移動させない
        if (_isFadeing)
        {
            return;
        }

        // 自分のキャラクター（Owner）のみが入力と移動をローカルで行う
        if (this.IsOwner)
        {
            if (_inputPlayerShot != null && _inputPlayerShot.IsPushShotButton)
            {
                CalcMoveMultiplier(_inputPlayerShot.ChargeValue);
            }
            else
            {
                _chargeMoveMultiplier = 1;
            }

            Vector2 input = Vector2.zero;
            if (_gamePad != null)
            {
                input = _gamePad.leftStick.ReadValue();
            }

            // コントローラーの入力がない場合、キーボード入力を取得する
            if (input.sqrMagnitude < 0.01f)
            {
                float x = 0;
                float y = 0;
                if (Keyboard.current != null)
                {
                    if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y += 1f;
                    if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y -= 1f;
                    if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
                    if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
                }
                else
                {
                    x = Input.GetAxisRaw("Horizontal");
                    y = Input.GetAxisRaw("Vertical");
                }
                input = new Vector2(x, y);
                // 斜め移動で速くならないように正規化する（キーボード入力のみ）
                if (input.sqrMagnitude > 1f)
                {
                    input.Normalize();
                }
            }

            _input = input;
            _moveDirection = new Vector3(input.x, input.y);

            float currentInputSpeed = Mathf.Max(Mathf.Abs(_input.x), Mathf.Abs(_input.y));
            _netAnimSpeed.Value = currentInputSpeed;
            Move();
        }
        else
        {
            // 他プレイヤーのキャラクターは物理シミュレーションを切る（ClientNetworkTransformの受信に任せる）
            if (_rigidbody != null && _rigidbody.bodyType != RigidbodyType2D.Kinematic)
            {
                _rigidbody.bodyType = RigidbodyType2D.Kinematic;
                _rigidbody.linearVelocity = Vector2.zero;
            }
        }
    }


    private void Move()
    {

        ////ベロシティを移動方向*スピードで直接変更
        Vector2 targetVelocity = _moveDirection * _playerMoveSpeed * _chargeMoveMultiplier;
        if (_moveDirection != Vector2.zero)
        {
            _saveDirection = _rigidbody.linearVelocity;
        }

        if (_isFloating)
        {
            float t = Time.fixedDeltaTime * _inertiaStrangth;
            _rigidbody.linearVelocity = Vector2.Lerp(_rigidbody.linearVelocity, targetVelocity, t);
            if (_moveDirection == Vector2.zero)
            {
                _rigidbody.linearVelocity = Vector2.Lerp(_rigidbody.linearVelocity, _saveDirection, t);
            }
        }
        else
        {
            _rigidbody.linearVelocity = targetVelocity;
        }

        if (_input.x != 0 || _input.y != 0)
        {
            _playerKnockBack.IsInputLeftStick = true;
            _playerAnimation.Run(Mathf.Max(Mathf.Abs(_input.x), Mathf.Abs(_input.y)));
            _canPlayWait = true;
        }
        else
        {
            if (_canPlayWait)
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
