using System;
using Unity.Netcode;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    private RoomInfo _roomInfo;

    private void Update()
    {
        UpdateRoomInfo();
    }

    private void UpdateRoomInfo()
    {
        // ルーム内のプレイヤー数を更新
        _roomInfo.CurrentPlayers = NetworkManager.Singleton.ConnectedClients.Count;
    }

    /// <summary>
    /// ルームを作成する
    /// </summary>
    /// <param name="roomName">ルーム名</param>
    /// <param name="maxPlayers">参加可能人数</param>
    public void CreateRoom(string roomName, int maxPlayers)
    {
        _roomInfo.RoomName = roomName;
        _roomInfo.MaxPlayers = maxPlayers;
    }

    public void CloseRoom()
    {
        
    }

    public int CurrentPlayers => _roomInfo.CurrentPlayers;
}
