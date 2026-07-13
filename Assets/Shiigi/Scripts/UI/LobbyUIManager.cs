using UnityEngine;
using UnityEngine.UI;

public enum LobbyState
{
    Searching,
    RoomCreateDialog,
    Room
}

public class LobbyUIManager : MonoBehaviour
{
    [Header("マッチングUI用ボタン")]
    [SerializeField] private Button _refreshButton;
    [SerializeField] private Button _createRoomButton;

    [Header("ルーム検索表示用パネル")]
    [SerializeField] private Transform _roomListContainer;

    [Header("ルーム表示用カードUIプレハブ")] 
    [SerializeField] private GameObject _roomCardPrefab;

    private readonly MultiplayerSessionManager _msm = MultiplayerSessionManager.Instance;

    private void Start()
    {
        _createRoomButton.onClick.AddListener(OnClickCreateRoomButton);
    }

    private void OnClickCreateRoomButton()
    {
        
    }

    private void OnClickOpenCreateRoomUIButton()
    {
        
    }
}
