using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

public class LobbyDiscoverUI : LobbyViewBase
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

    protected override void Awake()
    {
        base.Awake();

        if (_refreshButton != null)
        {
            _refreshButton.onClick.AddListener(() => OnRefreshRequested?.Invoke());
            EnsureButtonSound(_refreshButton, UISoundType.Enter);
        }
        if (_createRoomButton != null)
        {
            _createRoomButton.onClick.AddListener(() => OnCreateRoomRequested?.Invoke());
            EnsureButtonSound(_createRoomButton, UISoundType.Enter);
        }
    }

    private static void EnsureButtonSound(Button button, UISoundType soundType)
    {
        if (button.GetComponent<UIButtonSound>() == null)
        {
            var s = button.gameObject.AddComponent<UIButtonSound>();
            s.ClickSound = soundType;
        }
    }

    protected override void OnShowStarted()
    {
        SetButtonsActive(true);
    }

    protected override void OnHideCompleted()
    {
        SetButtonsActive(false);
    }

    private void SetButtonsActive(bool active)
    {
        if (_refreshButton != null) _refreshButton.gameObject.SetActive(active);
        if (_createRoomButton != null) _createRoomButton.gameObject.SetActive(active);
    }

    /// <summary>
    /// ルーム一覧パネルおよび作成・更新ボタンの表示/非表示を一括で切り替える（即時反映）
    /// </summary>
    public void SetVisible(bool visible)
    {
        SetVisibleInstant(visible);
        SetButtonsActive(visible);
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
