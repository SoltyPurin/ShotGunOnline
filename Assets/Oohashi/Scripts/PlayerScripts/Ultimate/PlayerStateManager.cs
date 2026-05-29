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


public class PlayerStateManager : MonoBehaviour
{
    //現在のプレイヤーのステートを保存
    private PlayerState _playerState = PlayerState.Normal;

    public PlayerState PlayerState
    {
        get { return _playerState; }
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
        _playerState = PlayerState.Ultimate;
    }

    /// <summary>
    /// ノーマルのステートに切り替え
    /// </summary>
    public void NormalState()
    {
        _playerState = PlayerState.Normal;
    }

    /// <summary>
    /// 落下のステートに切り替え
    /// </summary>
    public void FallState()
    {
        _playerState = PlayerState.Fall;
    }

    /// <summary>
    /// ノックバックのステート(ショットガンのノックバック)に切り替え
    /// </summary>
    public void KnockBackingState()
    {
        _playerState = PlayerState.KnockBack;
    }

    /// <summary>
    /// 被弾のノックバックのステートに切り替え
    /// </summary>
    public void DamageKnockBackState()
    {
        _playerState = PlayerState.DamageKnockBack;
    }

    /// <summary>
    /// ムービー再生中のステートに変更する
    /// </summary>
    public void MovieState()
    {
        _playerState = PlayerState.Movie;
    }
}
