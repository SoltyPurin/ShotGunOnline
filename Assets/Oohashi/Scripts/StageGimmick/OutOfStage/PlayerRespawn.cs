using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerRespawn : NetworkBehaviour
{
    [SerializeField,Header("リスポーンポイントを登録するリスト")]
    private List<GameObject> _respawnPoints = new List<GameObject>();

    [SerializeField, Header("カメラのオブジェクト")]
    private GameObject _camera = default;

    //現在のリスポーンポイントのインデックス
    private int _currentRespawnIndex = 0; 

    public int RespawnIndex
    {
        get { return _currentRespawnIndex; }
    }

    [SerializeField, Header("プレイヤーのオブジェクト")]
    private GameObject _playerObject = default;

    /// <summary>
    /// 現在のウェーブのインデックスを加算
    /// </summary>
    public void CountUp()
    {
        _currentRespawnIndex++;
    }

    /// <summary>
    /// リストに登録されているリスポーンポイントにリスポーンする
    /// </summary>
    public void Respawn()
    {
        if (IsServer)
        {
            RespawnClientRpc(_currentRespawnIndex);
        }
        else
        {
            DoLocalRespawn(_currentRespawnIndex);
        }
    }

    /// <summary>
    /// 全クライアント端末で各自機プレイヤーのテレポートとカメラ移動を実行する
    /// </summary>
    [ClientRpc]
    private void RespawnClientRpc(int respawnIndex)
    {
        _currentRespawnIndex = respawnIndex;
        DoLocalRespawn(_currentRespawnIndex);
    }

    private void DoLocalRespawn(int respawnIndex)
    {
        if (_respawnPoints == null || _respawnPoints.Count == 0)
        {
            Debug.LogWarning("[PlayerRespawn] _respawnPoints が設定されていません。");
            return;
        }

        if (respawnIndex < 0 || respawnIndex >= _respawnPoints.Count)
        {
            Debug.LogWarning($"[PlayerRespawn] respawnIndex ({respawnIndex}) が範囲外です。");
            respawnIndex = Mathf.Clamp(respawnIndex, 0, _respawnPoints.Count - 1);
        }

        GameObject targetObj = _respawnPoints[respawnIndex];
        if (targetObj == null)
        {
            Debug.LogWarning($"[PlayerRespawn] _respawnPoints[{respawnIndex}] が null です。");
            return;
        }

        Vector2 basePoint = targetObj.transform.position;

        // ネットワークプレイヤーを探索
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        bool localPlayerFound = false;

        foreach (GameObject p in players)
        {
            if (p == null) continue;

            NetworkObject netObj = p.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsOwner)
            {
                localPlayerFound = true;

                // プレイヤー同士が重なりすぎないよう、ClientIdに応じて左右に少しオフセット
                float offsetX = 0f;
                if (NetworkManager.Singleton != null)
                {
                    offsetX = (NetworkManager.Singleton.LocalClientId % 2 == 0) ? -0.8f : 0.8f;
                }
                Vector2 targetPos = basePoint + new Vector2(offsetX, 0f);

                // 慣性リセット
                Rigidbody2D rb = p.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                }

                // 自機の位置を瞬間移動（ClientNetworkTransform で全端末へ同期）
                NetworkTransform netTransform = p.GetComponent<NetworkTransform>();
                if (netTransform != null)
                {
                    netTransform.Teleport(targetPos, Quaternion.identity, p.transform.localScale);
                }
                else
                {
                    p.transform.position = targetPos;
                }

                // 自端末のカメラを即座に移動
                MoveCamera(targetPos);
                Debug.Log($"[PlayerRespawn] 自プレイヤー ({p.name}) をステージ {respawnIndex} (位置: {targetPos}) にテレポートしました。");
                break;
            }
        }

        // オフラインまたは非ネットワーク環境時のフォールバック
        if (!localPlayerFound)
        {
            if (_playerObject != null)
            {
                _playerObject.transform.position = basePoint;
            }
            else
            {
                foreach (GameObject p in players)
                {
                    if (p != null) p.transform.position = basePoint;
                }
            }
            MoveCamera(basePoint);
        }
    }

    private void MoveCamera(Vector2 pos)
    {
        if (_camera == null)
        {
            _camera = Camera.main != null ? Camera.main.gameObject : GameObject.FindWithTag("MainCamera");
        }
        if (_camera != null)
        {
            FollowingCameraToBurrel follow = _camera.GetComponent<FollowingCameraToBurrel>();
            if (follow != null)
            {
                follow.MovieMove(pos);
            }
            else
            {
                Vector3 camPos = pos;
                camPos.z = _camera.transform.position.z;
                _camera.transform.position = camPos;
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void FallRespawnRpc(NetworkObjectReference fallPlayerRef)
    {
        FallRespawnClientRpc(fallPlayerRef);
    }

    [ClientRpc]
    private void FallRespawnClientRpc(NetworkObjectReference fallPlayerRef)
    {
        if (fallPlayerRef.TryGet(out NetworkObject netObj))
        {
            // 落下したプレイヤーの所有者（自機）のみが自身のテレポートとカメラ移動を実行する
            if (netObj.IsOwner)
            {
                if (_respawnPoints != null && _currentRespawnIndex >= 0 && _currentRespawnIndex < _respawnPoints.Count)
                {
                    Vector2 respawnPoint = _respawnPoints[_currentRespawnIndex].transform.position;
                    NetworkTransform netPlayer = netObj.GetComponent<NetworkTransform>();
                    if (netPlayer != null)
                    {
                        netPlayer.Teleport(respawnPoint, Quaternion.identity, netObj.transform.localScale);
                    }
                    else
                    {
                        netObj.transform.position = respawnPoint;
                    }

                    Rigidbody2D rb = netObj.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        rb.linearVelocity = Vector2.zero;
                    }

                    MoveCamera(respawnPoint);
                }
            }
        }
    }
}
