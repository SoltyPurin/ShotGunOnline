using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerRespawn : NetworkBehaviour
{
    [SerializeField, Header("ウェーブごとのステージ移動・リスポーン先リスト")]
    private List<GameObject> _respawnPoints = new List<GameObject>();

    [SerializeField, Header("プレイヤー1のリスポーン地点の親オブジェクト（個別指定時）")]
    private GameObject _p1RespawnPointParent = default;
    private List<GameObject> _p1RespawnPoints = new List<GameObject>();

    [SerializeField, Header("プレイヤー2のリスポーン地点の親オブジェクト（個別指定時）")]
    private GameObject _p2RespawnPointParent = default;
    private List<GameObject> _p2RespawnPoints = new List<GameObject>();

    [SerializeField, Header("プレイヤー3のリスポーン地点の親オブジェクト（個別指定時）")]
    private GameObject _p3RespawnPointParent = default;
    private List<GameObject> _p3RespawnPoints = new List<GameObject>();

    [SerializeField, Header("プレイヤー4のリスポーン地点の親オブジェクト（個別指定時）")]
    private GameObject _p4RespawnPointParent = default;
    private List<GameObject> _p4RespawnPoints = new List<GameObject>();

    [SerializeField, Header("カメラのオブジェクト")]
    private GameObject _camera = default;

    // 現在のリスポーンポイントのインデックス（ウェーブ進行度）
    private int _currentRespawnIndex = 0;

    public int RespawnIndex
    {
        get { return _currentRespawnIndex; }
    }

    [SerializeField, Header("プレイヤーのオブジェクト")]
    private List<GameObject> _playerObjects = new List<GameObject>();

    public void Start()
    {
        // _respawnPoints がインスペクター未設定の場合、自身の子オブジェクト（First, Second, ...）を自動登録
        if (_respawnPoints == null || _respawnPoints.Count == 0)
        {
            _respawnPoints = new List<GameObject>();
            foreach (Transform child in transform)
            {
                _respawnPoints.Add(child.gameObject);
            }
        }

        GetChildRespawnPoints(_p1RespawnPointParent, _p1RespawnPoints);
        GetChildRespawnPoints(_p2RespawnPointParent, _p2RespawnPoints);
        GetChildRespawnPoints(_p3RespawnPointParent, _p3RespawnPoints);
        GetChildRespawnPoints(_p4RespawnPointParent, _p4RespawnPoints);
    }

    /// <summary>
    /// 親オブジェクトの直下にある子オブジェクトだけをリストに登録する共通メソッド
    /// </summary>
    private void GetChildRespawnPoints(GameObject parentObj, List<GameObject> targetList)
    {
        targetList.Clear();
        if (parentObj == null) return;

        foreach (Transform child in parentObj.transform)
        {
            targetList.Add(child.gameObject);
        }
    }

    /// <summary>
    /// 現在のウェーブのインデックスを加算（次のステージエリアへ進む）
    /// </summary>
    public void CountUp()
    {
        _currentRespawnIndex++;
        Debug.Log($"[PlayerRespawn] CountUp 実行。現在のステージIndex: {_currentRespawnIndex}");
    }

    /// <summary>
    /// リストに登録されているリスポーンポイントにリスポーン・テレポートする
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
        // 自端末のプレイヤーオブジェクトを取得してテレポート
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        bool localPlayerFound = false;

        foreach (GameObject p in players)
        {
            if (p == null) continue;

            NetworkObject netObj = p.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsOwner)
            {
                localPlayerFound = true;
                Vector2 targetPos = GetRespawnPosition(netObj, respawnIndex);

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
            Vector2 fallbackPos = GetRespawnPosition(null, respawnIndex);
            if (players.Length > 0 && players[0] != null)
            {
                players[0].transform.position = fallbackPos;
            }
            MoveCamera(fallbackPos);
        }
    }

    /// <summary>
    /// プレイヤー情報とステージIndexに応じたリスポーン位置を取得する
    /// </summary>
    public Vector2 GetRespawnPosition(NetworkObject netObj, int index)
    {
        int playerNumber = 0;
        if (netObj != null)
        {
            PlayerNumberSave numberSave = netObj.GetComponent<PlayerNumberSave>();
            if (numberSave != null)
            {
                playerNumber = numberSave.MyNumber;
            }
            else
            {
                playerNumber = (int)(netObj.OwnerClientId % 4);
            }
        }

        // 1. 各プレイヤー個別のリスポーンリストがあれば最優先で使用
        List<GameObject> dedicatedList = playerNumber switch
        {
            0 => _p1RespawnPoints,
            1 => _p2RespawnPoints,
            2 => _p3RespawnPoints,
            3 => _p4RespawnPoints,
            _ => _p1RespawnPoints
        };

        if (dedicatedList != null && dedicatedList.Count > 0)
        {
            int clampedIndex = Mathf.Clamp(index, 0, dedicatedList.Count - 1);
            if (dedicatedList[clampedIndex] != null)
            {
                return dedicatedList[clampedIndex].transform.position;
            }
        }

        // 2. 本線: ウェーブごとのステージ移動先リスト (_respawnPoints: First, Second, Third...) を使用
        if (_respawnPoints != null && _respawnPoints.Count > 0)
        {
            int clampedIndex = Mathf.Clamp(index, 0, _respawnPoints.Count - 1);
            if (_respawnPoints[clampedIndex] != null)
            {
                Vector2 basePos = _respawnPoints[clampedIndex].transform.position;
                // プレイヤー同士が重なりすぎないよう、プレイヤー番号に応じて左右に少しオフセット
                float offsetX = (playerNumber % 2 == 0 ? -1f : 1f) * (playerNumber < 2 ? 0.8f : 1.6f);
                return basePos + new Vector2(offsetX, 0f);
            }
        }

        // 3. Stage 0 のフォールバック: シーンの StageSpawnPoints
        if (index == 0 && StageSpawnPoints.Instance != null)
        {
            Transform stageSpawn = StageSpawnPoints.Instance.GetSpawnPoint(playerNumber);
            if (stageSpawn != null)
            {
                return stageSpawn.position;
            }
        }

        // 4. 最終フォールバック
        return transform.position;
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
                // 現在のウェーブのステージ位置を取得
                Vector2 respawnPoint = GetRespawnPosition(netObj, _currentRespawnIndex);

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
