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

    public PlayerState PlayerState
    {
        get { return _playerState.Value; }
    }
    //private void Update()
    //{
    //    Debug.Log(_playerState);
    //}

    /// <summary>
    /// ウルトのステートに切り替え
    /// </summary>
    public void UltimateState()
    {
        if (!IsOwner) return;
        _playerState.Value = PlayerState.Ultimate;
    }

    /// <summary>
    /// ノーマルのステートに切り替え
    /// </summary>
    public void NormalState()
    {
        if (!IsOwner) return;
        _playerState.Value = PlayerState.Normal;
    }

    /// <summary>
    /// 落下のステートに切り替え
    /// </summary>
    public void FallState()
    {
        if (!IsOwner) return;
        _playerState.Value = PlayerState.Fall;
    }

    /// <summary>
    /// ノックバックのステート(ショットガンのノックバック)に切り替え
    /// </summary>
    public void KnockBackingState()
    {
        if (!IsOwner) return;
        _playerState.Value = PlayerState.KnockBack;
    }

    /// <summary>
    /// 被弾のノックバックのステートに切り替え
    /// </summary>
    public void DamageKnockBackState()
    {
        if (!IsOwner) return;
        _playerState.Value = PlayerState.DamageKnockBack;
    }

    /// <summary>
    /// ムービー再生中のステートに変更する
    /// </summary>
    public void MovieState()
    {
        if (!IsOwner) return;
        _playerState.Value = PlayerState.Movie;
    }
}
