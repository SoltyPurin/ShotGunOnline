using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRespawn : MonoBehaviour
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
        _playerObject.transform.position = respawnPoint;
        FollowingCameraToBurrel follow = _camera.GetComponent<FollowingCameraToBurrel>();
        follow.MovieMove(respawnPoint);
    }
}
