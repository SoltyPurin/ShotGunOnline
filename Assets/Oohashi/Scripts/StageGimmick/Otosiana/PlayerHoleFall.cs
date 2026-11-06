using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerHoleFall : EntityFall
{
    //落とし穴に落ちたら操作不可能＋初期地点(0,0)に移動
    [SerializeField] private Animator _animator;
    [SerializeField] private SoulKeep _soul = default;
    [SerializeField] private PlayerStateManager _playerStateManager = default;
    [SerializeField] private GameObject _playerObject = default;
    [SerializeField] private GameObject _playerImage = default;
    [SerializeField] private GameObject _dropCoinObject = default;
    [SerializeField, Header("リジッドボディを取得")]
    private Rigidbody2D _rigidbody2D = default;
    [SerializeField, Header("ショットガンの吹き飛び")]
    private PlayerKnockBack _knockBack = default;
    [SerializeField, Header("ダメージの吹き飛び")]
    private PlayerDamageKnockBack _damageKnockBack = default;
    [SerializeField, Header("SEマネージャー")]
    private SEManager _seManager = default;
    [SerializeField, Header("リスポーンのスクリプト")]
    private PlayerRespawn _respawn = default;
    [SerializeField, Header("ステートチェンジ用スクリプト")]
    private InputChangeState _inputChangeState = default;

    [Header("アニメーター")]
    [SerializeField, Tooltip("フェードのアニメーター（PlayerUI内にある）")]
    private Animator _fadeAnimator = default;

    private bool _isFalling = false;

    //---アニメーターのトリガー---//
    private readonly string FALLTRIGGER = "Fall";

    private void Start()
    {
        _soul = GetComponent<SoulKeep>();
    }

    /// <summary>
    /// リスポーン座標に移動するコルーチン
    /// </summary>
    /// <returns></returns>
    private IEnumerator MoveToRespawnPoint()
    {
        transform.SetParent(null);
        if (_respawn == null)
        {
            GameObject respawnObj = GameObject.FindWithTag("RespawnPoint");
            if (respawnObj != null)
            {
                _respawn = respawnObj.GetComponent<PlayerRespawn>();
            }
            if (_respawn == null)
            {
                _respawn = FindAnyObjectByType<PlayerRespawn>();
            }
        }
        yield return new WaitForSeconds(2.2f);

        // 物理シミュレーションや通常状態に戻す前に、自機を確実にリスポーン地点へテレポートさせる
        TeleportToRespawnPoint();

        // 吹き飛び力をリセット
        if (_damageKnockBack != null)
        {
            _damageKnockBack.ResetForce();
        }

        // 慣性を確実にゼロにして物理シミュレーションを再開（リスポーン地点に移動後に行う）
        if (_rigidbody2D != null)
        {
            _rigidbody2D.linearVelocity = Vector2.zero;
            _rigidbody2D.simulated = true;
        }

        // フェードイン演出開始
        if (_fadeAnimator != null)
        {
            _fadeAnimator.SetTrigger("FadeIn");
        }

        // プレイヤー状態とアニメーションを通常へ戻す
        if (_playerStateManager != null)
        {
            _playerStateManager.NormalState();
        }
        if (_inputChangeState != null)
        {
            _inputChangeState.ToNormalAnimation();
        }
        _isFalling = false;
    }

    /// <summary>
    /// 落下位置からリスポーン位置へ自機を即座にテレポートし、カメラも移動させる
    /// </summary>
    private void TeleportToRespawnPoint()
    {
        NetworkObject netObj = GetComponent<NetworkObject>();
        Vector2 targetPos = Vector2.zero;
        bool hasTarget = false;

        // A. PlayerRespawn から取得
        if (_respawn != null)
        {
            targetPos = _respawn.RespawnPlayerLocally(netObj);
            hasTarget = true;
        }
        // B. StageController.ActiveStage から直接取得（_respawn が見つからない場合のフォールバック）
        else if (StageController.ActiveStage != null)
        {
            int playerNumber = GetPlayerNumber(netObj);
            targetPos = StageController.ActiveStage.GetRespawnPosition(playerNumber);
            hasTarget = true;
        }
        // C. StageSpawnPoints から直接取得
        else if (StageSpawnPoints.Instance != null)
        {
            int playerNumber = GetPlayerNumber(netObj);
            Transform spawn = StageSpawnPoints.Instance.GetSpawnPoint(playerNumber);
            if (spawn != null)
            {
                targetPos = spawn.position;
                hasTarget = true;
            }
        }

        if (!hasTarget)
        {
            Debug.LogWarning("[PlayerHoleFall] リスポーン先が特定できなかったため初期位置 (0, 0) へ移動します。");
            targetPos = Vector2.zero;
        }

        // _respawn が null だった場合のフォールバックテレポート処理
        if (_respawn == null)
        {
            Unity.Netcode.Components.NetworkTransform netTransform = GetComponent<Unity.Netcode.Components.NetworkTransform>();
            if (netTransform != null)
            {
                netTransform.Teleport(targetPos, Quaternion.identity, transform.localScale);
            }
            else
            {
                transform.position = targetPos;
            }

            MoveCameraTo(targetPos);
        }

        // ネットワーク同期補完のためサーバーへもRPC通知
        if (_respawn != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            _respawn.FallRespawnRpc(this.gameObject);
        }
    }

    private int GetPlayerNumber(NetworkObject netObj)
    {
        if (netObj != null)
        {
            PlayerNumberSave numberSave = netObj.GetComponent<PlayerNumberSave>();
            if (numberSave != null)
            {
                return numberSave.MyNumber;
            }
            return (int)(netObj.OwnerClientId % 4);
        }
        return 0;
    }

    private void MoveCameraTo(Vector2 pos)
    {
        Camera cam = Camera.main != null ? Camera.main : (GameObject.FindWithTag("MainCamera") != null ? GameObject.FindWithTag("MainCamera").GetComponent<Camera>() : null);
        if (cam != null)
        {
            FollowingCameraToBurrel follow = cam.GetComponent<FollowingCameraToBurrel>();
            if (follow != null)
            {
                follow.MovieMove(pos);
            }
            else
            {
                Vector3 camPos = pos;
                camPos.z = cam.transform.position.z;
                cam.transform.position = camPos;
            }
        }
    }

    /// <summary>
    /// 落下メソッド
    /// </summary>
    public override void Fall()
    {
        // マルチプレイ時、自機（所有権を持つローカルプレイヤー）のみ落下・画面エフェクト・リスポーンを実行
        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null && !netObj.IsOwner)
        {
            return;
        }

        if (_isFalling)
        {
            return;
        }

        // ムービー演出中は落下死させない（_isFalling を true にする前に判定する）
        if (_playerStateManager != null && _playerStateManager.PlayerState == PlayerState.Movie)
        {
            return;
        }

        _isFalling = true;
        _animator.SetTrigger(FALLTRIGGER);

        _knockBack.Fall();
        _damageKnockBack.Fall();

        if (!_damageKnockBack.IsInvincible)
        {
            // 無敵時間中でなければコイン減らす・被弾演出
            StartCoroutine(_damageKnockBack.DamagePerformance());
        }

        _rigidbody2D.simulated = false;
        _fadeAnimator.SetTrigger("FadeOut");
        if (_seManager != null)
        {
            _seManager.PlayDropSound();
        }
        _playerStateManager.FallState();
        StartCoroutine(MoveToRespawnPoint());
    }
}
