using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// ロビー画面左端からスライド展開するハンバーガーメニュー（サイドドロワー）。
/// プロフィール表示（名前・ステータス）と、マッチングメニュー／名前変更／タイトルへ戻るの各ボタンを集約し、
/// コントローラーの上下ナビゲーションだけで完結して操作できます。
/// </summary>
public class LobbyHamburgerMenuUI : MonoBehaviour
{
    [Header("ドロワー本体")]
    [SerializeField] private RectTransform _drawerPanel;
    [SerializeField] private CanvasGroup _drawerCanvasGroup;
    [SerializeField] private Button _dimmerButton;
    [SerializeField] private CanvasGroup _dimmerCanvasGroup;
    [SerializeField] private Button _closeButton;

    [Header("プロフィール表示")]
    [SerializeField] private TMP_Text _playerNameText;
    [SerializeField] private TMP_Text _statusText;
    [SerializeField] private Graphic _statusIndicator;

    [Header("メニューボタン")]
    [SerializeField] private Button _matchingButton;
    [SerializeField] private TMP_Text _matchingButtonText;
    [SerializeField] private Button _editNameButton;
    [SerializeField] private Button _returnToTitleButton;

    [Header("アニメーション設定")]
    [SerializeField] private float _slideDuration = 0.22f;

    [Header("ステータス色設定")]
    [SerializeField] private Color _onlineColor = new Color(0.36f, 0.92f, 0.80f, 1f);
    [SerializeField] private Color _inRoomColor = new Color(1f, 0.82f, 0.34f, 1f);
    [SerializeField] private Color _offlineColor = new Color(0.66f, 0.62f, 0.56f, 1f);

    public event Action OnMatchingMenuRequested;
    public event Action OnEditNameRequested;
    public event Action OnReturnToTitleRequested;
    public event Action OnCloseRequested;

    public bool IsOpen => _isOpen;

    private bool _isOpen;
    private bool _hasCapturedOpenPos;
    private Vector2 _openAnchoredPosition;
    private Sequence _animSequence;

    private void Awake()
    {
        if (_drawerPanel != null && _drawerCanvasGroup == null)
        {
            _drawerCanvasGroup = _drawerPanel.GetComponent<CanvasGroup>();
            if (_drawerCanvasGroup == null)
            {
                _drawerCanvasGroup = _drawerPanel.gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (_dimmerButton != null && _dimmerCanvasGroup == null)
        {
            _dimmerCanvasGroup = _dimmerButton.GetComponent<CanvasGroup>();
            if (_dimmerCanvasGroup == null)
            {
                _dimmerCanvasGroup = _dimmerButton.gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (_playerNameText != null)
        {
            _playerNameText.richText = false;
        }

        if (_matchingButton != null)
        {
            _matchingButton.onClick.AddListener(() => OnMatchingMenuRequested?.Invoke());
            EnsureButtonSound(_matchingButton, UIButtonType.Default);
            UIButtonMetallicVisual.Attach(_matchingButton);
        }

        if (_editNameButton != null)
        {
            _editNameButton.onClick.AddListener(() => OnEditNameRequested?.Invoke());
            EnsureButtonSound(_editNameButton, UIButtonType.SubAction);
            UIButtonMetallicVisual.Attach(_editNameButton);
        }

        if (_returnToTitleButton != null)
        {
            _returnToTitleButton.onClick.AddListener(() => OnReturnToTitleRequested?.Invoke());
            EnsureButtonSound(_returnToTitleButton, UIButtonType.Cancel);
            UIButtonMetallicVisual.Attach(_returnToTitleButton);
        }

        if (_dimmerButton != null)
        {
            _dimmerButton.onClick.AddListener(() => OnCloseRequested?.Invoke());
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(() => OnCloseRequested?.Invoke());
            EnsureButtonSound(_closeButton, UIButtonType.Cancel);
        }

        CaptureOpenPositionIfNeeded();
        RebuildNavigation();
    }

    private void Start()
    {
        PlayerProfileStore.OnProfileChanged += HandleProfileChanged;
        RefreshPlayerName(PlayerProfileStore.PlayerName);
    }

    private void OnDestroy()
    {
        PlayerProfileStore.OnProfileChanged -= HandleProfileChanged;
        _animSequence?.Kill();
    }

    public void Open(bool immediate = false)
    {
        SetOpen(true, immediate);
    }

    public void Close(bool immediate = false)
    {
        SetOpen(false, immediate);
    }

    public void SetOpen(bool open, bool immediate = false)
    {
        CaptureOpenPositionIfNeeded();
        _isOpen = open;
        _animSequence?.Kill();

        float slideWidth = _drawerPanel != null
            ? Mathf.Max(320f, _drawerPanel.rect.width + Mathf.Max(0f, _openAnchoredPosition.x) + 48f)
            : 480f;
        Vector2 closedPos = _openAnchoredPosition - new Vector2(slideWidth, 0f);

        if (immediate)
        {
            gameObject.SetActive(open);
            if (_drawerPanel != null)
            {
                _drawerPanel.anchoredPosition = open ? _openAnchoredPosition : closedPos;
            }
            if (_drawerCanvasGroup != null)
            {
                _drawerCanvasGroup.alpha = open ? 1f : 0f;
                _drawerCanvasGroup.interactable = open;
                _drawerCanvasGroup.blocksRaycasts = open;
            }
            if (_dimmerCanvasGroup != null)
            {
                _dimmerCanvasGroup.alpha = open ? 1f : 0f;
                _dimmerCanvasGroup.interactable = open;
                _dimmerCanvasGroup.blocksRaycasts = open;
            }
            if (open)
            {
                FocusDefaultButton();
            }
            return;
        }

        _animSequence = DOTween.Sequence().SetUpdate(true);

        if (open)
        {
            gameObject.SetActive(true);

            if (_drawerPanel != null)
            {
                _drawerPanel.anchoredPosition = closedPos;
            }
            if (_drawerCanvasGroup != null)
            {
                _drawerCanvasGroup.alpha = 0f;
                _drawerCanvasGroup.interactable = false;
                _drawerCanvasGroup.blocksRaycasts = false;
            }
            if (_dimmerCanvasGroup != null)
            {
                _dimmerCanvasGroup.alpha = 0f;
                _dimmerCanvasGroup.interactable = false;
                _dimmerCanvasGroup.blocksRaycasts = false;
            }

            if (_drawerPanel != null)
            {
                _animSequence.Join(_drawerPanel.DOAnchorPos(_openAnchoredPosition, _slideDuration).SetEase(Ease.OutCubic));
            }
            if (_drawerCanvasGroup != null)
            {
                _animSequence.Join(_drawerCanvasGroup.DOFade(1f, _slideDuration * 0.85f).SetEase(Ease.OutQuad));
            }
            if (_dimmerCanvasGroup != null)
            {
                _animSequence.Join(_dimmerCanvasGroup.DOFade(1f, _slideDuration).SetEase(Ease.OutQuad));
            }

            _animSequence.OnComplete(() =>
            {
                if (_drawerCanvasGroup != null)
                {
                    _drawerCanvasGroup.interactable = true;
                    _drawerCanvasGroup.blocksRaycasts = true;
                }
                if (_dimmerCanvasGroup != null)
                {
                    _dimmerCanvasGroup.interactable = true;
                    _dimmerCanvasGroup.blocksRaycasts = true;
                }
                FocusDefaultButton();
            });
        }
        else
        {
            if (_drawerCanvasGroup != null)
            {
                _drawerCanvasGroup.interactable = false;
                _drawerCanvasGroup.blocksRaycasts = false;
            }
            if (_dimmerCanvasGroup != null)
            {
                _dimmerCanvasGroup.interactable = false;
                _dimmerCanvasGroup.blocksRaycasts = false;
            }

            float closeDuration = _slideDuration * 0.78f;
            if (_drawerPanel != null)
            {
                _animSequence.Join(_drawerPanel.DOAnchorPos(closedPos, closeDuration).SetEase(Ease.InCubic));
            }
            if (_drawerCanvasGroup != null)
            {
                _animSequence.Join(_drawerCanvasGroup.DOFade(0f, closeDuration).SetEase(Ease.InQuad));
            }
            if (_dimmerCanvasGroup != null)
            {
                _animSequence.Join(_dimmerCanvasGroup.DOFade(0f, closeDuration).SetEase(Ease.InQuad));
            }
            _animSequence.OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
        }
    }

    public void SetInteractable(bool interactable)
    {
        if (_drawerCanvasGroup != null)
        {
            _drawerCanvasGroup.interactable = interactable && _isOpen;
            _drawerCanvasGroup.blocksRaycasts = interactable && _isOpen;
        }
        if (_dimmerCanvasGroup != null)
        {
            _dimmerCanvasGroup.interactable = interactable && _isOpen;
            _dimmerCanvasGroup.blocksRaycasts = interactable && _isOpen;
        }
    }

    public void FocusDefaultButton()
    {
        if (!_isOpen || !gameObject.activeInHierarchy || EventSystem.current == null) return;

        Button target = GetFirstInteractableButton();
        if (target != null)
        {
            EventSystem.current.SetSelectedGameObject(target.gameObject);
        }
    }

    public void FocusEditNameButton()
    {
        if (!_isOpen || !gameObject.activeInHierarchy || EventSystem.current == null) return;
        if (_editNameButton != null && _editNameButton.IsInteractable())
        {
            EventSystem.current.SetSelectedGameObject(_editNameButton.gameObject);
        }
        else
        {
            FocusDefaultButton();
        }
    }

    public void RefreshState(bool isOnline, bool isInRoom, int currentPlayers, int maxPlayers)
    {
        RefreshPlayerName(PlayerProfileStore.PlayerName);

        string statusLabel;
        Color statusColor;
        if (isInRoom)
        {
            statusLabel = "ルーム参加中";
            statusColor = _inRoomColor;
        }
        else if (isOnline)
        {
            statusLabel = "オンライン";
            statusColor = _onlineColor;
        }
        else
        {
            statusLabel = "オフライン";
            statusColor = _offlineColor;
        }

        if (_statusText != null)
        {
            _statusText.text = statusLabel;
            _statusText.color = statusColor;
        }

        if (_statusIndicator != null)
        {
            _statusIndicator.color = statusColor;
        }

        if (_matchingButtonText != null)
        {
            _matchingButtonText.text = isInRoom
                ? $"ルームメニュー ({currentPlayers}/{maxPlayers})"
                : "マッチングメニュー";
        }
    }

    private void RefreshPlayerName(string playerName)
    {
        if (_playerNameText != null)
        {
            _playerNameText.text = playerName ?? string.Empty;
        }
    }

    private void HandleProfileChanged(PlayerProfile profile)
    {
        if (profile != null)
        {
            RefreshPlayerName(profile.PlayerName);
        }
    }

    private Button GetFirstInteractableButton()
    {
        if (_matchingButton != null && _matchingButton.gameObject.activeInHierarchy && _matchingButton.IsInteractable()) return _matchingButton;
        if (_editNameButton != null && _editNameButton.gameObject.activeInHierarchy && _editNameButton.IsInteractable()) return _editNameButton;
        if (_returnToTitleButton != null && _returnToTitleButton.gameObject.activeInHierarchy && _returnToTitleButton.IsInteractable()) return _returnToTitleButton;
        return null;
    }

    private void RebuildNavigation()
    {
        var buttons = new System.Collections.Generic.List<Button>();
        if (_matchingButton != null) buttons.Add(_matchingButton);
        if (_editNameButton != null) buttons.Add(_editNameButton);
        if (_returnToTitleButton != null) buttons.Add(_returnToTitleButton);

        for (int i = 0; i < buttons.Count; i++)
        {
            var nav = new Navigation { mode = Navigation.Mode.Explicit };
            nav.selectOnUp = buttons[(i - 1 + buttons.Count) % buttons.Count];
            nav.selectOnDown = buttons[(i + 1) % buttons.Count];
            buttons[i].navigation = nav;
        }
    }

    private void CaptureOpenPositionIfNeeded()
    {
        if (_hasCapturedOpenPos || _drawerPanel == null) return;
        _openAnchoredPosition = _drawerPanel.anchoredPosition;
        _hasCapturedOpenPos = true;
    }

    private static void EnsureButtonSound(Button button, UIButtonType buttonType)
    {
        if (button == null) return;
        var sound = button.GetComponent<UIButtonSound>();
        if (sound == null)
        {
            sound = button.gameObject.AddComponent<UIButtonSound>();
        }
        sound.ButtonType = buttonType;
    }
}
