using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 自端末のローカルプレイヤー（PlayerStateManager）を追跡し、ゲーム進行状態に応じて操作ロック/解除を行う責務クラス (SRP)。
/// スポーンイベント購読とスロットリングにより、待機中の毎フレーム FindGameObjectsWithTag アロケーションを防ぎます。
/// </summary>
public class LocalPlayerStateLocker : IDisposable
{
    private const string PlayerTag = "Player";
    private const float FallbackSearchInterval = 0.25f;

    private PlayerStateManager _localPlayerState;
    private float _nextFallbackSearchTime;
    private bool _isSubscribed;

    public bool HasLocalPlayer => _localPlayerState != null;

    public LocalPlayerStateLocker()
    {
        SubscribeSpawnEvents();
    }

    public void Dispose()
    {
        if (!_isSubscribed) return;
        PlayerMove.LocalPlayerSpawned -= HandleLocalPlayerSpawned;
        PlayerMove.LocalPlayerDespawned -= HandleLocalPlayerDespawned;
        _isSubscribed = false;
    }

    private void SubscribeSpawnEvents()
    {
        if (_isSubscribed) return;
        PlayerMove.LocalPlayerSpawned += HandleLocalPlayerSpawned;
        PlayerMove.LocalPlayerDespawned += HandleLocalPlayerDespawned;
        _isSubscribed = true;
    }

    private void HandleLocalPlayerSpawned(PlayerMove player)
    {
        if (player != null)
        {
            _localPlayerState = player.GetComponent<PlayerStateManager>();
        }
    }

    private void HandleLocalPlayerDespawned(PlayerMove player)
    {
        if (_localPlayerState != null && player != null && _localPlayerState.gameObject == player.gameObject)
        {
            _localPlayerState = null;
        }
    }

    /// <summary>
    /// ゲーム開始待機中にローカルプレイヤーが存在すればMovieState（操作ロック）にする
    /// </summary>
    public void TryFindAndLockLocalPlayer(bool forceImmediateSearch = false)
    {
        if (_localPlayerState == null)
        {
            _localPlayerState = ResolveLocalPlayerStateManager(forceImmediateSearch);
        }

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
            _localPlayerState = ResolveLocalPlayerStateManager(forceImmediateSearch: true);
        }

        if (_localPlayerState != null)
        {
            _localPlayerState.NormalState();
        }
    }

    private PlayerStateManager ResolveLocalPlayerStateManager(bool forceImmediateSearch)
    {
        var localPlayerObj = NetworkManager.Singleton?.SpawnManager?.GetLocalPlayerObject();
        if (localPlayerObj != null && localPlayerObj.TryGetComponent<PlayerStateManager>(out var stateMgr))
        {
            return stateMgr;
        }

        float now = Time.unscaledTime;
        if (!forceImmediateSearch && now < _nextFallbackSearchTime)
        {
            return null;
        }
        _nextFallbackSearchTime = now + FallbackSearchInterval;

        var playerObjs = GameObject.FindGameObjectsWithTag(PlayerTag);
        for (int i = 0; i < playerObjs.Length; i++)
        {
            var obj = playerObjs[i];
            if (obj != null &&
                obj.TryGetComponent<NetworkObject>(out var netObj) &&
                netObj.IsLocalPlayer &&
                obj.TryGetComponent<PlayerStateManager>(out var foundState))
            {
                return foundState;
            }
        }

        return null;
    }
}
