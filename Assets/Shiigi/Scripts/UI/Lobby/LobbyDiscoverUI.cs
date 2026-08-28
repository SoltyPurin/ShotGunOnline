using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

public class LobbyDiscoverUI : MonoBehaviour
{
    [Header("マッチング用ボタン")][SerializeField] private Button _refreshButton;
    [SerializeField] private Button _createRoomButton;

    [Header("ルーム用コンテナ")][SerializeField] private Transform _roomListContainer;

    [Header("ルーム用カードUIプレハブ")]
    [SerializeField]
    private RoomCardUI _roomCardPrefab;

    // Presenterが購読するためのイベント群
    public event Action OnRefreshRequested;
    public event Action OnCreateRoomRequested;
    public event Action<ISessionInfo> OnJoinRoomRequested;

    private readonly List<RoomCardUI> _spawnedCards = new();

    private void Awake()
    {
        _refreshButton.onClick.AddListener(() => OnRefreshRequested?.Invoke());
        _createRoomButton.onClick.AddListener(() => OnCreateRoomRequested?.Invoke());
    }

    /// <summary>
    /// ルーム一覧をグリッドに再描画する
    /// </summary>
    public void UpdateRoomList(List<ISessionInfo> sessions)
    {
        ClearRoomList();

        foreach (var session in sessions)
        {
            var card = Instantiate(_roomCardPrefab, _roomListContainer);
            card.Setup(session);

            // カード個別の参加ボタンイベントを中継して公開
            card.OnJoinRequested += HandleJoinRequest;
            _spawnedCards.Add(card);
        }
    }

    private void HandleJoinRequest(ISessionInfo session)
    {
        OnJoinRoomRequested?.Invoke(session);
    }

    private void ClearRoomList()
    {
        foreach (var card in _spawnedCards.Where(card => card != null))
        {
            card.OnJoinRequested -= HandleJoinRequest;
            Destroy(card.gameObject);
        }

        // カードコンテナの子オブジェクトをクリア
        for (var i = 0; i < _roomListContainer.childCount; i++)
        {
            Destroy(_roomListContainer.GetChild(i).gameObject);
        }

        _spawnedCards.Clear();
    }
}
