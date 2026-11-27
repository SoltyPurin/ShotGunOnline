using Cysharp.Threading.Tasks;
using System;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// ロビー画面の画面状態遷移とルームセッション操作を統括する。
/// </summary>
public class LobbyUIManager : MonoBehaviour
{
    [SerializeField] private LobbyDiscoverUI _discoverUI;
    [SerializeField] private CreateRoomDialogUI _createRoomDialogUI;
    [SerializeField] private ConfirmDialogUI _confirmDialogUI;
    [SerializeField] private LobbyRoomUI _roomUI;

    [Header("ローディング表示用オブジェクト")]
    [SerializeField] private LoadingOverlayController _loadingOverlay;

    [Header("集会所メニュー開閉設定")]
    [Tooltip("シーン開始時にロビーUIを開いた状態にするか（false の場合は集会所ステージから開始）")]
    [SerializeField] private bool _startWithMenuOpen;
    [Tooltip("ハンバーガーメニュー（設定時はOption/Tab/Escでこちらを開き、そこから各機能へ遷移）")]
    [SerializeField] private LobbyHamburgerMenuUI _hamburgerMenuUI;
    [Tooltip("画面左上のメニュー開閉ボタン（未設定時は自動生成）")]
    [SerializeField] private Button _menuToggleButton;
    [SerializeField] private Text _menuToggleButtonText;
    [SerializeField] private GameObject _menuToggleHamburgerIcon;
    [SerializeField] private GameObject _menuToggleCloseIcon;
    [SerializeField] private string _titleSceneName = "Title";

    [Header("プレイヤー名変更設定")]
    [Tooltip("プレイヤー名変更ボタン（ハンバーガーメニュー未設定時のみ自動生成）")]
    [SerializeField] private Button _playerNameButton;
    [SerializeField] private Text _playerNameButtonText;
    [SerializeField] private LobbyVirtualKeyboardUI _virtualKeyboardPrefab;
    [SerializeField] private LobbyTextInputDialogUI _textInputDialogPrefab;

    private ILobbySessionService _sessionService;
    private MultiplayerSessionManager _sessionManager;
    private LobbyHudPresenter _hudPresenter;

    private LobbyUIState _currentState;
    private bool _isMenuOpen;
    private bool _isHamburgerOpen;
    private bool _hasRefreshedOnce;
    private bool _isBusyOperation;
    private bool _isUpdatingReady;
    private bool _isEditingPlayerName;
    private bool _isStartingGame;

    public bool IsMenuOpen => _isMenuOpen || _isHamburgerOpen;

    /// <summary>
    /// 外部から ILobbySessionService を注入する
    /// </summary>
    public void InjectSessionService(ILobbySessionService sessionService)
    {
        if (_sessionService != null)
        {
            _sessionService.OnSessionChanged -= UpdateRoomUI;
        }
        _sessionService = sessionService;
        if (isActiveAndEnabled && _sessionService != null)
        {
            _sessionService.OnSessionChanged += UpdateRoomUI;
        }
    }

    private void Start()
    {
        _sessionManager = MultiplayerSessionManager.Instance;
        _sessionService ??= _sessionManager;

        if (_discoverUI != null)
        {
            _discoverUI.OnRefreshRequested += HandleRefreshRoomListRequested;
            _discoverUI.OnCreateRoomRequested += HandleOpenCreateRoomDialogRequested;
            _discoverUI.OnBackRequested += HandleMenuBackRequested;
            _discoverUI.OnJoinRoomRequested += HandleJoinRoomRequested;
        }

        if (_roomUI != null)
        {
            _roomUI.OnLeaveRequested += HandleLeaveRoomRequested;
            _roomUI.OnReadyToggleRequested += HandleToggleReadyRequested;
            _roomUI.OnStartGameRequested += StartGame;
        }

        if (_sessionService != null)
        {
            _sessionService.OnSessionChanged -= UpdateRoomUI;
            _sessionService.OnSessionChanged += UpdateRoomUI;
        }
        if (_sessionManager != null)
        {
            _sessionManager.OnGameStarting += ShowGameStarting;
        }

        LobbyViewBase.OnActiveViewStackChanged += RefreshHudVisuals;
        PlayerProfileStore.OnProfileChanged += HandlePlayerProfileChanged;
        PlayerSkinSync.OnLocalPlayerSkinChanged += HandleLocalPlayerSkinChanged;

        if (_createRoomDialogUI != null) _createRoomDialogUI.gameObject.SetActive(false);
        if (_confirmDialogUI != null) _confirmDialogUI.gameObject.SetActive(false);

        ResolveAndBindHamburgerMenu();

        _hudPresenter = new LobbyHudPresenter(
            transform,
            _roomUI != null ? _roomUI.transform : null,
            _hamburgerMenuUI,
            _menuToggleButton,
            _menuToggleButtonText,
            _menuToggleHamburgerIcon,
            _menuToggleCloseIcon,
            _playerNameButton,
            _playerNameButtonText,
            ToggleMenuVisibility,
            OpenPlayerNameKeyboard);
        _hudPresenter.Initialize();

        _menuToggleButton = _hudPresenter.MenuToggleButton;
        _playerNameButton = _hudPresenter.PlayerNameButton;

        _isMenuOpen = _startWithMenuOpen;
        _isHamburgerOpen = false;
        TransitionTo(LobbyUIState.Discover, immediate: true);
    }

    private void OnDestroy()
    {
        LobbyMovement.IsInputBlocked = false;
        _hudPresenter?.Dispose();

        if (_hamburgerMenuUI != null)
        {
            _hamburgerMenuUI.OnMatchingMenuRequested -= OpenMatchingMenuFromHamburger;
            _hamburgerMenuUI.OnEditNameRequested -= OpenPlayerNameKeyboard;
            _hamburgerMenuUI.OnReturnToTitleRequested -= RequestReturnToTitle;
            _hamburgerMenuUI.OnCloseRequested -= CloseHamburgerMenu;
        }

        if (_discoverUI != null)
        {
            _discoverUI.OnRefreshRequested -= HandleRefreshRoomListRequested;
            _discoverUI.OnCreateRoomRequested -= HandleOpenCreateRoomDialogRequested;
            _discoverUI.OnBackRequested -= HandleMenuBackRequested;
            _discoverUI.OnJoinRoomRequested -= HandleJoinRoomRequested;
        }

        if (_roomUI != null)
        {
            _roomUI.OnLeaveRequested -= HandleLeaveRoomRequested;
            _roomUI.OnReadyToggleRequested -= HandleToggleReadyRequested;
            _roomUI.OnStartGameRequested -= StartGame;
        }

        if (_sessionService != null)
        {
            _sessionService.OnSessionChanged -= UpdateRoomUI;
        }
        if (_sessionManager != null)
        {
            _sessionManager.OnGameStarting -= ShowGameStarting;
        }

        LobbyViewBase.OnActiveViewStackChanged -= RefreshHudVisuals;
        PlayerProfileStore.OnProfileChanged -= HandlePlayerProfileChanged;
        PlayerSkinSync.OnLocalPlayerSkinChanged -= HandleLocalPlayerSkinChanged;
    }

    private void Update()
    {
        if (LobbyUIInputHelper.WasMenuTogglePressedThisFrame())
        {
            if (CanToggleMenu())
            {
                ToggleMenuVisibility();
                return;
            }
            Debug.LogWarning($"[LobbyUIManager] メニュー開閉がブロックされました: state={_currentState}");
        }

        HandleControllerMenuInput();
    }

    private void HandleControllerMenuInput()
    {
        if (!CanToggleMenu()) return;

        if (_isHamburgerOpen)
        {
            var pad = Gamepad.current;
            if (pad != null && pad.buttonEast.wasPressedThisFrame)
            {
                SetHamburgerMenuOpen(false);
                return;
            }

            if (EventSystem.current != null && !LobbyUIInputHelper.IsCurrentSelectionValid() && LobbyUIInputHelper.WasNavigationInputPressedThisFrame())
            {
                _hamburgerMenuUI?.FocusDefaultButton();
            }
            return;
        }

        if (!_isMenuOpen) return;

        var gamepad = Gamepad.current;
        if (gamepad != null)
        {
            if (gamepad.buttonEast.wasPressedThisFrame)
            {
                HandleMenuBackRequested();
                return;
            }

            if (gamepad.buttonWest.wasPressedThisFrame && _currentState == LobbyUIState.Discover)
            {
                UISound.Play(UISoundType.Enter);
                HandleOpenCreateRoomDialogRequested();
                return;
            }

            if (gamepad.buttonNorth.wasPressedThisFrame)
            {
                if (_currentState == LobbyUIState.Discover)
                {
                    UISound.Play(UISoundType.Enter);
                    HandleRefreshRoomListRequested();
                    return;
                }
                if (_currentState == LobbyUIState.InRoom && _sessionService?.CurrentSession != null && !_sessionService.CurrentSession.IsHost)
                {
                    UISound.Play(UISoundType.Enter);
                    HandleToggleReadyRequested();
                    return;
                }
            }
        }

        if (EventSystem.current != null && !LobbyUIInputHelper.IsCurrentSelectionValid() && LobbyUIInputHelper.WasNavigationInputPressedThisFrame())
        {
            FocusCurrentViewDefaultElement();
        }
    }

    #region メニュー・画面状態制御

    public void ToggleMenuVisibility()
    {
        if (!CanToggleMenu()) return;

        if (_hamburgerMenuUI != null)
        {
            if (_isMenuOpen)
            {
                SetMenuOpen(false);
            }
            else
            {
                SetHamburgerMenuOpen(!_isHamburgerOpen);
            }
            return;
        }

        SetMenuOpen(!_isMenuOpen);
    }

    public void SetHamburgerMenuOpen(bool isOpen)
    {
        if (_hamburgerMenuUI == null || !CanToggleMenu() || _isHamburgerOpen == isOpen) return;

        _isHamburgerOpen = isOpen;
        if (_isHamburgerOpen)
        {
            _isMenuOpen = false;
        }

        UISound.Play(_isHamburgerOpen ? UISoundType.Enter : UISoundType.Cancel);
        ApplyVisibility(immediate: false);

        if (!_isHamburgerOpen && !_isMenuOpen && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void CloseHamburgerMenu() => SetHamburgerMenuOpen(false);

    private void OpenMatchingMenuFromHamburger()
    {
        if (!CanToggleMenu()) return;

        _isHamburgerOpen = false;
        _isMenuOpen = true;
        UISound.Play(UISoundType.Enter);
        ApplyVisibility(immediate: false);

        TriggerInitialRoomRefreshIfNeeded();
        FocusCurrentViewDefaultElementDelayed().Forget();
    }

    private void ReturnToHamburgerFromMatchingMenu()
    {
        if (_hamburgerMenuUI == null || !CanToggleMenu()) return;

        _isMenuOpen = false;
        _isHamburgerOpen = true;
        UISound.Play(UISoundType.Cancel);
        ApplyVisibility(immediate: false);
    }

    private void HandleMenuBackRequested()
    {
        if (!CanToggleMenu()) return;
        if (_hamburgerMenuUI != null)
        {
            ReturnToHamburgerFromMatchingMenu();
        }
        else
        {
            SetMenuOpen(false);
        }
    }

    public void SetMenuOpen(bool isOpen)
    {
        if (!CanToggleMenu() || _isMenuOpen == isOpen) return;

        _isMenuOpen = isOpen;
        UISound.Play(_isMenuOpen ? UISoundType.Enter : UISoundType.Cancel);
        ApplyVisibility(immediate: false);

        if (_isMenuOpen)
        {
            TriggerInitialRoomRefreshIfNeeded();
            FocusCurrentViewDefaultElementDelayed().Forget();
        }
        else if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void TriggerInitialRoomRefreshIfNeeded()
    {
        if (_currentState == LobbyUIState.Discover && !_hasRefreshedOnce)
        {
            _hasRefreshedOnce = true;
            HandleRefreshRoomListRequested();
        }
    }

    private void TransitionTo(LobbyUIState state, bool immediate = false)
    {
        _currentState = state;
        ApplyVisibility(immediate);

        if (_isMenuOpen && state != LobbyUIState.Connecting)
        {
            FocusCurrentViewDefaultElementDelayed().Forget();
        }
    }

    private void ApplyVisibility(bool immediate = false)
    {
        bool isConnecting = _currentState == LobbyUIState.Connecting;
        bool discoverTarget = !isConnecting && _isMenuOpen && _currentState == LobbyUIState.Discover;
        bool roomTarget = !isConnecting && _isMenuOpen && _currentState == LobbyUIState.InRoom;
        bool hamburgerTarget = !isConnecting && _isHamburgerOpen && !_isMenuOpen;

        if (immediate)
        {
            _discoverUI?.SetVisible(discoverTarget);
            _roomUI?.SetVisibleInstant(roomTarget);
            _hamburgerMenuUI?.SetOpen(hamburgerTarget, immediate: true);
        }
        else
        {
            UpdateViewVisibilityAnimated(_discoverUI, discoverTarget);
            UpdateViewVisibilityAnimated(_roomUI, roomTarget);
            if (_hamburgerMenuUI != null && _hamburgerMenuUI.IsOpen != hamburgerTarget)
            {
                _hamburgerMenuUI.SetOpen(hamburgerTarget, immediate: false);
            }
        }

        if (_loadingOverlay != null)
        {
            _loadingOverlay.SetLoadingState(isConnecting, isConnecting ? "通信中..." : null, immediate);
        }

        SyncMovementInputBlock();
        RefreshHudVisuals();
    }

    private void SyncMovementInputBlock()
    {
        LobbyMovement.IsInputBlocked =
            _currentState == LobbyUIState.Connecting ||
            _isMenuOpen ||
            _isHamburgerOpen ||
            _isEditingPlayerName;
    }

    private void UpdateViewVisibilityAnimated(LobbyViewBase view, bool targetVisible)
    {
        if (view == null) return;

        if (targetVisible)
        {
            if (!view.IsVisible || !view.gameObject.activeInHierarchy)
            {
                view.ShowAsync().Forget();
            }
        }
        else if (view.IsVisible || view.gameObject.activeSelf)
        {
            view.HideAsync().Forget();
        }
    }

    private bool CanToggleMenu()
    {
        if (_currentState == LobbyUIState.Connecting || _isEditingPlayerName) return false;
        if (_createRoomDialogUI != null && _createRoomDialogUI.gameObject.activeInHierarchy) return false;
        if (_confirmDialogUI != null && _confirmDialogUI.gameObject.activeInHierarchy) return false;
        return true;
    }

    private bool CanStartLobbyOperation()
    {
        return !_isBusyOperation && !_isStartingGame && _currentState != LobbyUIState.Connecting;
    }

    private bool CanOpenPlayerNameEditor()
    {
        return !_isEditingPlayerName && CanStartLobbyOperation() && !LobbyViewBase.IsModalOpen && CanToggleMenu();
    }

    private void ResolveAndBindHamburgerMenu()
    {
        if (_hamburgerMenuUI == null)
        {
            _hamburgerMenuUI = GetComponentInChildren<LobbyHamburgerMenuUI>(true);
        }
        if (_hamburgerMenuUI == null)
        {
            _hamburgerMenuUI = FindAnyObjectByType<LobbyHamburgerMenuUI>(FindObjectsInactive.Include);
        }

        if (_hamburgerMenuUI == null) return;

        _hamburgerMenuUI.OnMatchingMenuRequested -= OpenMatchingMenuFromHamburger;
        _hamburgerMenuUI.OnMatchingMenuRequested += OpenMatchingMenuFromHamburger;
        _hamburgerMenuUI.OnEditNameRequested -= OpenPlayerNameKeyboard;
        _hamburgerMenuUI.OnEditNameRequested += OpenPlayerNameKeyboard;
        _hamburgerMenuUI.OnReturnToTitleRequested -= RequestReturnToTitle;
        _hamburgerMenuUI.OnReturnToTitleRequested += RequestReturnToTitle;
        _hamburgerMenuUI.OnCloseRequested -= CloseHamburgerMenu;
        _hamburgerMenuUI.OnCloseRequested += CloseHamburgerMenu;
    }

    private void RefreshHudVisuals()
    {
        bool isOnline = _sessionManager != null && _sessionManager.IsInitialized;
        _hudPresenter?.RefreshHudVisuals(
            _currentState,
            _isMenuOpen,
            _isHamburgerOpen,
            _isEditingPlayerName,
            isOnline,
            _sessionService?.CurrentSession);
    }

    private void HandlePlayerProfileChanged(PlayerProfile profile) => RefreshHudVisuals();
    private void HandleLocalPlayerSkinChanged(int skinId) => RefreshHudVisuals();

    private async UniTaskVoid FocusCurrentViewDefaultElementDelayed()
    {
        await UniTask.Yield(PlayerLoopTiming.Update);
        if (this == null || !_isMenuOpen) return;

        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            if (this == null || !_isMenuOpen || !CanToggleMenu()) return;

            LobbyViewBase activeView = _currentState == LobbyUIState.Discover
                ? (LobbyViewBase)_discoverUI
                : (_currentState == LobbyUIState.InRoom ? _roomUI : null);

            if (activeView != null && activeView.IsTopActiveView)
            {
                if (!activeView.TryGetComponent<CanvasGroup>(out var cg) || cg.interactable)
                {
                    break;
                }
            }

            elapsed += Time.unscaledDeltaTime;
            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        if (this == null || !_isMenuOpen) return;
        FocusCurrentViewDefaultElement();
    }

    private void FocusCurrentViewDefaultElement()
    {
        if (!_isMenuOpen || !CanToggleMenu()) return;

        if (_currentState == LobbyUIState.Discover)
        {
            _discoverUI?.FocusDefaultElement();
        }
        else if (_currentState == LobbyUIState.InRoom)
        {
            _roomUI?.FocusDefaultElement();
        }
    }

    #endregion

    #region プレイヤー名編集・タイトル帰還ユースケース

    public void OpenPlayerNameKeyboard()
    {
        if (!CanOpenPlayerNameEditor()) return;
        OpenPlayerNameKeyboardAsync().Forget();
    }

    private async UniTaskVoid OpenPlayerNameKeyboardAsync()
    {
        if (!CanOpenPlayerNameEditor()) return;

        _isEditingPlayerName = true;
        SyncMovementInputBlock();
        RefreshHudVisuals();

        try
        {
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            LobbyVirtualKeyboardUI vkPrefab = _virtualKeyboardPrefab != null
                ? _virtualKeyboardPrefab
                : (_createRoomDialogUI != null ? _createRoomDialogUI.VirtualKeyboardPrefab : null);

            var (confirmed, inputText) = await LobbyTextInputPrompt.OpenAsync(
                parentCanvas,
                initialText: PlayerProfileStore.PlayerName,
                title: "プレイヤー名を入力",
                maxLength: PlayerProfile.MaxPlayerNameLength,
                virtualKeyboardPrefab: vkPrefab,
                textInputDialogPrefab: _textInputDialogPrefab,
                showPresetsOnVirtualKeyboard: false);

            if (this == null) return;

            if (confirmed)
            {
                PlayerProfileStore.SetPlayerName(inputText);
            }
        }
        catch (OperationCanceledException)
        {
            // ダイアログ破棄等によるキャンセル時は静かに終了
        }
        finally
        {
            _isEditingPlayerName = false;
            if (this != null)
            {
                SyncMovementInputBlock();
                RefreshHudVisuals();
                if (_isHamburgerOpen)
                {
                    _hamburgerMenuUI?.FocusEditNameButton();
                }
                else if (_isMenuOpen)
                {
                    FocusCurrentViewDefaultElementDelayed().Forget();
                }
            }
        }
    }

    private void RequestReturnToTitle()
    {
        if (!CanStartLobbyOperation()) return;
        RunBusyOperationAsync(ReturnToTitleCoreAsync).Forget();
    }

    private async UniTask ReturnToTitleCoreAsync()
    {
        if (_confirmDialogUI != null)
        {
            bool confirmed = await _confirmDialogUI.ShowAsync("タイトル画面に戻りますか？");
            if (this == null) return;

            if (!confirmed)
            {
                if (_isHamburgerOpen)
                {
                    _hamburgerMenuUI?.FocusDefaultButton();
                }
                return;
            }
        }

        _isHamburgerOpen = false;
        _isMenuOpen = false;
        TransitionTo(LobbyUIState.Connecting, immediate: true);

        if (_sessionManager != null)
        {
            await _sessionManager.DisconnectAndCleanupAsync();
            if (this == null) return;
        }

        SceneManager.LoadScene(string.IsNullOrEmpty(_titleSceneName) ? "Title" : _titleSceneName);
    }

    #endregion

    #region セッション操作ユースケース

    private async UniTaskVoid RunBusyOperationAsync(Func<UniTask> operation)
    {
        if (!CanStartLobbyOperation()) return;

        _isBusyOperation = true;
        try
        {
            await operation();
        }
        catch (OperationCanceledException)
        {
            // UI破棄・キャンセル時は静かに終了
        }
        finally
        {
            _isBusyOperation = false;
        }
    }

    private void HandleRefreshRoomListRequested() => RunBusyOperationAsync(RefreshRoomListCoreAsync).Forget();
    private void HandleOpenCreateRoomDialogRequested() => RunBusyOperationAsync(OpenCreateRoomDialogCoreAsync).Forget();
    private void HandleJoinRoomRequested(ISessionInfo sessionInfo)
    {
        if (sessionInfo == null) return;
        RunBusyOperationAsync(() => JoinRoomCoreAsync(sessionInfo)).Forget();
    }
    private void HandleLeaveRoomRequested() => RunBusyOperationAsync(LeaveRoomCoreAsync).Forget();
    private void HandleToggleReadyRequested() => ToggleReadyStateAsync().Forget();

    private async UniTask RefreshRoomListCoreAsync()
    {
        if (_sessionService == null) return;

        _loadingOverlay?.SetLoadingState(true, "更新中...");
        try
        {
            var sessions = await _sessionService.SearchRoomSessionsAsync();
            if (this == null) return;

            _discoverUI?.UpdateRoomList(sessions);
        }
        finally
        {
            if (this != null)
            {
                _loadingOverlay?.SetLoadingState(false);
                if (_isMenuOpen && _currentState == LobbyUIState.Discover)
                {
                    FocusCurrentViewDefaultElementDelayed().Forget();
                }
            }
        }
    }

    private async UniTask OpenCreateRoomDialogCoreAsync()
    {
        if (_createRoomDialogUI == null || _sessionService == null) return;

        var result = await _createRoomDialogUI.ShowAsync();
        if (this == null) return;

        if (result.IsCanceled)
        {
            FocusCurrentViewDefaultElementDelayed().Forget();
            return;
        }

        while (string.IsNullOrWhiteSpace(result.RoomName))
        {
            bool retry = _confirmDialogUI != null && await _confirmDialogUI.ShowAsync("ルーム名を正しく入力してください");
            if (this == null) return;

            if (!retry)
            {
                FocusCurrentViewDefaultElementDelayed().Forget();
                return;
            }

            result = await _createRoomDialogUI.ShowAsync();
            if (this == null) return;

            if (result.IsCanceled)
            {
                FocusCurrentViewDefaultElementDelayed().Forget();
                return;
            }
        }

        await ExecuteSessionConnectionAsync(
            () => _sessionService.CreateRoomSessionAsync(result.RoomName, result.MaxPlayers, result.Protocol),
            failureDialogMessage: "ルームの作成に失敗しました。",
            errorLogPrefix: "ルーム作成中");
    }

    private async UniTask JoinRoomCoreAsync(ISessionInfo sessionInfo)
    {
        if (_sessionService == null) return;

        if (_confirmDialogUI != null)
        {
            bool confirmed = await _confirmDialogUI.ShowAsync($"{sessionInfo.Name}に参加しますか？");
            if (this == null) return;

            if (!confirmed)
            {
                FocusCurrentViewDefaultElementDelayed().Forget();
                return;
            }
        }

        await ExecuteSessionConnectionAsync(
            () => _sessionService.JoinRoomSessionAsync(sessionInfo),
            failureDialogMessage: "ルームへの参加に失敗しました。",
            errorLogPrefix: "ルーム参加中");
    }

    private async UniTask ExecuteSessionConnectionAsync(
        Func<UniTask<bool>> connectAction,
        string failureDialogMessage,
        string errorLogPrefix)
    {
        TransitionTo(LobbyUIState.Connecting);
        try
        {
            bool success = await connectAction();
            if (this == null) return;

            if (success)
            {
                TransitionTo(LobbyUIState.InRoom);
                UpdateRoomUI();
            }
            else
            {
                TransitionTo(LobbyUIState.Discover);
                if (_confirmDialogUI != null)
                {
                    await _confirmDialogUI.ShowAsync(failureDialogMessage);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Debug.LogError($"[LobbyUIManager] {errorLogPrefix}にエラーが発生しました: {ex.Message}");
            if (this != null)
            {
                TransitionTo(LobbyUIState.Discover);
            }
        }
    }

    private async UniTask LeaveRoomCoreAsync()
    {
        if (_sessionService == null) return;

        if (_confirmDialogUI != null)
        {
            bool confirmed = await _confirmDialogUI.ShowAsync("退出しますか？");
            if (this == null) return;

            if (!confirmed)
            {
                FocusCurrentViewDefaultElementDelayed().Forget();
                return;
            }
        }

        bool leftSuccessfully = false;
        TransitionTo(LobbyUIState.Connecting);
        try
        {
            await _sessionService.LeaveRoomSessionAsync();
            if (this == null) return;

            TransitionTo(LobbyUIState.Discover);
            leftSuccessfully = true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Debug.LogError($"[LobbyUIManager] ルーム退出中にエラーが発生しました: {ex.Message}");
            if (this != null)
            {
                TransitionTo(LobbyUIState.Discover);
            }
        }

        if (leftSuccessfully && this != null)
        {
            await RefreshRoomListCoreAsync();
        }
    }

    private void UpdateRoomUI()
    {
        if (_currentState == LobbyUIState.InRoom && _sessionService?.CurrentSession != null)
        {
            _roomUI?.Refresh(_sessionService.CurrentSession);
        }
        RefreshHudVisuals();
    }

    private async UniTaskVoid ToggleReadyStateAsync()
    {
        if (_isUpdatingReady || _isBusyOperation || _isStartingGame) return;

        var session = _sessionService?.CurrentSession;
        if (session?.CurrentPlayer == null) return;

        _isUpdatingReady = true;
        try
        {
            bool isReady = SessionPlayerPropertyHelper.IsPlayerReady(session.CurrentPlayer);
            await _sessionService.SetReadyStatusAsync(!isReady);
        }
        finally
        {
            _isUpdatingReady = false;
        }
    }

    private void StartGame()
    {
        if (_isStartingGame) return;
        if (_sessionManager == null)
        {
            Debug.LogError("[LobbyUIManager] MultiplayerSessionManager が見つからないためゲームを開始できません。");
            return;
        }

        _isStartingGame = true;
        StartGameAsync().Forget();
    }

    private async UniTaskVoid StartGameAsync()
    {
        try
        {
            // EventSystem のクリックイベントループを抜けてからシーン遷移を開始する
            await UniTask.Yield(PlayerLoopTiming.Update);
            if (this == null) return;

            if (_sessionManager == null)
            {
                _isStartingGame = false;
                return;
            }

            TransitionTo(LobbyUIState.Connecting, immediate: true);
            _sessionManager.StartMultiplayGame();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Debug.LogError($"[LobbyUIManager] ゲーム開始処理でエラーが発生しました: {ex.Message}");
            _isStartingGame = false;
            if (this != null)
            {
                TransitionTo(LobbyUIState.InRoom);
            }
        }
    }

    private void ShowGameStarting() => ShowGameStartingAsync().Forget();

    private async UniTaskVoid ShowGameStartingAsync()
    {
        await UniTask.Yield(PlayerLoopTiming.Update);
        if (this == null) return;

        TransitionTo(LobbyUIState.Connecting, immediate: true);
    }

    #endregion
}
