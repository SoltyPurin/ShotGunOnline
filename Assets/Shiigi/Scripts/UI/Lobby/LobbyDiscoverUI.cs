using System;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LobbyDiscoverUI : LobbyViewBase
{
    private static readonly Color InGameDetailColor = new Color(0.94f, 0.50f, 0.40f, 1f);
    private static readonly Color WaitingDetailColor = new Color(0.36f, 0.92f, 0.80f, 1f);
    private static readonly Color EmptyDetailColor = new Color(0.78f, 0.69f, 0.51f, 1f);
    private const float SameRowYThreshold = 10f;

    [Header("マッチング用ボタン")]
    [SerializeField] private Button _refreshButton;
    [SerializeField] private Button _createRoomButton;
    [SerializeField] private Button _backButton;

    [Header("ルーム用コンテナ")]
    [SerializeField] private Transform _roomListContainer;
    [SerializeField] private GameObject _emptyStateObject;

    [Header("ルーム用カードUIプレハブ")]
    [SerializeField] private RoomCardUI _roomCardPrefab;

    [Header("ルーム詳細パネル")]
    [SerializeField] private GameObject _roomMenuRoot;
    [SerializeField] private GameObject _roomDetailPanel;
    [SerializeField] private TMP_Text _detailRoomNameText;
    [SerializeField] private TMP_Text _detailPlayerCountText;
    [SerializeField] private TMP_Text _detailGameStatusText;
    [SerializeField] private TMP_Text _detailProtocolText;

    public event Action OnRefreshRequested;
    public event Action OnCreateRoomRequested;
    public event Action OnBackRequested;
    public event Action<ISessionInfo> OnJoinRoomRequested;

    private readonly List<RoomCardUI> _spawnedCards = new();
    private readonly List<RoomCardUI> _activeCardsBuffer = new();
    private readonly List<(Button Button, Vector2 Pos)> _cardRectBuffer = new();
    private readonly List<List<Button>> _gridRowsBuffer = new();
    private readonly List<List<Button>> _gridRowPool = new();
    private ISessionInfo _selectedSession;
    private RoomCardUI _selectedCard;

    protected override void Awake()
    {
        _viewType = UIViewType.DefaultWindow;
        base.Awake();

        ResolveDetailPanelReferences();

        var scrollRect = GetComponentInChildren<ScrollRect>(true);
        if (scrollRect != null && !scrollRect.TryGetComponent<ScrollRectAutoFollower>(out _))
        {
            scrollRect.gameObject.AddComponent<ScrollRectAutoFollower>();
        }

        if (_refreshButton != null)
        {
            _refreshButton.onClick.AddListener(() => OnRefreshRequested?.Invoke());
            UIButtonSound.ConfigureButton(_refreshButton, UIButtonType.SubAction);
        }
        if (_createRoomButton != null)
        {
            _createRoomButton.onClick.AddListener(() => OnCreateRoomRequested?.Invoke());
            UIButtonSound.ConfigureButton(_createRoomButton, UIButtonType.Default);
        }
        if (_backButton != null)
        {
            _backButton.onClick.AddListener(() => OnBackRequested?.Invoke());
            UIButtonSound.EnsureAttached(_backButton, UIButtonType.Cancel);
        }

        ClearRoomDetail();
        RebuildControllerNavigation();
    }

    protected override void OnBeforeShow()
    {
        SetMenuRootActive(true);
    }

    protected override void OnShowStarted()
    {
        SetButtonsActive(true);
    }

    protected override void OnShowCompleted()
    {
        RebuildControllerNavigation();
    }

    protected override void OnHideCompleted()
    {
        SetButtonsActive(false);
        SetMenuRootActive(false);
    }

    private void SetButtonsActive(bool active)
    {
        if (_refreshButton != null) _refreshButton.gameObject.SetActive(active);
        if (_createRoomButton != null) _createRoomButton.gameObject.SetActive(active);
    }

    public void SetMenuRootActive(bool active)
    {
        EnsureRoomMenuRoot();
        if (_roomMenuRoot != null && _roomMenuRoot.activeSelf != active)
        {
            _roomMenuRoot.SetActive(active);
        }
    }

    public void SetVisible(bool visible)
    {
        if (visible)
        {
            SetMenuRootActive(true);
        }

        SetVisibleInstant(visible);
        SetButtonsActive(visible);

        if (!visible)
        {
            SetMenuRootActive(false);
        }
    }

    /// <summary>
    /// ルーム一覧を再描画する
    /// </summary>
    public void UpdateRoomList(List<ISessionInfo> sessions)
    {
        string previousSelectedId = _selectedSession?.Id;
        ClearRoomList();

        if (sessions != null)
        {
            for (int i = 0; i < sessions.Count; i++)
            {
                var session = sessions[i];
                var card = Instantiate(_roomCardPrefab, _roomListContainer);
                card.Setup(session);

                card.OnCardSelected += HandleCardSelected;
                card.OnCardSubmitted += HandleCardSubmitted;
                card.OnJoinRequested += HandleJoinRequest;
                _spawnedCards.Add(card);
            }
        }

        if (_roomListContainer is RectTransform containerRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
        }

        if (_spawnedCards.Count > 0)
        {
            if (_emptyStateObject != null) _emptyStateObject.SetActive(false);

            RoomCardUI initialCard = FindCardBySessionId(previousSelectedId) ?? _spawnedCards[0];
            _selectedCard = initialCard;
            UpdateSelectedCardVisuals();
            ShowRoomDetail(initialCard.SessionInfo);
        }
        else
        {
            if (_emptyStateObject != null) _emptyStateObject.SetActive(true);
            ClearRoomDetail();
        }

        RebuildControllerNavigation();
    }

    private RoomCardUI FindCardBySessionId(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId)) return null;
        for (int i = 0; i < _spawnedCards.Count; i++)
        {
            var card = _spawnedCards[i];
            if (card != null && card.SessionInfo != null && card.SessionInfo.Id == sessionId)
            {
                return card;
            }
        }
        return null;
    }

    public override GameObject GetDefaultFocusTarget()
    {
        for (int i = 0; i < _spawnedCards.Count; i++)
        {
            var card = _spawnedCards[i];
            if (card != null && card.JoinButton != null && card.JoinButton.gameObject.activeInHierarchy && card.JoinButton.interactable)
            {
                return card.JoinButton.gameObject;
            }
        }

        if (_createRoomButton != null && _createRoomButton.gameObject.activeInHierarchy && _createRoomButton.interactable) return _createRoomButton.gameObject;
        if (_refreshButton != null && _refreshButton.gameObject.activeInHierarchy && _refreshButton.interactable) return _refreshButton.gameObject;
        return null;
    }

    protected override void OnActiveInteractableChanged(bool interactable)
    {
        if (_refreshButton != null) _refreshButton.interactable = interactable;
        if (_createRoomButton != null) _createRoomButton.interactable = interactable;
        if (_backButton != null) _backButton.interactable = interactable;
        if (interactable)
        {
            RebuildControllerNavigation();
        }
    }

    protected override bool IsOwnedByThisView(Transform target)
    {
        if (base.IsOwnedByThisView(target)) return true;
        if (_roomMenuRoot != null && target.IsChildOf(_roomMenuRoot.transform)) return true;
        if (_refreshButton != null && target.IsChildOf(_refreshButton.transform)) return true;
        if (_createRoomButton != null && target.IsChildOf(_createRoomButton.transform)) return true;
        return false;
    }

    private void RebuildControllerNavigation()
    {
        _activeCardsBuffer.Clear();
        for (int i = 0; i < _spawnedCards.Count; i++)
        {
            var c = _spawnedCards[i];
            if (c != null && c.JoinButton != null && c.JoinButton.interactable)
            {
                _activeCardsBuffer.Add(c);
            }
        }

        List<List<Button>> rows = BuildCardButtonGrid(_activeCardsBuffer);
        Button primaryTopLeft = _refreshButton != null ? _refreshButton : _createRoomButton;
        Button primaryTopRight = _createRoomButton != null ? _createRoomButton : _refreshButton;

        for (int r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            for (int c = 0; c < row.Count; c++)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };

                if (r > 0)
                {
                    var upRow = rows[r - 1];
                    nav.selectOnUp = upRow[Mathf.Min(c, upRow.Count - 1)];
                }
                else
                {
                    nav.selectOnUp = (c == 0 && row.Count > 1 && _refreshButton != null)
                        ? _refreshButton
                        : primaryTopRight;
                }

                if (r < rows.Count - 1)
                {
                    var downRow = rows[r + 1];
                    nav.selectOnDown = downRow[Mathf.Min(c, downRow.Count - 1)];
                }
                else
                {
                    nav.selectOnDown = (c == 0 && row.Count > 1 && _refreshButton != null)
                        ? _refreshButton
                        : primaryTopRight;
                }

                nav.selectOnLeft = c > 0 ? row[c - 1] : primaryTopLeft;
                nav.selectOnRight = c < row.Count - 1 ? row[c + 1] : primaryTopRight;
                row[c].navigation = nav;
            }
        }

        if (_refreshButton != null)
        {
            _refreshButton.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = rows.Count > 0 ? rows[rows.Count - 1][0] : _createRoomButton,
                selectOnDown = rows.Count > 0 ? rows[0][0] : _createRoomButton,
                selectOnLeft = _createRoomButton,
                selectOnRight = _createRoomButton
            };
        }

        if (_createRoomButton != null)
        {
            _createRoomButton.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = rows.Count > 0 ? rows[rows.Count - 1][Mathf.Min(1, rows[rows.Count - 1].Count - 1)] : _refreshButton,
                selectOnDown = rows.Count > 0 ? rows[0][Mathf.Min(1, rows[0].Count - 1)] : _refreshButton,
                selectOnLeft = _refreshButton,
                selectOnRight = _refreshButton
            };
        }
    }

    private List<Button> RentGridRow()
    {
        for (int i = 0; i < _gridRowPool.Count; i++)
        {
            if (!_gridRowsBuffer.Contains(_gridRowPool[i]))
            {
                _gridRowPool[i].Clear();
                return _gridRowPool[i];
            }
        }
        var row = new List<Button>();
        _gridRowPool.Add(row);
        return row;
    }

    private List<List<Button>> BuildCardButtonGrid(List<RoomCardUI> cards)
    {
        _gridRowsBuffer.Clear();
        if (cards.Count == 0) return _gridRowsBuffer;

        _cardRectBuffer.Clear();
        bool hasLayoutPositions = false;
        Vector2 firstPos = Vector2.zero;

        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i].TryGetComponent<RectTransform>(out var rect))
            {
                Vector2 pos = rect.anchoredPosition;
                if (_cardRectBuffer.Count == 0)
                {
                    firstPos = pos;
                }
                else if ((pos - firstPos).sqrMagnitude > 1f)
                {
                    hasLayoutPositions = true;
                }
                _cardRectBuffer.Add((cards[i].JoinButton, pos));
            }
        }

        if (hasLayoutPositions)
        {
            // Y座標降順 → X座標昇順でインプレースソート（LINQクロージャと中間リストを排除）
            _cardRectBuffer.Sort(static (a, b) =>
            {
                if (Mathf.Abs(a.Pos.y - b.Pos.y) > SameRowYThreshold)
                {
                    return b.Pos.y.CompareTo(a.Pos.y);
                }
                return a.Pos.x.CompareTo(b.Pos.x);
            });

            float currentRowY = _cardRectBuffer[0].Pos.y;
            var currentRow = RentGridRow();
            _gridRowsBuffer.Add(currentRow);

            for (int i = 0; i < _cardRectBuffer.Count; i++)
            {
                var item = _cardRectBuffer[i];
                if (Mathf.Abs(item.Pos.y - currentRowY) > SameRowYThreshold)
                {
                    currentRow = RentGridRow();
                    _gridRowsBuffer.Add(currentRow);
                    currentRowY = item.Pos.y;
                }
                currentRow.Add(item.Button);
            }

            return _gridRowsBuffer;
        }

        int columns = EstimateGridColumns();
        for (int i = 0; i < cards.Count; i += columns)
        {
            var row = RentGridRow();
            for (int j = 0; j < columns && i + j < cards.Count; j++)
            {
                row.Add(cards[i + j].JoinButton);
            }
            _gridRowsBuffer.Add(row);
        }

        return _gridRowsBuffer;
    }

    private int EstimateGridColumns()
    {
        if (_roomListContainer == null || !_roomListContainer.TryGetComponent<GridLayoutGroup>(out var grid))
        {
            return 1;
        }

        if (grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount && grid.constraintCount > 0)
        {
            return grid.constraintCount;
        }

        var containerRect = _roomListContainer as RectTransform;
        float width = containerRect != null ? containerRect.rect.width : 0f;
        if (width <= 1f && _roomListContainer.parent is RectTransform parentRect)
        {
            width = parentRect.rect.width;
        }

        float available = width - grid.padding.left - grid.padding.right;
        float step = grid.cellSize.x + grid.spacing.x;
        if (available <= 0f || step <= 0f) return 1;

        return Mathf.Max(1, Mathf.FloorToInt((available + grid.spacing.x) / step));
    }

    private void HandleCardSelected(ISessionInfo session, RoomCardUI card)
    {
        // ホバーやカーソル移動のたびにナビゲーション全再構築を行う無駄な処理を廃止し、表示更新のみ行う
        _selectedCard = card;
        UpdateSelectedCardVisuals();
        ShowRoomDetail(session);
    }

    private void UpdateSelectedCardVisuals()
    {
        for (int i = 0; i < _spawnedCards.Count; i++)
        {
            var spawnedCard = _spawnedCards[i];
            if (spawnedCard != null)
            {
                spawnedCard.SetSelected(spawnedCard == _selectedCard);
            }
        }
    }

    private void HandleCardSubmitted(ISessionInfo session, RoomCardUI card)
    {
        HandleCardSelected(session, card);

        if (SessionPlayerPropertyHelper.CanJoinSession(session))
        {
            OnJoinRoomRequested?.Invoke(session);
        }
    }

    private void HandleJoinRequest(ISessionInfo session)
    {
        if (SessionPlayerPropertyHelper.CanJoinSession(session))
        {
            OnJoinRoomRequested?.Invoke(session);
        }
    }

    private void ShowRoomDetail(ISessionInfo session)
    {
        _selectedSession = session;
        if (session == null)
        {
            ClearRoomDetail();
            return;
        }

        if (_detailRoomNameText != null)
        {
            _detailRoomNameText.text = session.Name;
        }

        if (_detailPlayerCountText != null)
        {
            int currentPlayerCount = SessionPlayerPropertyHelper.GetCurrentPlayerCount(session);
            _detailPlayerCountText.text = $"{currentPlayerCount} / {session.MaxPlayers}";
        }

        if (_detailGameStatusText != null)
        {
            bool inGame = SessionPlayerPropertyHelper.IsSessionInGame(session);
            _detailGameStatusText.text = SessionPlayerPropertyHelper.GetSessionGameStatusDisplayText(session);
            _detailGameStatusText.color = inGame ? InGameDetailColor : WaitingDetailColor;
        }

        if (_detailProtocolText != null)
        {
            var protocol = SessionPlayerPropertyHelper.GetSessionRelayProtocol(session);
            _detailProtocolText.text = protocol.ToString();
        }
    }

    private void ClearRoomDetail()
    {
        _selectedSession = null;
        _selectedCard = null;
        UpdateSelectedCardVisuals();

        if (_emptyStateObject != null && _spawnedCards.Count == 0)
        {
            _emptyStateObject.SetActive(true);
        }

        if (_detailRoomNameText != null) _detailRoomNameText.text = "-";
        if (_detailPlayerCountText != null) _detailPlayerCountText.text = "- / -";
        if (_detailGameStatusText != null)
        {
            _detailGameStatusText.text = "-";
            _detailGameStatusText.color = EmptyDetailColor;
        }
        if (_detailProtocolText != null) _detailProtocolText.text = "-";
    }

    private void EnsureRoomMenuRoot()
    {
        if (_roomMenuRoot != null) return;
        if (transform.parent != null && transform.parent.name == "RoomMenu")
        {
            _roomMenuRoot = transform.parent.gameObject;
        }
    }

    private void ResolveDetailPanelReferences()
    {
        EnsureRoomMenuRoot();

        if (_roomDetailPanel == null)
        {
            _roomDetailPanel = FindDescendantTransform(transform, "RoomDetailPanel")?.gameObject;
            if (_roomDetailPanel == null && _roomMenuRoot != null)
            {
                _roomDetailPanel = FindDescendantTransform(_roomMenuRoot.transform, "RoomDetailPanel")?.gameObject;
            }
            if (_roomDetailPanel == null && transform.parent != null)
            {
                _roomDetailPanel = FindDescendantTransform(transform.parent, "RoomDetailPanel")?.gameObject;
            }
        }

        Transform detailRoot = _roomDetailPanel != null ? _roomDetailPanel.transform : transform;
        if (_detailRoomNameText == null) _detailRoomNameText = FindDescendantComponent<TMP_Text>(detailRoot, "TMP_RoomName");
        if (_detailPlayerCountText == null) _detailPlayerCountText = FindDescendantComponent<TMP_Text>(detailRoot, "TMP_NumberOfPlayers");
        if (_detailGameStatusText == null) _detailGameStatusText = FindDescendantComponent<TMP_Text>(detailRoot, "TMP_InGameStatus");
        if (_detailProtocolText == null) _detailProtocolText = FindDescendantComponent<TMP_Text>(detailRoot, "TMP_UsedProtocol");

        if (_detailRoomNameText != null)
        {
            _detailRoomNameText.richText = false;
        }
    }

    private static T FindDescendantComponent<T>(Transform root, string targetName) where T : Component
    {
        var target = FindDescendantTransform(root, targetName);
        return target != null ? target.GetComponent<T>() : null;
    }

    private static Transform FindDescendantTransform(Transform root, string targetName)
    {
        if (root == null) return null;
        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            if (child.name == targetName) return child;

            var found = FindDescendantTransform(child, targetName);
            if (found != null) return found;
        }
        return null;
    }

    private void ClearRoomList()
    {
        for (int i = 0; i < _spawnedCards.Count; i++)
        {
            var card = _spawnedCards[i];
            if (card == null) continue;
            card.OnCardSelected -= HandleCardSelected;
            card.OnCardSubmitted -= HandleCardSubmitted;
            card.OnJoinRequested -= HandleJoinRequest;
            Destroy(card.gameObject);
        }
        _spawnedCards.Clear();
        _selectedCard = null;
    }
}
