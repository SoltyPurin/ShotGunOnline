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
        Vector2 respawnPoint = _respawnPoints[_currentRespawnIndex].transform.position;

        if (_playerObject == null)
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach(GameObject p in players)
            {
                p.transform.position = respawnPoint;
            }
        }
        else
        {
            _playerObject.transform.position = respawnPoint;
        }
            FollowingCameraToBurrel follow = _camera.GetComponent<FollowingCameraToBurrel>();
        follow.MovieMove(respawnPoint);
    }

    [Rpc(SendTo.Server)]
    public void FallRespawnRpc(NetworkObjectReference fallPlayerRef)
    {
        if (fallPlayerRef.TryGet(out NetworkObject netObj))
        {

            NetworkTransform netPlayer = netObj.GetComponent<NetworkTransform>();
            Vector2 respawnPoint = _respawnPoints[_currentRespawnIndex].transform.position;
            if (netPlayer != null)
            {
                netPlayer.Teleport(respawnPoint, Quaternion.identity, netObj.transform.localScale);
            }
            else
            {
                netObj.transform.position = respawnPoint;
            }
            FollowingCameraToBurrel follow = _camera.GetComponent<FollowingCameraToBurrel>();
            follow.MovieMove(respawnPoint);
        }
    }
}
