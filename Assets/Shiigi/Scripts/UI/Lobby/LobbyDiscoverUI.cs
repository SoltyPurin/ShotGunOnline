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
            UIButtonMetallicVisual.Attach(_refreshButton);
        }
        if (_createRoomButton != null)
        {
            _createRoomButton.onClick.AddListener(() => OnCreateRoomRequested?.Invoke());
            EnsureButtonSound(_createRoomButton, UIButtonType.Default);
            UIButtonMetallicVisual.Attach(_createRoomButton);
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

    protected override void OnShowCompleted()
    {
        RebuildControllerNavigation();
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

        if (_roomListContainer is RectTransform containerRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
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
            .Where(c => c != null && c.JoinButton != null && c.JoinButton.gameObject.activeInHierarchy && c.JoinButton.interactable)
            .Select(c => c.JoinButton)
            .FirstOrDefault();

        if (firstCardBtn != null) return firstCardBtn.gameObject;
        if (_createRoomButton != null && _createRoomButton.gameObject.activeInHierarchy && _createRoomButton.interactable) return _createRoomButton.gameObject;
        if (_refreshButton != null && _refreshButton.gameObject.activeInHierarchy && _refreshButton.interactable) return _refreshButton.gameObject;
        return null;
    }

    protected override void OnActiveInteractableChanged(bool interactable)
    {
        if (_refreshButton != null) _refreshButton.interactable = interactable;
        if (_createRoomButton != null) _createRoomButton.interactable = interactable;
        if (interactable)
        {
            RebuildControllerNavigation();
        }
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
        var activeCards = _spawnedCards
            .Where(c => c != null && c.JoinButton != null && c.JoinButton.interactable)
            .ToList();

        List<List<Button>> rows = BuildCardButtonGrid(activeCards);
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
            var nav = new Navigation { mode = Navigation.Mode.Explicit };
            nav.selectOnUp = rows.Count > 0 ? rows[rows.Count - 1][0] : _createRoomButton;
            nav.selectOnDown = rows.Count > 0 ? rows[0][0] : _createRoomButton;
            nav.selectOnLeft = _createRoomButton;
            nav.selectOnRight = _createRoomButton;
            _refreshButton.navigation = nav;
        }

        if (_createRoomButton != null)
        {
            var nav = new Navigation { mode = Navigation.Mode.Explicit };
            nav.selectOnUp = rows.Count > 0 ? rows[rows.Count - 1][Mathf.Min(1, rows[rows.Count - 1].Count - 1)] : _refreshButton;
            nav.selectOnDown = rows.Count > 0 ? rows[0][Mathf.Min(1, rows[0].Count - 1)] : _refreshButton;
            nav.selectOnLeft = _refreshButton;
            nav.selectOnRight = _refreshButton;
            _createRoomButton.navigation = nav;
        }
    }

    private List<List<Button>> BuildCardButtonGrid(List<RoomCardUI> cards)
    {
        var rows = new List<List<Button>>();
        if (cards.Count == 0) return rows;

        // レイアウト計算済みの座標から行・列を構築（未計算時はGridLayoutGroupから列数を算出）
        var cardRects = cards
            .Select(c => new { Button = c.JoinButton, Rect = c.GetComponent<RectTransform>() })
            .Where(x => x.Rect != null)
            .ToList();

        bool hasLayoutPositions = cardRects.Count > 1 &&
            cardRects.Any(x => (x.Rect.anchoredPosition - cardRects[0].Rect.anchoredPosition).sqrMagnitude > 1f);

        if (hasLayoutPositions)
        {
            // Y座標が大きい（上にある）順に並べ、誤差10px以内を同じ行として扱う
            var sorted = cardRects
                .OrderByDescending(x => x.Rect.anchoredPosition.y)
                .ThenBy(x => x.Rect.anchoredPosition.x)
                .ToList();

            float currentRowY = sorted[0].Rect.anchoredPosition.y;
            var currentRow = new List<(Button Button, float X)>();

            foreach (var item in sorted)
            {
                if (Mathf.Abs(item.Rect.anchoredPosition.y - currentRowY) > 10f)
                {
                    rows.Add(currentRow.OrderBy(x => x.X).Select(x => x.Button).ToList());
                    currentRow.Clear();
                    currentRowY = item.Rect.anchoredPosition.y;
                }
                currentRow.Add((item.Button, item.Rect.anchoredPosition.x));
            }

            if (currentRow.Count > 0)
            {
                rows.Add(currentRow.OrderBy(x => x.X).Select(x => x.Button).ToList());
            }

            return rows;
        }

        int columns = EstimateGridColumns();
        for (int i = 0; i < cards.Count; i += columns)
        {
            var row = new List<Button>();
            for (int j = 0; j < columns && i + j < cards.Count; j++)
            {
                row.Add(cards[i + j].JoinButton);
            }
            rows.Add(row);
        }

        return rows;
    }

    private int EstimateGridColumns()
    {
        if (_roomListContainer == null) return 1;
        var grid = _roomListContainer.GetComponent<GridLayoutGroup>();
        if (grid == null) return 1;

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
