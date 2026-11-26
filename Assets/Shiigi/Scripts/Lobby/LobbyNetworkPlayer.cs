using System;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// ロビー（集会所）用のネットワークプレイヤー。
/// 位置同期は ClientNetworkTransform（オーナー権限）に任せ、ここでは入力・移動・向き・アニメ速度の同期を担当します。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class LobbyNetworkPlayer : NetworkBehaviour, IPlayerNameSource
{
    /// <summary>自端末のプレイヤーがスポーンした時に通知（カメラ追従・分身との入れ替え用）</summary>
    public static event Action<LobbyNetworkPlayer> LocalPlayerSpawned;

    /// <summary>自端末のプレイヤーがデスポーンする時に通知</summary>
    public static event Action<LobbyNetworkPlayer> LocalPlayerDespawned;

    [Header("移動速度")]
    [SerializeField] private float _moveSpeed = 6.5f;

    [Header("ビジュアル参照（未設定時は子階層から自動取得）")]
    [SerializeField] private PlayerAnimation _playerAnimation;
    [SerializeField] private SpriteRenderer _frontSprite;
    [SerializeField] private SpriteRenderer _backSprite;

    private readonly NetworkVariable<float> _netAnimSpeed = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    private readonly NetworkVariable<byte> _netFacingMask = new NetworkVariable<byte>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    private readonly NetworkVariable<FixedString64Bytes> _netPlayerName = new NetworkVariable<FixedString64Bytes>(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    private Rigidbody2D _rigidbody;
    private NetworkTransform _networkTransform;
    private Vector2 _input;
    private Vector2? _pendingTeleport;
    private string _cachedRemotePlayerName = string.Empty;
    private byte _lastAppliedFacingMask = byte.MaxValue;
    private float _lastAppliedAnimSpeed = -1f;

    public byte FacingMask => _netFacingMask.Value;

    // IPlayerNameSource 実装（キャッシュ済みの文字列を返すことで毎フレームの FixedString.ToString() アロケーションを防止）
    public string PlayerName => IsLocalControl ? PlayerProfileStore.PlayerName : _cachedRemotePlayerName;
    public bool IsLocalControl => IsOwner || !IsSpawned;
    public bool IsNameplateVisible => true;
    public event Action<string> OnPlayerNameChanged;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _networkTransform = GetComponent<NetworkTransform>();
        _rigidbody.gravityScale = 0f;
        _rigidbody.freezeRotation = true;

        LobbyMovement.AutoResolveVisuals(transform, ref _playerAnimation, ref _frontSprite, ref _backSprite);
        ApplyFacingIfChanged(0);
        PlayerNameplateUI.EnsureAttached(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        ApplyFacingIfChanged(_netFacingMask.Value);
        _netPlayerName.OnValueChanged += HandleNetPlayerNameChanged;

        if (!IsOwner)
        {
            _rigidbody.bodyType = RigidbodyType2D.Kinematic;
            _rigidbody.linearVelocity = Vector2.zero;
            _cachedRemotePlayerName = _netPlayerName.Value.ToString();
            OnPlayerNameChanged?.Invoke(_cachedRemotePlayerName);
            return;
        }

        _netPlayerName.Value = new FixedString64Bytes(PlayerProfileStore.PlayerName);
        PlayerProfileStore.OnProfileChanged += HandleLocalProfileChanged;
        OnPlayerNameChanged?.Invoke(PlayerProfileStore.PlayerName);

        LocalPlayerSpawned?.Invoke(this);
    }

    public override void OnNetworkDespawn()
    {
        _netPlayerName.OnValueChanged -= HandleNetPlayerNameChanged;
        if (IsOwner)
        {
            PlayerProfileStore.OnProfileChanged -= HandleLocalProfileChanged;
            LocalPlayerDespawned?.Invoke(this);
        }
    }

    private void HandleLocalProfileChanged(PlayerProfile profile)
    {
        if (!IsSpawned || !IsOwner || profile == null) return;
        _netPlayerName.Value = new FixedString64Bytes(profile.PlayerName);
        OnPlayerNameChanged?.Invoke(profile.PlayerName);
    }

    private void HandleNetPlayerNameChanged(FixedString64Bytes previousValue, FixedString64Bytes newValue)
    {
        _cachedRemotePlayerName = newValue.ToString();
        OnPlayerNameChanged?.Invoke(_cachedRemotePlayerName);
    }

    /// <summary>
    /// オフライン分身の位置と向きを引き継ぐ（オーナーのみ有効）
    /// </summary>
    public void Teleport(Vector2 position, byte facingMask = 0)
    {
        if (!IsOwner) return;

        _pendingTeleport = position;
        _netFacingMask.Value = facingMask;
        ApplyFacingIfChanged(facingMask);
    }

    private void ApplyPendingTeleport()
    {
        if (!_pendingTeleport.HasValue) return;

        Vector2 position = _pendingTeleport.Value;
        _pendingTeleport = null;

        var target = new Vector3(position.x, position.y, transform.position.z);
        _rigidbody.position = position;
        _rigidbody.linearVelocity = Vector2.zero;

        if (_networkTransform == null)
        {
            TryGetComponent(out _networkTransform);
        }

        if (_networkTransform != null && _networkTransform.CanCommitToTransform)
        {
            _networkTransform.Teleport(target, transform.rotation, transform.localScale);
        }
        else
        {
            transform.position = target;
        }
    }

    private void Update()
    {
        if (!IsSpawned) return;

        if (IsOwner)
        {
            ApplyPendingTeleport();

            _input = LobbyMovement.ReadMoveInput();
            float animSpeed = LobbyMovement.ToAnimSpeed(_input);
            if (Mathf.Abs(_netAnimSpeed.Value - animSpeed) > 0.01f || (animSpeed == 0f && _netAnimSpeed.Value != 0f))
            {
                _netAnimSpeed.Value = animSpeed;
            }

            byte nextFacing = LobbyMovement.UpdateFacingMask(_input, _netFacingMask.Value);
            if (_netFacingMask.Value != nextFacing)
            {
                _netFacingMask.Value = nextFacing;
            }
        }

        ApplyFacingIfChanged(_netFacingMask.Value);

        float syncedAnimSpeed = _netAnimSpeed.Value;
        if (Mathf.Abs(syncedAnimSpeed - _lastAppliedAnimSpeed) > 0.005f || (syncedAnimSpeed == 0f && _lastAppliedAnimSpeed != 0f))
        {
            _lastAppliedAnimSpeed = syncedAnimSpeed;
            LobbyMovement.ApplyAnimation(_playerAnimation, syncedAnimSpeed);
        }
    }

    private void FixedUpdate()
    {
        if (!IsSpawned || !IsOwner) return;

        _rigidbody.linearVelocity = _input * _moveSpeed;
    }

    private void ApplyFacingIfChanged(byte mask)
    {
        if (_lastAppliedFacingMask == mask) return;
        _lastAppliedFacingMask = mask;
        LobbyMovement.ApplyFacing(_frontSprite, _backSprite, mask);
    }
}
