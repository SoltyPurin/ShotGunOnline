using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// ロビー画面左端からスライド展開するハンバーガーメニュー（サイドドロワー）。
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
    [SerializeField] private Image _playerPortraitImage;
    [SerializeField] private Sprite[] _skinPortraits;

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
        if (_drawerPanel != null && _drawerCanvasGroup == null && !_drawerPanel.TryGetComponent(out _drawerCanvasGroup))
        {
            _drawerCanvasGroup = _drawerPanel.gameObject.AddComponent<CanvasGroup>();
        }

        if (_dimmerButton != null && _dimmerCanvasGroup == null && !_dimmerButton.TryGetComponent(out _dimmerCanvasGroup))
        {
            _dimmerCanvasGroup = _dimmerButton.gameObject.AddComponent<CanvasGroup>();
        }

        if (_playerNameText != null)
        {
            _playerNameText.richText = false;
        }

        if (_matchingButton != null)
        {
            _matchingButton.onClick.AddListener(() => OnMatchingMenuRequested?.Invoke());
            UIButtonSound.ConfigureButton(_matchingButton, UIButtonType.Default);
        }

        if (_editNameButton != null)
        {
            _editNameButton.onClick.AddListener(() => OnEditNameRequested?.Invoke());
            UIButtonSound.ConfigureButton(_editNameButton, UIButtonType.SubAction);
        }

        if (_returnToTitleButton != null)
        {
            _returnToTitleButton.onClick.AddListener(() => OnReturnToTitleRequested?.Invoke());
            UIButtonSound.ConfigureButton(_returnToTitleButton, UIButtonType.Cancel);
        }

        if (_dimmerButton != null)
        {
            _dimmerButton.onClick.AddListener(() => OnCloseRequested?.Invoke());
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(() => OnCloseRequested?.Invoke());
            UIButtonSound.EnsureAttached(_closeButton, UIButtonType.Cancel);
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

    public void Open(bool immediate = false) => SetOpen(true, immediate);
    public void Close(bool immediate = false) => SetOpen(false, immediate);

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
            SetCanvasGroupState(_drawerCanvasGroup, open ? 1f : 0f, open);
            SetCanvasGroupState(_dimmerCanvasGroup, open ? 1f : 0f, open);
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
            SetCanvasGroupState(_drawerCanvasGroup, 0f, interactable: false);
            SetCanvasGroupState(_dimmerCanvasGroup, 0f, interactable: false);

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
                SetInteractable(true);
                FocusDefaultButton();
            });
        }
        else
        {
            SetInteractable(false);

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
            _animSequence.OnComplete(() => gameObject.SetActive(false));
        }
    }

    private static void SetCanvasGroupState(CanvasGroup group, float alpha, bool interactable)
    {
        if (group == null) return;
        group.alpha = alpha;
        group.interactable = interactable;
        group.blocksRaycasts = interactable;
    }

    public void SetInteractable(bool interactable)
    {
        bool canInteract = interactable && _isOpen;
        if (_drawerCanvasGroup != null)
        {
            _drawerCanvasGroup.interactable = canInteract;
            _drawerCanvasGroup.blocksRaycasts = canInteract;
        }
        if (_dimmerCanvasGroup != null)
        {
            _dimmerCanvasGroup.interactable = canInteract;
            _dimmerCanvasGroup.blocksRaycasts = canInteract;
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

    public void RefreshState(bool isOnline, bool isInRoom, int currentPlayers, int maxPlayers, int skinId = 0)
    {
        RefreshPlayerName(PlayerProfileStore.PlayerName);
        RefreshPlayerSkin(isInRoom ? skinId : 0);

        (string statusLabel, Color statusColor) = isInRoom
            ? ("ルーム参加中", _inRoomColor)
            : (isOnline ? ("オンライン", _onlineColor) : ("オフライン", _offlineColor));

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

    public void RefreshPlayerSkin(int skinId)
    {
        if (_playerPortraitImage == null) return;
        Sprite portrait = LobbyUIInputHelper.SelectCyclicSprite(_skinPortraits, skinId);
        if (portrait != null)
        {
            _playerPortraitImage.sprite = portrait;
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
        var buttons = new List<Button>(3);
        if (_matchingButton != null) buttons.Add(_matchingButton);
        if (_editNameButton != null) buttons.Add(_editNameButton);
        if (_returnToTitleButton != null) buttons.Add(_returnToTitleButton);

        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = buttons[(i - 1 + buttons.Count) % buttons.Count],
                selectOnDown = buttons[(i + 1) % buttons.Count]
            };
        }
    }

    private void CaptureOpenPositionIfNeeded()
    {
        if (_hasCapturedOpenPos || _drawerPanel == null) return;
        _openAnchoredPosition = _drawerPanel.anchoredPosition;
        _hasCapturedOpenPos = true;
    }
}
