using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class CoinControl : NetworkBehaviour
{
    private enum CoinState
    {
        Up,
        Wait,
        Move
    }

    private CoinState _coinState = CoinState.Up;

    [SerializeField, Header("上昇値")]
    private float _increasedValue = 0.5f;

    [SerializeField, Header("最大上昇量")]
    private float _maxUpValue = 2f;

    [SerializeField, Header("飛び上がって何秒待つ？")]
    private float _waitLimitTime = 1f;

    [SerializeField, Header("プレイヤーに向かう速度")]
    private float _absorbSpeed = 10f;

    [SerializeField, Header("上昇値をだんだん小さくしていく値")]
    private float _risePowerDecay = 0.95f;

    [SerializeField, Header("どれくらい近づいたら消える？")]
    private float _disappearDistance = 0.8f;

    private float _initIncreasedValue = 0f;
    private float _initWaitTime = 0f;

    private GameObject _playerObj = default;
    private readonly string PLAYER_TAGNAME = "Player";
    private bool _hasAbsorbed = false;

    // サーバー上での二重回収防止フラグ
    private bool _serverAbsorbed = false;

    public void SetTargetPlayer(GameObject target)
    {
        _playerObj = target;
        if (IsServer && target != null)
        {
            var netObj = target.GetComponent<NetworkObject>() ?? target.GetComponentInParent<NetworkObject>();
            if (netObj != null)
            {
                SetTargetClientRpc(new NetworkObjectReference(netObj));
            }
        }
    }

    [ClientRpc]
    private void SetTargetClientRpc(NetworkObjectReference targetRef)
    {
        if (targetRef.TryGet(out NetworkObject netObj))
        {
            _playerObj = netObj.gameObject;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // 追従移動はクライアント側で完全ローカル計算するため、NetworkTransformの強制位置上書きを無効化
        var netTransform = GetComponent<NetworkTransform>();
        if (netTransform != null)
        {
            netTransform.enabled = false;
        }

        if (_playerObj == null)
        {
            FindTargetPlayer();
        }
    }

    private void Update()
    {
        // クライアント・サーバー問わず各端末のローカル画面でヌルヌル動かす
        switch (_coinState)
        {
            case CoinState.Up:
                UpMethod();
                break;

            case CoinState.Wait:
                WaitMethod(_waitLimitTime);
                break;

            case CoinState.Move:
                MoveMethod();
                break;
        }
    }

    private void UpMethod()
    {
        if (_initIncreasedValue <= _maxUpValue)
        {
            Vector3 initPos = transform.position;
            initPos.y += _increasedValue * (Time.deltaTime * 60f);
            transform.position = initPos;
            _increasedValue *= Mathf.Pow(_risePowerDecay, Time.deltaTime * 60f);
            _initIncreasedValue += _increasedValue;
        }
        else
        {
            _coinState = CoinState.Wait;
        }
    }

    public void WaitMethod(float waitTime)
    {
        _waitLimitTime = waitTime;
        _initWaitTime += Time.deltaTime;
        if (_waitLimitTime <= _initWaitTime)
        {
            _coinState = CoinState.Move;
        }
    }

    private void MoveMethod()
    {
        if (_hasAbsorbed) return;

        if (_playerObj == null || !_playerObj.activeInHierarchy)
        {
            FindTargetPlayer();
            if (_playerObj == null) return;
        }

        // プレイヤーに向かって加速度的に吸い込む
        Vector3 direction = (_playerObj.transform.position - transform.position).normalized;
        transform.position += direction * _absorbSpeed * Time.deltaTime;

        float distance = Vector3.Distance(_playerObj.transform.position, transform.position);
        if (distance <= _disappearDistance)
        {
            _hasAbsorbed = true;

            var playerNetObj = _playerObj.GetComponent<NetworkObject>() ?? _playerObj.GetComponentInParent<NetworkObject>();

            // サーバー上なら即座に取得・加算
            if (IsServer)
            {
                ApplyAbsorb(playerNetObj);
            }
            else
            {
                // クライアント側で到達した場合、サーバーへ取得をリクエスト
                if (playerNetObj != null)
                {
                    RequestAbsorbServerRpc(new NetworkObjectReference(playerNetObj));
                }
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void RequestAbsorbServerRpc(NetworkObjectReference playerRef)
    {
        if (playerRef.TryGet(out NetworkObject playerNetObj))
        {
            ApplyAbsorb(playerNetObj);
        }
    }

    private void ApplyAbsorb(NetworkObject playerNetObj)
    {
        if (!IsServer || _serverAbsorbed) return;
        _serverAbsorbed = true;

        if (playerNetObj != null)
        {
            SoulKeep soul = playerNetObj.GetComponent<SoulKeep>() ?? playerNetObj.GetComponentInChildren<SoulKeep>();
            if (soul != null)
            {
                soul.AdditionCoin();
            }
        }

        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void FindTargetPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag(PLAYER_TAGNAME);
        if (players == null || players.Length == 0) return;

        // クライアント側では可能な限り自分（ローカルプレイヤー）を優先、無ければ最寄り
        GameObject closest = null;
        float minDst = float.MaxValue;
        Vector3 curPos = transform.position;

        foreach (var p in players)
        {
            if (p == null || !p.activeInHierarchy) continue;
            var netObj = p.GetComponent<NetworkObject>() ?? p.GetComponentInParent<NetworkObject>();
            if (netObj != null && netObj.IsLocalPlayer)
            {
                _playerObj = p;
                return;
            }

            float dst = Vector3.Distance(curPos, p.transform.position);
            if (dst < minDst)
            {
                minDst = dst;
                closest = p;
            }
        }

        _playerObj = closest;
    }
}
