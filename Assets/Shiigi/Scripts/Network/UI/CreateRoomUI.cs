using System;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.UI;

public class CreateRoomUI : MonoBehaviour
{
    [SerializeField] private Text _roomName;
    [SerializeField] private Text _roomDescription;
    [SerializeField] private Text _maxPlayers;

    [SerializeField] private Button _createRoomButton;

    [SerializeField] private RoomManager _roomManager;

    private void Start()
    {
        _createRoomButton.onClick.AddListener(OnClickCreateRoomButton);
    }

    public void OnClickCreateRoomButton()
    {
        if (!_roomManager)
        {
            throw new InvalidOperationException("RoomManagerが見つかりません");
        }

        var roomName = _roomName.text;
        var maxPlayers = int.Parse(_maxPlayers.text);
        _roomManager.CreateRoom(roomName, maxPlayers);
    }
}
