using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.EventSystems;
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
        _viewType = UIViewType.DefaultWindow;
        base.Awake();

        var scrollRect = GetComponentInChildren<ScrollRect>(true);
        if (scrollRect != null && scrollRect.GetComponent<ScrollRectAutoFollower>() == null)
        {
            scrollRect.gameObject.AddComponent<ScrollRectAutoFollower>();
        }

        if (_refreshButton != null)
        {
            _refreshButton.onClick.AddListener(() => OnRefreshRequested?.Invoke());
            EnsureButtonSound(_refreshButton, UIButtonType.SubAction);
            LobbyModernTheme.AttachMetallicVisual(_refreshButton);
        }
        if (_createRoomButton != null)
        {
            _createRoomButton.onClick.AddListener(() => OnCreateRoomRequested?.Invoke());
            EnsureButtonSound(_createRoomButton, UIButtonType.Default);
            LobbyModernTheme.AttachMetallicVisual(_createRoomButton);
        }

        RebuildControllerNavigation();
    }

    private static void EnsureButtonSound(Button button, UIButtonType buttonType)
    {
        if (button == null) return;
        var s = button.GetComponent<UIButtonSound>();
        if (s == null)
        {
            s = button.gameObject.AddComponent<UIButtonSound>();
        }
        s.ButtonType = buttonType;
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

        RebuildControllerNavigation();
    }

    /// <summary>
    /// コントローラー／キーボード操作向けにデフォルトの選択要素へフォーカスを当てる
    /// </summary>
    public void FocusDefaultElement()
    {
        if (EventSystem.current == null || !gameObject.activeInHierarchy || !IsTopActiveView) return;

        var target = GetDefaultFocusTarget();
        if (target != null)
        {
            EventSystem.current.SetSelectedGameObject(target);
        }
    }

    public override GameObject GetDefaultFocusTarget()
    {
        Button firstCardBtn = _spawnedCards
            .Where(c => c != null && c.JoinButton != null && c.JoinButton.gameObject.activeInHierarchy && c.JoinButton.IsInteractable())
            .Select(c => c.JoinButton)
            .FirstOrDefault();

        if (firstCardBtn != null) return firstCardBtn.gameObject;
        if (_createRoomButton != null && _createRoomButton.gameObject.activeInHierarchy && _createRoomButton.IsInteractable()) return _createRoomButton.gameObject;
        if (_refreshButton != null && _refreshButton.gameObject.activeInHierarchy && _refreshButton.IsInteractable()) return _refreshButton.gameObject;
        return null;
    }

    protected override void OnActiveInteractableChanged(bool interactable)
    {
        if (_refreshButton != null) _refreshButton.interactable = interactable;
        if (_createRoomButton != null) _createRoomButton.interactable = interactable;
    }

    protected override bool IsOwnedByThisView(Transform target)
    {
        if (base.IsOwnedByThisView(target)) return true;
        if (_refreshButton != null && target.IsChildOf(_refreshButton.transform)) return true;
        if (_createRoomButton != null && target.IsChildOf(_createRoomButton.transform)) return true;
        return false;
    }

    private void RebuildControllerNavigation()
    {
        var cardButtons = _spawnedCards
            .Where(c => c != null && c.JoinButton != null && c.JoinButton.IsInteractable())
            .Select(c => c.JoinButton)
            .ToList();

        Button fallbackAction = _createRoomButton != null ? _createRoomButton : _refreshButton;

        for (int i = 0; i < cardButtons.Count; i++)
        {
            var nav = new Navigation { mode = Navigation.Mode.Explicit };
            nav.selectOnUp = i > 0 ? cardButtons[i - 1] : _refreshButton;
            nav.selectOnDown = i < cardButtons.Count - 1 ? cardButtons[i + 1] : _createRoomButton;
            nav.selectOnLeft = _refreshButton;
            nav.selectOnRight = fallbackAction;
            cardButtons[i].navigation = nav;
        }

        if (_refreshButton != null)
        {
            var nav = new Navigation { mode = Navigation.Mode.Explicit };
            nav.selectOnUp = cardButtons.Count > 0 ? cardButtons[cardButtons.Count - 1] : _createRoomButton;
            nav.selectOnDown = cardButtons.Count > 0 ? cardButtons[0] : _createRoomButton;
            nav.selectOnLeft = _createRoomButton;
            nav.selectOnRight = _createRoomButton;
            _refreshButton.navigation = nav;
        }

        if (_createRoomButton != null)
        {
            var nav = new Navigation { mode = Navigation.Mode.Explicit };
            nav.selectOnUp = cardButtons.Count > 0 ? cardButtons[cardButtons.Count - 1] : _refreshButton;
            nav.selectOnDown = cardButtons.Count > 0 ? cardButtons[0] : _refreshButton;
            nav.selectOnLeft = _refreshButton;
            nav.selectOnRight = _refreshButton;
            _createRoomButton.navigation = nav;
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
