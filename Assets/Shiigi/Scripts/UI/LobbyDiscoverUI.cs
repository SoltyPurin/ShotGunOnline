using System;
using UnityEngine;
using UnityEngine.UI;

public class LobbyDiscoverUI : MonoBehaviour
{
    [Header("マッチング画面用ボタン")] 
    [SerializeField] private Button _refreshButton;
    [SerializeField] private Button _createRoomButton;

    [Header("ルーム表示用コンテナ")] 
    [SerializeField] private Transform _roomListContainer;

    [Header("ルーム表示用カードUIプレハブ")] 
    [SerializeField] private GameObject _roomCardPrefab;

    public event Action OnCreateRoomCallback;
}
