using UnityEngine;

/// <summary>
/// ルーム未接続時にロビーを歩き回るためのオフライン分身。
/// NetworkObject を持たず、NGO が起動していなくても動作します。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class LobbyLocalAvatar : MonoBehaviour
{
    [Header("移動速度")]
    [SerializeField] private float _moveSpeed = 10f;

    [Header("ビジュアル参照（未設定時は子階層から自動取得）")]
    [SerializeField] private PlayerAnimation _playerAnimation;
    [SerializeField] private SpriteRenderer _frontSprite;
    [SerializeField] private SpriteRenderer _backSprite;

    private Rigidbody2D _rigidbody;
    private Vector2 _input;
    private byte _facingMask;

    public byte FacingMask => _facingMask;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _rigidbody.gravityScale = 0f;
        _rigidbody.freezeRotation = true;

        LobbyMovement.AutoResolveVisuals(transform, ref _playerAnimation, ref _frontSprite, ref _backSprite);
        LobbyMovement.ApplyFacing(_frontSprite, _backSprite, _facingMask);
    }

    public void SetFacingMask(byte facingMask)
    {
        _facingMask = facingMask;
        LobbyMovement.ApplyFacing(_frontSprite, _backSprite, _facingMask);
    }

    private void Update()
    {
        _input = LobbyMovement.ReadMoveInput();
        _facingMask = LobbyMovement.UpdateFacingMask(_input, _facingMask);
        LobbyMovement.ApplyFacing(_frontSprite, _backSprite, _facingMask);
        LobbyMovement.ApplyAnimation(_playerAnimation, LobbyMovement.ToAnimSpeed(_input));
    }

    private void FixedUpdate()
    {
        _rigidbody.linearVelocity = _input * _moveSpeed;
    }
}
