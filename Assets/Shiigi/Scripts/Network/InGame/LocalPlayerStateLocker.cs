using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 自端末のローカルプレイヤー（PlayerStateManager）を探し、ゲーム進行状態に応じて操作ロック/解除を行う責務クラス (SRP)
/// </summary>
public class LocalPlayerStateLocker
{
    private const string PlayerTag = "Player";
    private PlayerStateManager _localPlayerState;

    public bool HasLocalPlayer => _localPlayerState != null;

    /// <summary>
    /// ゲーム開始待機中にローカルプレイヤーが存在すればMovieState（操作ロック）にする
    /// </summary>
    public void TryFindAndLockLocalPlayer()
    {
        if (_localPlayerState == null)
        {
            _localPlayerState = FindLocalPlayerStateManager();
        }

        // プレイヤースポーン時のOnNetworkSpawn等でNormalStateに書き換えられた場合でも再度MovieStateでロックする
        if (_localPlayerState != null && _localPlayerState.PlayerState != PlayerState.Movie)
        {
            _localPlayerState.MovieState();
        }
    }

    /// <summary>
    /// ゲームプレイ開始時にローカルプレイヤーの操作ロックを解除する
    /// </summary>
    public void FindAndUnlockLocalPlayer()
    {
        if (_localPlayerState == null)
        {
            _localPlayerState = FindLocalPlayerStateManager();
        }

        if (_localPlayerState != null)
        {
            _localPlayerState.NormalState();
        }
    }

    private static PlayerStateManager FindLocalPlayerStateManager()
    {
        // NetworkManagerにLocalClientのPlayerObjectが登録されていればタグ検索を省いて高速取得
        var localPlayerObj = NetworkManager.Singleton?.SpawnManager?.GetLocalPlayerObject();
        if (localPlayerObj != null)
        {
            var stateMgr = localPlayerObj.GetComponent<PlayerStateManager>();
            if (stateMgr != null) return stateMgr;
        }

        var playerObjs = GameObject.FindGameObjectsWithTag(PlayerTag);
        foreach (var obj in playerObjs)
        {
            var netObj = obj.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsLocalPlayer)
            {
                return obj.GetComponent<PlayerStateManager>();
            }
        }

        return null;
    }
}
