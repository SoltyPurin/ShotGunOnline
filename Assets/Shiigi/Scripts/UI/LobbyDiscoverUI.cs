using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

public class LobbyDiscoverUI : MonoBehaviour
{
    [Header("マッチング用ボタン")] [SerializeField] private Button _refreshButton;
    [SerializeField] private Button _createRoomButton;

    [Header("ルーム用コンテナ")] [SerializeField] private Transform _roomListContainer;

    [Header("ルーム用カードUIプレハブ")] [SerializeField]
    private RoomCardUI _roomCardPrefab;

    [Header("ローディング表示用オブジェクト")] [SerializeField]
    private GameObject _loadingOverlay;

    // Presenterが購読するためのイベント群
    public event Action OnRefreshRequested;
    public event Action OnCreateRoomRequested;
    public event Action<ISessionInfo> OnJoinRoomRequested;

    private readonly List<RoomCardUI> _spawnedCards = new List<RoomCardUI>();

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

        _spawnedCards.Clear();
    }

    /// <summary>
    /// 通信中のUI非活性化・ローディング表示制御
    /// </summary>
    public void SetLoadingState(bool isLoading)
    {
        if (_loadingOverlay != null)
        {
            _loadingOverlay.SetActive(isLoading);
        }

        _refreshButton.interactable = !isLoading;
        _createRoomButton.interactable = !isLoading;
    }
}
