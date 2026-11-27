using System;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// ロビー画面左上の常設HUDボタンの構築・表示更新・ハンバーガーメニュー状態同期を行う。
/// </summary>
public sealed class LobbyHudPresenter : IDisposable
{
    private static readonly Color DefaultHudButtonBgColor = new Color(0.12f, 0.14f, 0.18f, 0.88f);

    private readonly Transform _hudRoot;
    private readonly Transform _roomUiTransform;
    private readonly LobbyHamburgerMenuUI _hamburgerMenuUI;
    private readonly GameObject _menuToggleHamburgerIcon;
    private readonly GameObject _menuToggleCloseIcon;
    private readonly UnityAction _onToggleMenuClicked;
    private readonly UnityAction _onEditPlayerNameClicked;

    private Button _menuToggleButton;
    private Text _menuToggleButtonText;
    private Button _playerNameButton;
    private Text _playerNameButtonText;

    public Button MenuToggleButton => _menuToggleButton;
    public Button PlayerNameButton => _playerNameButton;

    public LobbyHudPresenter(
        Transform hudRoot,
        Transform roomUiTransform,
        LobbyHamburgerMenuUI hamburgerMenuUI,
        Button menuToggleButton,
        Text menuToggleButtonText,
        GameObject menuToggleHamburgerIcon,
        GameObject menuToggleCloseIcon,
        Button playerNameButton,
        Text playerNameButtonText,
        UnityAction onToggleMenuClicked,
        UnityAction onEditPlayerNameClicked)
    {
        _hudRoot = hudRoot;
        _roomUiTransform = roomUiTransform;
        _hamburgerMenuUI = hamburgerMenuUI;
        _menuToggleButton = menuToggleButton;
        _menuToggleButtonText = menuToggleButtonText;
        _menuToggleHamburgerIcon = menuToggleHamburgerIcon;
        _menuToggleCloseIcon = menuToggleCloseIcon;
        _playerNameButton = playerNameButton;
        _playerNameButtonText = playerNameButtonText;
        _onToggleMenuClicked = onToggleMenuClicked;
        _onEditPlayerNameClicked = onEditPlayerNameClicked;
    }

    public void Initialize()
    {
        HideLegacyTitleBackdropIfNeeded();
        EnsureToggleHudButtonCreated();

        if (_hamburgerMenuUI == null)
        {
            EnsurePlayerNameButtonCreated();
        }
    }

    public void Dispose()
    {
        if (_menuToggleButton != null && _onToggleMenuClicked != null)
        {
            _menuToggleButton.onClick.RemoveListener(_onToggleMenuClicked);
        }
        if (_playerNameButton != null && _onEditPlayerNameClicked != null)
        {
            _playerNameButton.onClick.RemoveListener(_onEditPlayerNameClicked);
        }
    }

    public void RefreshHudVisuals(
        LobbyUIState currentState,
        bool isMenuOpen,
        bool isHamburgerOpen,
        bool isEditingPlayerName,
        bool isOnline,
        ISession currentSession)
    {
        if (_menuToggleButton != null)
        {
            bool hasCustomIcons = _menuToggleHamburgerIcon != null || _menuToggleCloseIcon != null;
            bool isAnyMenuOpen = isHamburgerOpen || isMenuOpen;
            bool canShowButton = currentState != LobbyUIState.Connecting && (!hasCustomIcons || !isHamburgerOpen);

            _menuToggleButton.gameObject.SetActive(canShowButton);
            _menuToggleButton.interactable = canShowButton && !LobbyViewBase.IsModalOpen;

            if (_menuToggleHamburgerIcon != null)
            {
                _menuToggleHamburgerIcon.SetActive(!isAnyMenuOpen);
            }
            if (_menuToggleCloseIcon != null)
            {
                _menuToggleCloseIcon.SetActive(isAnyMenuOpen);
            }

            if (_menuToggleButtonText != null)
            {
                _menuToggleButtonText.text = FormatToggleButtonLabel(
                    hasCustomIcons,
                    _hamburgerMenuUI != null,
                    isMenuOpen,
                    isHamburgerOpen,
                    currentState,
                    currentSession);
            }
        }

        RefreshProfileAndHamburgerState(currentState, isEditingPlayerName, isOnline, currentSession);
    }

    public void RefreshProfileAndHamburgerState(
        LobbyUIState currentState,
        bool isEditingPlayerName,
        bool isOnline,
        ISession currentSession)
    {
        bool canShow = currentState != LobbyUIState.Connecting;
        bool canInteract = canShow && !LobbyViewBase.IsModalOpen && !isEditingPlayerName;
        bool isInRoom = currentState == LobbyUIState.InRoom && currentSession != null;

        if (_hamburgerMenuUI != null)
        {
            _hamburgerMenuUI.SetInteractable(canInteract);
            _hamburgerMenuUI.RefreshState(
                isOnline,
                isInRoom,
                currentSession != null ? currentSession.Players.Count : 0,
                currentSession != null ? currentSession.MaxPlayers : 0,
                isInRoom ? PlayerSkinSync.LocalPlayerSkinId : 0);
        }

        if (_playerNameButton == null) return;

        bool showFallbackButton = canShow && _hamburgerMenuUI == null;
        _playerNameButton.gameObject.SetActive(showFallbackButton);
        _playerNameButton.interactable = showFallbackButton && canInteract;

        if (_playerNameButtonText != null)
        {
            _playerNameButtonText.supportRichText = false;
            _playerNameButtonText.text = $"名前: {PlayerProfileStore.PlayerName}";
        }
    }

    private static string FormatToggleButtonLabel(
        bool hasCustomIcons,
        bool hasHamburgerMenu,
        bool isMenuOpen,
        bool isHamburgerOpen,
        LobbyUIState state,
        ISession session)
    {
        bool isAnyMenuOpen = isHamburgerOpen || isMenuOpen;
        bool hasRoomCount = state == LobbyUIState.InRoom && session != null;

        if (hasCustomIcons)
        {
            if (isAnyMenuOpen) return "ステージへ戻る";
            return hasRoomCount ? $"メニュー ({session.Players.Count}/{session.MaxPlayers})" : "メニュー";
        }

        if (hasHamburgerMenu)
        {
            if (isAnyMenuOpen) return "× ステージへ戻る [Tab / Esc / Option]";
            return hasRoomCount
                ? $"≡ メニュー ({session.Players.Count}/{session.MaxPlayers}) [Tab / Esc / Option]"
                : "≡ メニュー [Tab / Esc / Option]";
        }

        if (isMenuOpen) return "× ステージへ戻る [Tab / Esc / Option]";
        return hasRoomCount
            ? $"ルームメニュー ({session.Players.Count}/{session.MaxPlayers}) [Tab / Esc / Option]"
            : "マッチングメニューを開く [Tab / Esc / Option]";
    }

    private void EnsureToggleHudButtonCreated()
    {
        if (_menuToggleButton == null && _hudRoot != null)
        {
            Transform existing = _hudRoot.Find("Button_ToggleLobbyMenu");
            if (existing != null)
            {
                _menuToggleButton = existing.GetComponent<Button>();
                _menuToggleButtonText = existing.GetComponentInChildren<Text>(true);
            }
        }

        if (_menuToggleButton == null && _hudRoot != null)
        {
            int siblingIndex = _roomUiTransform != null ? _roomUiTransform.GetSiblingIndex() + 1 : -1;
            (_menuToggleButton, _menuToggleButtonText) = CreateFallbackHudButton(
                "Button_ToggleLobbyMenu",
                anchoredPosition: new Vector2(32f, -24f),
                sizeDelta: new Vector2(460f, 64f),
                fontSize: 22,
                siblingIndex: siblingIndex);
        }

        if (_menuToggleButton != null)
        {
            if (_onToggleMenuClicked != null)
            {
                _menuToggleButton.onClick.RemoveListener(_onToggleMenuClicked);
                _menuToggleButton.onClick.AddListener(_onToggleMenuClicked);
            }
            UIButtonMetallicVisual.Attach(_menuToggleButton);
        }
    }

    private void EnsurePlayerNameButtonCreated()
    {
        if (_playerNameButton == null && _hudRoot != null)
        {
            Transform existing = _hudRoot.Find("Button_EditPlayerName");
            if (existing != null)
            {
                _playerNameButton = existing.GetComponent<Button>();
                _playerNameButtonText = existing.GetComponentInChildren<Text>(true);
            }
        }

        if (_playerNameButton == null && _hudRoot != null)
        {
            int siblingIndex = _menuToggleButton != null ? _menuToggleButton.transform.GetSiblingIndex() + 1 : -1;
            (_playerNameButton, _playerNameButtonText) = CreateFallbackHudButton(
                "Button_EditPlayerName",
                anchoredPosition: new Vector2(32f, -96f),
                sizeDelta: new Vector2(460f, 52f),
                fontSize: 20,
                siblingIndex: siblingIndex);
            _playerNameButtonText.supportRichText = false;
        }

        if (_playerNameButton != null)
        {
            if (_onEditPlayerNameClicked != null)
            {
                _playerNameButton.onClick.RemoveListener(_onEditPlayerNameClicked);
                _playerNameButton.onClick.AddListener(_onEditPlayerNameClicked);
            }
            UIButtonMetallicVisual.Attach(_playerNameButton);
        }
    }

    private (Button Button, Text Label) CreateFallbackHudButton(
        string objectName,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        int fontSize,
        int siblingIndex)
    {
        GameObject btnGo = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(_hudRoot, false);
        if (siblingIndex >= 0)
        {
            btnGo.transform.SetSiblingIndex(siblingIndex);
        }

        RectTransform rt = btnGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;

        Image img = btnGo.GetComponent<Image>();
        img.color = DefaultHudButtonBgColor;

        Button btn = btnGo.GetComponent<Button>();
        btn.targetGraphic = img;

        GameObject txtGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        txtGo.transform.SetParent(btnGo.transform, false);

        RectTransform txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(12f, 4f);
        txtRt.offsetMax = new Vector2(-12f, -4f);

        Text label = txtGo.GetComponent<Text>();
        Font referenceFont = _menuToggleButtonText != null
            ? _menuToggleButtonText.font
            : _hudRoot.GetComponentInChildren<Text>(true)?.font;
        if (referenceFont == null)
        {
            referenceFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        label.font = referenceFont;
        label.fontSize = fontSize;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;

        return (btn, label);
    }

    /// <summary>
    /// タイトルシーンから流用された ImageShadow（巨大立ち絵）や上下黒帯が集会所ステージを隠さないよう非表示にする
    /// </summary>
    private void HideLegacyTitleBackdropIfNeeded()
    {
        Canvas rootCanvas = _hudRoot != null ? _hudRoot.GetComponentInParent<Canvas>()?.rootCanvas : null;
        RectTransform[] rects = rootCanvas != null
            ? rootCanvas.GetComponentsInChildren<RectTransform>(true)
            : UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include);

        for (int i = 0; i < rects.Length; i++)
        {
            var rt = rects[i];
            if (rt == null) continue;
            if (rt.name == "ImageShadow" || (rt.name == "Black" && rt.parent != null && rt.parent.name == "Canvas"))
            {
                rt.gameObject.SetActive(false);
            }
        }
    }
}
