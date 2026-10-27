using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerRespawn : NetworkBehaviour
{
    [SerializeField, Header("プレイヤー1のリスポーン地点の親オブジェクト")]
    private GameObject _p1RespawnPointParent = default;
    //親オブジェクトだけ登録させて実際のポイント登録はクラス内部で行う
    private List<GameObject> _p1RespawnPoints = new List<GameObject>();

    [SerializeField,Header("プレイヤー2のリスポーン地点の親オブジェクト")]
    private GameObject _p2RespawnPointParent = default;

    private List<GameObject> _p2RespawnPoints = new List<GameObject>();


    [SerializeField,Header("プレイヤー3のリスポーン地点の親オブジェクト")]
    private GameObject _p3RespawnPointParent = default;

    private List<GameObject> _p3RespawnPoints = new List<GameObject>();

    [SerializeField, Header("プレイヤー4のリスポーン地点の親オブジェクト")]
    private GameObject _p4RespawnPointParent = default;

    private List<GameObject> _p4RespawnPoints = new List<GameObject>();

    [SerializeField, Header("カメラのオブジェクト")]
    private GameObject _camera = default;

    //現在のリスポーンポイントのインデックス
    private int _currentRespawnIndex = 0; 

    public int RespawnIndex
    {
        get { return _currentRespawnIndex; }
    }

    [SerializeField, Header("プレイヤーのオブジェクト")]
    private List<GameObject> _playerObjects = new List<GameObject>();

    public void Start()
    {
        GameObject[] pObjs = GameObject.FindGameObjectsWithTag("Player");
        foreach(GameObject obj in pObjs)
        {
            _playerObjects.Add(obj);
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

        // transform を foreach で回すと「直下の子オブジェクト」だけを取得できる（親自身は含まれない）
        foreach (Transform child in parentObj.transform)
        {
            targetList.Add(child.gameObject);
        }
    }
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
        #region ヌルチェック
        if (_p1RespawnPoints == null || _p1RespawnPoints.Count == 0)
        {
            Debug.LogWarning("[PlayerRespawn] _respawnPoints が設定されていません。");
            return;
        }

        if (respawnIndex < 0 || respawnIndex >= _p1RespawnPoints.Count)
        {
            Debug.LogWarning($"[PlayerRespawn] respawnIndex ({respawnIndex}) が範囲外です。");
            respawnIndex = Mathf.Clamp(respawnIndex, 0, _p1RespawnPoints.Count - 1);
        }
        if(_playerObjects.Count == 0)
        {
            Debug.Log("プレイヤーが登録されてない");
            GameObject[] pObjs = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject obj in pObjs)
            {
                _playerObjects.Add(obj);
            }
        }
        #endregion


        // ネットワークプレイヤーを探索
        bool localPlayerFound = false;

        foreach (GameObject p in _playerObjects)
        {
            if (p == null) continue;

            NetworkObject netObj = p.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsOwner)
            {
                localPlayerFound = true;

                PlayerNumberSave numberSave = p.GetComponent<PlayerNumberSave>();

                // プレイヤー同士が重なりすぎないよう、ClientIdに応じて左右に少しオフセット
                float offsetX = 0f;
                if (NetworkManager.Singleton != null)
                {
                    offsetX = (NetworkManager.Singleton.LocalClientId % 2 == 0) ? -0.8f : 0.8f;
                }

                Vector2 targetPos = Vector2.zero;

                Debug.Log("このクライアントの番号は" + numberSave.MyNumber);
                switch (numberSave.MyNumber)
                {
                    case 0:
                        targetPos = _p1RespawnPoints[respawnIndex].transform.position;
                        break;
                    case 1:
                        targetPos = _p2RespawnPoints[respawnIndex].transform.position;
                        Debug.Log("スポーン座標は" + targetPos.x + "," +  targetPos.y);
                        break;

                    case 2:
                        targetPos = _p3RespawnPoints[respawnIndex].transform.position;
                        break;

                    case 3:
                        targetPos = _p4RespawnPoints[respawnIndex].transform.position;
                        break;
                }

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
            if (_playerObjects != null)
            {
                _playerObjects[0].transform.position = _p1RespawnPoints[respawnIndex].transform.position;
            }
            else
            {
                foreach (GameObject p in _playerObjects)
                {
                    if (p != null) p.transform.position = _p1RespawnPoints[respawnIndex].transform.position;
                }
            }
            MoveCamera(_p1RespawnPoints[respawnIndex].transform.position);
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
                if (_p1RespawnPoints != null && _currentRespawnIndex >= 0 && _currentRespawnIndex < _p1RespawnPoints.Count)
                {
                    Vector2 respawnPoint = _p1RespawnPoints[_currentRespawnIndex].transform.position;
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
