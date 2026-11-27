using Unity.Netcode;
using UnityEngine;

public class PlayerKnockBack : NetworkBehaviour
{
    private const byte STATE_NONE = 0;
    private const byte STATE_LIGHT = 1;
    private const byte STATE_STRONG = 2;

    [Header("プレイヤーのオブジェクト")]
    [SerializeField] private GameObject _playerObject = default;

    [Header("スクリプトを設定")]
    [SerializeField] private PlayerMove _playerMove = default;

    [SerializeField, Header("ステートを判定するスクリプト")]
    private PlayerStateManager _playerState = default;

    [SerializeField, Header("プレイヤーのアニメーション再生スクリプト")]
    private PlayerAnimation _animation = default;

    private Rigidbody2D _rigidBody = default;

    // 毎フレームのfloat同期を廃止し、状態遷移時（なし / 弱反動 / 強反動アニメ）のみ1バイト同期する
    private readonly NetworkVariable<byte> _netKnockBackState = new NetworkVariable<byte>(
        STATE_NONE,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    private byte _localKnockBackState = STATE_NONE;

    private byte CurrentKnockBackState => IsLocalControl ? _localKnockBackState : _netKnockBackState.Value;

    public bool IsKnockBacking => CurrentKnockBackState != STATE_NONE;

    private Vector3 _minusDiretion = default;
    private float _powValue = default;
    private float _force = 0f;
    private bool _isInputLeftStick = false;

    private readonly float KNOCKBACK_FORCE = 5.0f;

    public bool IsInputLeftStick
    {
        set { _isInputLeftStick = value; }
    }

    private bool IsLocalControl => IsOwner || !IsSpawned;

    private void Start()
    {
        _rigidBody = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner && _netKnockBackState.Value != _localKnockBackState)
        {
            _netKnockBackState.Value = _localKnockBackState;
        }
    }

    private void SetKnockBackState(byte nextState)
    {
        _localKnockBackState = nextState;
        if (IsSpawned && IsOwner && _netKnockBackState.Value != nextState)
        {
            _netKnockBackState.Value = nextState;
        }
    }

    private byte EvaluateStateFromForce(float currentForce)
    {
        if (currentForce <= 0f) return STATE_NONE;
        return currentForce >= KNOCKBACK_FORCE ? STATE_STRONG : STATE_LIGHT;
    }

    public void SetDirection(Vector3 direction, float chargeTime)
    {
        if (!IsLocalControl)
        {
            return;
        }

        // 現在見ている方向と逆の方向を出す
        _minusDiretion = (direction * -1f).normalized;
        _powValue = Mathf.Pow(chargeTime, 5f);
        _force = Mathf.Lerp(3f, _powValue, 0.5f);

        SetKnockBackState(EvaluateStateFromForce(_force));
    }

    public void Fall()
    {
        if (!IsLocalControl)
        {
            return;
        }

        _force = 0f;
        SetKnockBackState(STATE_NONE);
    }

    public void TakeDamage()
    {
        if (!IsLocalControl)
        {
            return;
        }

        _force = 0f;
        SetKnockBackState(STATE_NONE);
    }

    private void FixedUpdate()
    {
        if (!IsLocalControl)
        {
            return;
        }

        if (_localKnockBackState == STATE_NONE)
        {
            return;
        }

        if (_playerState != null && _playerState.PlayerState == PlayerState.DamageKnockBack)
        {
            _force = 0f;
            SetKnockBackState(STATE_NONE);
            return;
        }

        KnockBacking();
    }

    private void Update()
    {
        // 被弾ノックバック中は被弾アニメを優先
        if (_playerState != null && _playerState.PlayerState == PlayerState.DamageKnockBack) return;

        // 強い反動ノックバック中のみアニメーションを適用する
        if (CurrentKnockBackState == STATE_STRONG && _animation != null)
        {
            _animation.KnockBack();
        }
    }

    private void KnockBacking()
    {
        transform.position += _minusDiretion * _force * Time.fixedDeltaTime;
        if (_playerMove != null)
        {
            _playerMove.SaveDirection = _minusDiretion * _force;
        }

        if (_force >= KNOCKBACK_FORCE && _animation != null)
        {
            _animation.KnockBack();
        }

        // 秒単位の減衰率 (0.02s換算で 元の 0.5f -> 25f/s, 0.2f -> 10f/s)
        float decayRate = _isInputLeftStick ? 25f : 10f;
        _force -= decayRate * Time.fixedDeltaTime;

        if (_force <= 0f)
        {
            _force = 0f;
            if (_animation != null)
            {
                _animation.Wait();
            }
            SetKnockBackState(STATE_NONE);
        }
        else
        {
            SetKnockBackState(EvaluateStateFromForce(_force));
        }
    }
}
