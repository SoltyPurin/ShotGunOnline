using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// ロビー（集会所）用のネットワークプレイヤー。
/// 位置同期は ClientNetworkTransform（オーナー権限）に任せ、ここでは入力・移動・向き・アニメ速度の同期を担当します。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class LobbyNetworkPlayer : NetworkBehaviour
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

    private Rigidbody2D _rigidbody;
    private Vector2 _input;
    private Vector2? _pendingTeleport;

    public byte FacingMask => _netFacingMask.Value;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _rigidbody.gravityScale = 0f;
        _rigidbody.freezeRotation = true;

        LobbyMovement.AutoResolveVisuals(transform, ref _playerAnimation, ref _frontSprite, ref _backSprite);
        LobbyMovement.ApplyFacing(_frontSprite, _backSprite, 0);
    }

    public override void OnNetworkSpawn()
    {
        LobbyMovement.ApplyFacing(_frontSprite, _backSprite, _netFacingMask.Value);

        // 他プレイヤーの物理は動かさず、同期された位置に任せる
        if (!IsOwner)
        {
            _rigidbody.bodyType = RigidbodyType2D.Kinematic;
            _rigidbody.linearVelocity = Vector2.zero;
            return;
        }

        LocalPlayerSpawned?.Invoke(this);
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            LocalPlayerDespawned?.Invoke(this);
        }
    }

    /// <summary>
    /// オフライン分身の位置と向きを引き継ぐ（オーナーのみ有効）
    /// OnNetworkSpawn 中は NetworkTransform の初期化前の可能性があるため、次の Update で反映する
    /// </summary>
    public void Teleport(Vector2 position, byte facingMask = 0)
    {
        if (!IsOwner) return;

        _pendingTeleport = position;
        _netFacingMask.Value = facingMask;
        LobbyMovement.ApplyFacing(_frontSprite, _backSprite, facingMask);
    }

    private void ApplyPendingTeleport()
    {
        if (!_pendingTeleport.HasValue) return;

        Vector2 position = _pendingTeleport.Value;
        _pendingTeleport = null;

        var target = new Vector3(position.x, position.y, transform.position.z);
        _rigidbody.position = position;
        _rigidbody.linearVelocity = Vector2.zero;

        // 補間せず瞬間移動として他端末に同期する
        if (TryGetComponent(out NetworkTransform networkTransform) && networkTransform.CanCommitToTransform)
        {
            networkTransform.Teleport(target, transform.rotation, transform.localScale);
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

        LobbyMovement.ApplyFacing(_frontSprite, _backSprite, _netFacingMask.Value);
        LobbyMovement.ApplyAnimation(_playerAnimation, _netAnimSpeed.Value);
    }

    private void FixedUpdate()
    {
        if (!IsSpawned || !IsOwner) return;

        _rigidbody.linearVelocity = _input * _moveSpeed;
    }
}
