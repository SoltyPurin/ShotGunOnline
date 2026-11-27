using System;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RoomCardUI : MonoBehaviour, ISelectHandler, IPointerEnterHandler
{
    [Header("テキスト")]
    [SerializeField] private Text _roomNameText;
    [SerializeField] private Text _playerCountText;
    [SerializeField] private Text _statusText;
    [SerializeField] private Graphic _statusIndicator;

    [Header("ボタン")]
    [SerializeField] private Button _joinButton;
    [SerializeField] private Button _cardButton;

    [Header("選択状態の見た目")]
    [SerializeField] private Color _selectedBgColor = new Color(0.28f, 0.21f, 0.11f, 0.98f);
    [SerializeField] private Color _selectedOutlineColor = new Color(0.98f, 0.83f, 0.43f, 1.0f);
    [SerializeField] private Vector2 _selectedOutlineDistance = new Vector2(2.5f, -2.5f);
    [SerializeField] private Color _selectedAccentColor = new Color(0.36f, 0.92f, 0.80f, 1.0f);
    [SerializeField] private Color _selectedTextColor = new Color(1.0f, 0.98f, 0.90f, 1.0f);
    [SerializeField] private float _accentBarWidth = 7f;

    [Header("ステータス色")]
    [SerializeField] private Color _waitingStatusColor = new Color(0.36f, 0.92f, 0.80f, 1f);
    [SerializeField] private Color _inGameStatusColor = new Color(0.92f, 0.48f, 0.38f, 1f);
    [SerializeField] private Color _fullStatusColor = new Color(0.76f, 0.66f, 0.48f, 1f);

    private ISessionInfo _sessionInfo;
    private Image _backgroundImage;
    private Outline _cardOutline;
    private Image _selectedAccentBar;
    private UIButtonMetallicVisual _cardVisual;

    private Color _normalBgColor = new Color(0.18f, 0.14f, 0.09f, 0.94f);
    private Color _normalOutlineColor = new Color(0.82f, 0.64f, 0.22f, 0.6f);
    private Vector2 _normalOutlineDistance = new Vector2(2f, -2f);
    private Color _normalTextColor = new Color(0.98f, 0.94f, 0.84f, 1f);
    private bool _isSelected;
    private bool _visualInitialized;

    public event Action<ISessionInfo> OnJoinRequested;
    public event Action<ISessionInfo, RoomCardUI> OnCardSelected;
    public event Action<ISessionInfo, RoomCardUI> OnCardSubmitted;

    public ISessionInfo SessionInfo => _sessionInfo;
    public Button JoinButton => NavigationButton;
    public Button NavigationButton => (_joinButton != null && _joinButton.gameObject.activeInHierarchy) ? _joinButton : _cardButton;

    private void Awake()
    {
        if (_roomNameText != null)
        {
            _roomNameText.supportRichText = false;
            _normalTextColor = _roomNameText.color;
        }

        CacheVisualComponents();
        EnsureCardButton();

        if (_cardButton != null)
        {
            _cardButton.onClick.AddListener(HandleCardClicked);
            _cardVisual = UIButtonMetallicVisual.Attach(_cardButton);
            _cardVisual?.SetUseOutline(false);
            UIButtonSound.EnsureAttached(_cardButton, UIButtonType.Default);
        }

        if (_joinButton != null)
        {
            _joinButton.onClick.AddListener(() => OnJoinRequested?.Invoke(_sessionInfo));
            UIButtonSound.ConfigureButton(_joinButton, UIButtonType.Default);
        }

        ApplySelectedVisual(false);
    }

    private void CacheVisualComponents()
    {
        _backgroundImage = GetComponent<Image>();
        if (_backgroundImage != null)
        {
            _normalBgColor = _backgroundImage.color;
        }

        _cardOutline = GetComponent<Outline>();
        if (_cardOutline != null)
        {
            _normalOutlineColor = _cardOutline.effectColor;
            _normalOutlineDistance = _cardOutline.effectDistance;
        }

        EnsureSelectedAccentBar();
    }

    private void EnsureSelectedAccentBar()
    {
        if (_selectedAccentBar != null) return;

        Transform existing = transform.Find("SelectedAccentBar");
        if (existing != null)
        {
            _selectedAccentBar = existing.GetComponent<Image>();
        }

        if (_selectedAccentBar == null)
        {
            var barObj = new GameObject("SelectedAccentBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            barObj.layer = gameObject.layer;

            var rect = barObj.GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(_accentBarWidth, 0f);

            _selectedAccentBar = barObj.GetComponent<Image>();
            _selectedAccentBar.raycastTarget = false;
        }

        _selectedAccentBar.color = _selectedAccentColor;
        _selectedAccentBar.gameObject.SetActive(false);
    }

    private void EnsureCardButton()
    {
        if (_cardButton != null) return;

        if (_joinButton == null || !_joinButton.gameObject.activeSelf)
        {
            if (!TryGetComponent(out _cardButton))
            {
                _cardButton = gameObject.AddComponent<Button>();
                var colors = _cardButton.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = Color.white;
                colors.selectedColor = Color.white;
                colors.pressedColor = new Color(0.84f, 0.84f, 0.84f, 1f);
                colors.fadeDuration = 0.08f;
                _cardButton.colors = colors;
            }
            if (_cardButton.targetGraphic == null)
            {
                _cardButton.targetGraphic = GetComponent<Graphic>();
            }
        }
    }

    public void SetSelected(bool selected)
    {
        if (_visualInitialized && _isSelected == selected) return;
        ApplySelectedVisual(selected);
    }

    private void ApplySelectedVisual(bool selected)
    {
        bool changedToSelected = selected && !_isSelected;
        _isSelected = selected;
        _visualInitialized = true;

        if (_backgroundImage != null)
        {
            _backgroundImage.color = selected ? _selectedBgColor : _normalBgColor;
        }

        if (_cardOutline != null)
        {
            _cardOutline.effectColor = selected ? _selectedOutlineColor : _normalOutlineColor;
            _cardOutline.effectDistance = selected ? _selectedOutlineDistance : _normalOutlineDistance;
        }

        if (_selectedAccentBar != null)
        {
            _selectedAccentBar.color = _selectedAccentColor;
            _selectedAccentBar.gameObject.SetActive(selected);
        }

        if (_roomNameText != null)
        {
            _roomNameText.color = selected ? _selectedTextColor : _normalTextColor;
        }

        if (changedToSelected && _cardVisual != null)
        {
            _cardVisual.TriggerShine();
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (_sessionInfo != null)
        {
            OnCardSelected?.Invoke(_sessionInfo, this);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_sessionInfo != null)
        {
            OnCardSelected?.Invoke(_sessionInfo, this);
        }
    }

    private void HandleCardClicked()
    {
        if (_sessionInfo == null) return;
        OnCardSelected?.Invoke(_sessionInfo, this);
        OnCardSubmitted?.Invoke(_sessionInfo, this);
    }

    /// <summary>
    /// セッション情報を元にカードの表示を更新する（ドメインルールは SessionPlayerPropertyHelper へ委譲）
    /// </summary>
    public void Setup(ISessionInfo sessionInfo)
    {
        _sessionInfo = sessionInfo;
        if (sessionInfo == null) return;

        if (_roomNameText != null)
        {
            _roomNameText.text = sessionInfo.Name;
        }

        if (_playerCountText != null)
        {
            int currentPlayerCount = SessionPlayerPropertyHelper.GetCurrentPlayerCount(sessionInfo);
            _playerCountText.text = $"{currentPlayerCount} / {sessionInfo.MaxPlayers}";
        }

        var status = SessionPlayerPropertyHelper.GetRoomAvailabilityStatus(sessionInfo);
        string statusLabel = SessionPlayerPropertyHelper.GetRoomStatusDisplayText(status);
        Color statusColor = status switch
        {
            RoomAvailabilityStatus.InGame => _inGameStatusColor,
            RoomAvailabilityStatus.Locked => _inGameStatusColor,
            RoomAvailabilityStatus.Full => _fullStatusColor,
            _ => _waitingStatusColor
        };

        if (_statusText != null)
        {
            _statusText.text = statusLabel;
            _statusText.color = statusColor;
        }
        if (_statusIndicator != null)
        {
            _statusIndicator.color = statusColor;
        }

        if (_joinButton != null && _joinButton.gameObject.activeSelf)
        {
            _joinButton.interactable = SessionPlayerPropertyHelper.CanJoinSession(sessionInfo);
        }
    }
}
