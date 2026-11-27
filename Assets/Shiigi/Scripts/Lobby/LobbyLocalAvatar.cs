using System;
using UnityEngine;

/// <summary>
/// ルーム未接続時にロビーを歩き回るためのオフライン分身。
/// NetworkObject を持たず、NGO が起動していなくても動作します。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class LobbyLocalAvatar : MonoBehaviour, IPlayerNameSource
{
    [Header("移動速度")]
    [SerializeField] private float _moveSpeed = 6.5f;

    [Header("ビジュアル参照（未設定時は子階層から自動取得）")]
    [SerializeField] private PlayerAnimation _playerAnimation;
    [SerializeField] private SpriteRenderer _frontSprite;
    [SerializeField] private SpriteRenderer _backSprite;

    private Rigidbody2D _rigidbody;
    private Vector2 _input;
    private byte _facingMask;
    private byte _lastAppliedFacingMask = byte.MaxValue;
    private float _lastAppliedAnimSpeed = -1f;

    public byte FacingMask => _facingMask;

    // IPlayerNameSource 実装
    public string PlayerName => PlayerProfileStore.PlayerName;
    public bool IsLocalControl => true;
    public bool IsNameplateVisible => true;
    public event Action<string> OnPlayerNameChanged;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _rigidbody.gravityScale = 0f;
        _rigidbody.freezeRotation = true;

        LobbyMovement.AutoResolveVisuals(transform, ref _playerAnimation, ref _frontSprite, ref _backSprite);
        ApplyFacingIfChanged(_facingMask);
        PlayerNameplateUI.EnsureAttached(gameObject);
    }

    private void OnEnable()
    {
        PlayerProfileStore.OnProfileChanged += HandleProfileChanged;
    }

    private void OnDisable()
    {
        PlayerProfileStore.OnProfileChanged -= HandleProfileChanged;
    }

    private void HandleProfileChanged(PlayerProfile profile)
    {
        if (profile != null)
        {
            OnPlayerNameChanged?.Invoke(profile.PlayerName);
        }
    }

    public void SetFacingMask(byte facingMask)
    {
        _facingMask = facingMask;
        ApplyFacingIfChanged(_facingMask);
    }

    private void Update()
    {
        _input = LobbyMovement.ReadMoveInput();
        _facingMask = LobbyMovement.UpdateFacingMask(_input, _facingMask);
        ApplyFacingIfChanged(_facingMask);

        float animSpeed = LobbyMovement.ToAnimSpeed(_input);
        if (Mathf.Abs(animSpeed - _lastAppliedAnimSpeed) > 0.005f || (animSpeed == 0f && _lastAppliedAnimSpeed != 0f))
        {
            _lastAppliedAnimSpeed = animSpeed;
            LobbyMovement.ApplyAnimation(_playerAnimation, animSpeed);
        }
    }

    private void FixedUpdate()
    {
        _rigidbody.linearVelocity = _input * _moveSpeed;
    }

    private void ApplyFacingIfChanged(byte mask)
    {
        if (_lastAppliedFacingMask == mask) return;
        _lastAppliedFacingMask = mask;
        LobbyMovement.ApplyFacing(_frontSprite, _backSprite, mask);
    }
}
