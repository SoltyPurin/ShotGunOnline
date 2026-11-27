using Unity.Netcode;
using UnityEngine;

public enum PlayerState
{
    Normal,
    KnockBack,
    DamageKnockBack,
    Ultimate,
    Fall,
    Movie,
}


public class PlayerStateManager : NetworkBehaviour
{
    //現在のプレイヤーのステートを保存
    private NetworkVariable<PlayerState> _playerState = new NetworkVariable<PlayerState>(
            PlayerState.Normal,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

    private PlayerState _localState = PlayerState.Normal;

    public PlayerState PlayerState
    {
        get { return IsLocalControl ? _localState : _playerState.Value; }
    }

    private bool IsLocalControl => IsOwner || !IsSpawned;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            _playerState.Value = _localState;
        }
    }

    private void ApplyState(PlayerState nextState)
    {
        if (!IsLocalControl) return;
        _localState = nextState;
        if (IsSpawned && IsOwner && _playerState.Value != nextState)
        {
            _playerState.Value = nextState;
        }
    }

    /// <summary>
    /// ウルトのステートに切り替え
    /// </summary>
    public void UltimateState()
    {
        ApplyState(PlayerState.Ultimate);
    }

    /// <summary>
    /// ノーマルのステートに切り替え
    /// </summary>
    public void NormalState()
    {
        ApplyState(PlayerState.Normal);
    }

    /// <summary>
    /// 落下のステートに切り替え
    /// </summary>
    public void FallState()
    {
        ApplyState(PlayerState.Fall);
    }

    /// <summary>
    /// ノックバックのステート(ショットガンのノックバック)に切り替え
    /// </summary>
    public void KnockBackingState()
    {
        ApplyState(PlayerState.KnockBack);
    }

    /// <summary>
    /// 被弾のノックバックのステートに切り替え
    /// </summary>
    public void DamageKnockBackState()
    {
        ApplyState(PlayerState.DamageKnockBack);
    }

    /// <summary>
    /// ムービー再生中のステートに変更する
    /// </summary>
    public void MovieState()
    {
        ApplyState(PlayerState.Movie);
    }
}
