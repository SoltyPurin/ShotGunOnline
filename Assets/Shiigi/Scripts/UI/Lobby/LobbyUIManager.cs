using Cysharp.Threading.Tasks;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class LobbyUIManager : MonoBehaviour
{
    [SerializeField] private LobbyDiscoverUI _discoverUI;
    [SerializeField] private CreateRoomDialogUI _createRoomDialogUI;
    [SerializeField] private ConfirmDialogUI _confirmDialogUI; // 汎用の確認用
    [SerializeField] private LobbyRoomUI _roomUI;

    [Header("ローディング表示用オブジェクト")]
    [SerializeField]
    private LoadingOverlayController _loadingOverlay;

    [Header("集会所メニュー開閉設定")]
    [Tooltip("シーン開始時にロビーUIを開いた状態にするか（false の場合は集会所ステージから開始）")]
    [SerializeField] private bool _startWithMenuOpen = false;
    [Tooltip("画面右上のメニュー開閉ボタン（未設定時は自動生成）")]
    [SerializeField] private Button _menuToggleButton;
    [SerializeField] private Text _menuToggleButtonText;

    private ILobbySessionService _sessionService;
    private MultiplayerSessionManager _sessionManager;
    private LobbyUIState _currentState;
    private bool _isMenuOpen;
    private bool _hasRefreshedOnce;

    public bool IsMenuOpen => _isMenuOpen;

    private void Start()
    {
        _sessionManager = MultiplayerSessionManager.Instance;
        _sessionService = _sessionManager;

        // Viewのイベントを購読
        _discoverUI.OnRefreshRequested += RefreshRoomList;
        _discoverUI.OnCreateRoomRequested += OpenCreateRoomDialog;
        _discoverUI.OnJoinRoomRequested += JoinRoom;

        // ルームUIのイベントを購読
        _roomUI.OnLeaveRequested += LeaveRoom;
        _roomUI.OnReadyToggleRequested += ToggleReadyState;
        _roomUI.OnStartGameRequested += StartGame;

        // セッション更新イベントを購読
        if (_sessionService != null)
        {
            _sessionService.OnSessionChanged += UpdateRoomUI;
        }
        if (_sessionManager != null)
        {
            _sessionManager.OnGameStarting += ShowGameStarting;
        }
        LobbyViewBase.OnActiveViewStackChanged += UpdateToggleButtonVisual;

        if (_createRoomDialogUI != null)
        {
            _createRoomDialogUI.gameObject.SetActive(false);
        }
        if (_confirmDialogUI != null)
        {
            _confirmDialogUI.gameObject.SetActive(false);
        }

        HideLegacyTitleBackdropIfNeeded();
        EnsureToggleHudButtonCreated();

        // 初期状態に遷移（集会所ステージ移動から開始できるよう _startWithMenuOpen を反映）
        _isMenuOpen = _startWithMenuOpen;
        TransitionTo(LobbyUIState.Discover, immediate: true);
    }

    private void OnDestroy()
    {
        LobbyMovement.IsInputBlocked = false;

        if (_menuToggleButton != null)
        {
            _menuToggleButton.onClick.RemoveListener(ToggleMenuVisibility);
        }

        // オブジェクトが破棄されたらイベントの購読を解除
        if (_discoverUI != null)
        {
            _discoverUI.OnRefreshRequested -= RefreshRoomList;
            _discoverUI.OnCreateRoomRequested -= OpenCreateRoomDialog;
            _discoverUI.OnJoinRoomRequested -= JoinRoom;
        }

        if (_roomUI != null)
        {
            _roomUI.OnLeaveRequested -= LeaveRoom;
            _roomUI.OnReadyToggleRequested -= ToggleReadyState;
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
        LobbyViewBase.OnActiveViewStackChanged -= UpdateToggleButtonVisual;
    }

    private void Update()
    {
        if (WasMenuTogglePressedThisFrame())
        {
            if (CanToggleMenu())
            {
                ToggleMenuVisibility();
                return;
            }
            else
            {
                Debug.LogWarning($"[LobbyUIManager] メニュー開閉がブロックされました: state={_currentState}, createDialogActive={_createRoomDialogUI != null && _createRoomDialogUI.gameObject.activeInHierarchy}, confirmDialogActive={_confirmDialogUI != null && _confirmDialogUI.gameObject.activeInHierarchy}");
            }
        }

        HandleControllerMenuInput();
    }

    private void HandleControllerMenuInput()
    {
        if (!_isMenuOpen || !CanToggleMenu()) return;

        var gamepad = Gamepad.current;
        if (gamepad != null)
        {
            // Bボタン: メニューを閉じて集会所へ戻る
            if (gamepad.buttonEast.wasPressedThisFrame)
            {
                SetMenuOpen(false);
                return;
            }

            // Xボタン: Discover中はルーム作成ダイアログを開く
            if (gamepad.buttonWest.wasPressedThisFrame && _currentState == LobbyUIState.Discover)
            {
                UISound.Play(UISoundType.Enter);
                OpenCreateRoomDialog();
                return;
            }

            // Yボタン: Discover中は一覧更新 / InRoom(ゲスト)中は準備完了トグル
            if (gamepad.buttonNorth.wasPressedThisFrame)
            {
                if (_currentState == LobbyUIState.Discover)
                {
                    UISound.Play(UISoundType.Enter);
                    RefreshRoomList();
                    return;
                }
                else if (_currentState == LobbyUIState.InRoom && _sessionService?.CurrentSession != null && !_sessionService.CurrentSession.IsHost)
                {
                    UISound.Play(UISoundType.Enter);
                    ToggleReadyState();
                    return;
                }
            }
        }

        // マウス操作などでフォーカスが外れた場合、十字キー/スティック入力で自動復帰
        if (EventSystem.current != null && !IsCurrentSelectionValid() && WasNavigationInputPressedThisFrame())
        {
            FocusCurrentViewDefaultElement();
        }
    }

    private static bool IsCurrentSelectionValid()
    {
        var selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null || !selected.activeInHierarchy) return false;
        var sel = selected.GetComponent<Selectable>();
        return sel == null || sel.IsInteractable();
    }

    private static bool WasNavigationInputPressedThisFrame()
    {
        var gamepad = Gamepad.current;
        if (gamepad != null)
        {
            if (gamepad.dpad.up.wasPressedThisFrame || gamepad.dpad.down.wasPressedThisFrame ||
                gamepad.dpad.left.wasPressedThisFrame || gamepad.dpad.right.wasPressedThisFrame ||
                gamepad.leftStick.ReadValue().sqrMagnitude > 0.25f ||
                gamepad.buttonSouth.wasPressedThisFrame)
            {
                return true;
            }
        }

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame ||
                keyboard.leftArrowKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame ||
                keyboard.wKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame ||
                keyboard.aKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame ||
                keyboard.enterKey.wasPressedThisFrame)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// ロビーUI（ルーム検索・ルーム待機画面）の表示/非表示を切り替える
    /// </summary>
    public void ToggleMenuVisibility()
    {
        SetMenuOpen(!_isMenuOpen);
    }

    /// <summary>
    /// ロビーUIの表示状態を明示的に設定する
    /// </summary>
    public void SetMenuOpen(bool isOpen)
    {
        if (!CanToggleMenu()) return;
        if (_isMenuOpen == isOpen) return;

        _isMenuOpen = isOpen;

        // メニュー開閉SEを再生
        UISound.Play(_isMenuOpen ? UISoundType.Enter : UISoundType.Cancel);

        ApplyVisibility(immediate: false);

        // ルーム検索画面を初めて開いた時は一覧を自動更新する
        if (_isMenuOpen && _currentState == LobbyUIState.Discover && !_hasRefreshedOnce)
        {
            _hasRefreshedOnce = true;
            RefreshRoomList();
        }

        if (_isMenuOpen)
        {
            FocusCurrentViewDefaultElementDelayed().Forget();
        }
        else if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    /// <summary>
    /// UIの状態を遷移させ、表示・非表示を制御する
    /// </summary>
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

        if (immediate)
        {
            _discoverUI.SetVisible(discoverTarget);
            _roomUI.SetVisibleInstant(roomTarget);
        }
        else
        {
            UpdateViewVisibilityAnimated(_discoverUI, discoverTarget);
            UpdateViewVisibilityAnimated(_roomUI, roomTarget);
        }

        // Connectingステートの場合はローディング表示を出す
        if (isConnecting)
        {
            _loadingOverlay.SetLoadingState(true, "通信中...", immediate);
        }
        else
        {
            _loadingOverlay.SetLoadingState(false, null, immediate);
        }

        // メニュー表示中または通信中は集会所キャラの移動入力をブロックする
        LobbyMovement.IsInputBlocked = isConnecting || _isMenuOpen;

        UpdateToggleButtonVisual();
    }

    private void UpdateViewVisibilityAnimated(LobbyViewBase view, bool targetVisible)
    {
        if (view == null) return;

        if (targetVisible)
        {
            if (!view.IsVisible || !view.gameObject.activeSelf)
            {
                view.ShowAsync().Forget();
            }
        }
        else
        {
            if (view.IsVisible || view.gameObject.activeSelf)
            {
                view.HideAsync().Forget();
            }
        }
    }

    private bool CanToggleMenu()
    {
        if (_currentState == LobbyUIState.Connecting) return false;
        if (_createRoomDialogUI != null && _createRoomDialogUI.gameObject.activeInHierarchy) return false;
        if (_confirmDialogUI != null && _confirmDialogUI.gameObject.activeInHierarchy) return false;
        return true;
    }

    private static bool WasMenuTogglePressedThisFrame()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.tabKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame))
        {
            return true;
        }

        var gamepad = Gamepad.current;
        if (gamepad != null && (gamepad.startButton.wasPressedThisFrame || gamepad.selectButton.wasPressedThisFrame))
        {
            return true;
        }

        return false;
    }

    private void UpdateToggleButtonVisual()
    {
        if (_menuToggleButton == null) return;

        bool canShowButton = _currentState != LobbyUIState.Connecting;
        _menuToggleButton.gameObject.SetActive(canShowButton);
        _menuToggleButton.interactable = canShowButton && !LobbyViewBase.IsModalOpen;

        if (_menuToggleButtonText == null) return;

        if (_isMenuOpen)
        {
            _menuToggleButtonText.text = "× ステージへ戻る [Tab / Esc / Option]";
        }
        else if (_currentState == LobbyUIState.InRoom && _sessionService?.CurrentSession != null)
        {
            var s = _sessionService.CurrentSession;
            _menuToggleButtonText.text = $"ルームメニュー ({s.Players.Count}/{s.MaxPlayers}) [Tab / Esc / Option]";
        }
        else
        {
            _menuToggleButtonText.text = "マッチングメニューを開く [Tab / Esc / Option]";
        }
    }

    /// <summary>
    /// 画面右上にロビーUI開閉用の常設HUDボタンを自動生成する（インスペクター未設定時）
    /// </summary>
    private void EnsureToggleHudButtonCreated()
    {
        if (_menuToggleButton == null)
        {
            Transform existing = transform.Find("Button_ToggleLobbyMenu");
            if (existing != null)
            {
                _menuToggleButton = existing.GetComponent<Button>();
                _menuToggleButtonText = existing.GetComponentInChildren<Text>(true);
            }
        }

        if (_menuToggleButton == null)
        {
            GameObject btnGo = new GameObject("Button_ToggleLobbyMenu", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(transform, false);

            // ダイアログやローディングより手前になりすぎないよう、RoomUI の直後に配置
            if (_roomUI != null)
            {
                btnGo.transform.SetSiblingIndex(_roomUI.transform.GetSiblingIndex() + 1);
            }

            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-32f, -24f);
            rt.sizeDelta = new Vector2(460f, 64f);

            Image img = btnGo.GetComponent<Image>();
            img.color = new Color(0.12f, 0.14f, 0.18f, 0.88f);

            _menuToggleButton = btnGo.GetComponent<Button>();
            _menuToggleButton.targetGraphic = img;

            GameObject txtGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            txtGo.transform.SetParent(btnGo.transform, false);

            RectTransform txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(12f, 4f);
            txtRt.offsetMax = new Vector2(-12f, -4f);

            _menuToggleButtonText = txtGo.GetComponent<Text>();
            Font referenceFont = GetComponentInChildren<Text>(true)?.font;
            if (referenceFont == null)
            {
                referenceFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            _menuToggleButtonText.font = referenceFont;
            _menuToggleButtonText.fontSize = 22;
            _menuToggleButtonText.alignment = TextAnchor.MiddleCenter;
            _menuToggleButtonText.color = Color.white;
        }

        _menuToggleButton.onClick.RemoveListener(ToggleMenuVisibility);
        _menuToggleButton.onClick.AddListener(ToggleMenuVisibility);
        LobbyModernTheme.AttachMetallicVisual(_menuToggleButton);
    }

    private async UniTaskVoid FocusCurrentViewDefaultElementDelayed()
    {
        await UniTask.Yield(PlayerLoopTiming.Update);
        if (this == null || !_isMenuOpen) return;
        FocusCurrentViewDefaultElement();
    }

    private void FocusCurrentViewDefaultElement()
    {
        if (!_isMenuOpen || !CanToggleMenu()) return;

        if (_currentState == LobbyUIState.Discover && _discoverUI != null)
        {
            _discoverUI.FocusDefaultElement();
        }
        else if (_currentState == LobbyUIState.InRoom && _roomUI != null)
        {
            _roomUI.FocusDefaultElement();
        }
    }

    /// <summary>
    /// タイトルシーンから流用された ImageShadow（巨大立ち絵）や上下黒帯が集会所ステージを隠さないよう非表示にする
    /// </summary>
    private static void HideLegacyTitleBackdropIfNeeded()
    {
        foreach (var rt in FindObjectsByType<RectTransform>(FindObjectsInactive.Include))
        {
            if (rt == null) continue;
            if (rt.name == "ImageShadow" || (rt.name == "Black" && rt.parent != null && rt.parent.name == "Canvas"))
            {
                rt.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// ルーム一覧の更新処理
    /// </summary>
    private async void RefreshRoomList()
    {
        _loadingOverlay.SetLoadingState(true, "更新中...");
        try
        {
            var sessions = await _sessionService.SearchRoomSessionsAsync();
            _discoverUI.UpdateRoomList(sessions);
        }
        finally
        {
            _loadingOverlay.SetLoadingState(false);
            if (_isMenuOpen && _currentState == LobbyUIState.Discover)
            {
                FocusCurrentViewDefaultElementDelayed().Forget();
            }
        }
    }

    /// <summary>
    /// ルーム作成ダイアログを開き、作成処理を行う
    /// </summary>
    private async void OpenCreateRoomDialog()
    {
        // ダイアログを開いて結果を待機
        var result = await _createRoomDialogUI.ShowAsync();

        if (result.IsCanceled)
        {
            FocusCurrentViewDefaultElementDelayed().Forget();
            return;
        }

        // バリデーション
        while (string.IsNullOrWhiteSpace(result.RoomName))
        {
            if (await _confirmDialogUI.ShowAsync("ルーム名を正しく入力してください"))
            {
                result = await _createRoomDialogUI.ShowAsync();
                if (result.IsCanceled)
                {
                    FocusCurrentViewDefaultElementDelayed().Forget();
                    return;
                }
            }
            else
            {
                FocusCurrentViewDefaultElementDelayed().Forget();
                return;
            }
        }

        // 決定されたらセッション作成処理実行（選択された詳細設定のプロトコルを使用）
        TransitionTo(LobbyUIState.Connecting);
        try
        {
            var success = await _sessionService.CreateRoomSessionAsync(
                result.RoomName,
                result.MaxPlayers,
                result.Protocol);
            if (success)
            {
                TransitionTo(LobbyUIState.InRoom);
                UpdateRoomUI();
            }
            else
            {
                TransitionTo(LobbyUIState.Discover);
                await _confirmDialogUI.ShowAsync("ルームの作成に失敗しました。");
            }
        }
        catch
        {
            TransitionTo(LobbyUIState.Discover);
        }
    }

    /// <summary>
    /// ルーム参加処理
    /// </summary>
    private async void JoinRoom(ISessionInfo sessionInfo)
    {
        TransitionTo(LobbyUIState.Connecting);
        try
        {
            var success = await _sessionService.JoinRoomSessionAsync(sessionInfo);
            if (success)
            {
                TransitionTo(LobbyUIState.InRoom);
                UpdateRoomUI();
            }
            else
            {
                TransitionTo(LobbyUIState.Discover);
                await _confirmDialogUI.ShowAsync("ルームへの参加に失敗しました。");
            }
        }
        catch
        {
            TransitionTo(LobbyUIState.Discover);
        }
    }

    /// <summary>
    /// ルーム退出処理
    /// </summary>
    private async void LeaveRoom()
    {
        // 確認ダイアログを表示して、ユーザーがキャンセルした場合は処理を中断
        if (!await _confirmDialogUI.ShowAsync("退出しますか？"))
        {
            FocusCurrentViewDefaultElementDelayed().Forget();
            return;
        }

        TransitionTo(LobbyUIState.Connecting);
        try
        {
            await _sessionService.LeaveRoomSessionAsync();
            TransitionTo(LobbyUIState.Discover);
            RefreshRoomList(); // リストを最新にする
        }
        catch
        {
            TransitionTo(LobbyUIState.Discover);
        }
    }

    /// <summary>
    /// ルーム画面の描画を更新する
    /// </summary>
    private void UpdateRoomUI()
    {
        if (_currentState == LobbyUIState.InRoom && _sessionService?.CurrentSession != null)
        {
            _roomUI.Refresh(_sessionService.CurrentSession);
        }
        UpdateToggleButtonVisual();
    }

    /// <summary>
    /// 準備完了状態のトグル切り替え
    /// </summary>
    private async void ToggleReadyState()
    {
        var session = _sessionService?.CurrentSession;
        if (session == null || session.CurrentPlayer == null) return;

        // 現在の準備状況を取得して反転させて送信
        var isReady = SessionPlayerPropertyHelper.IsPlayerReady(session.CurrentPlayer);
        await _sessionService.SetReadyStatusAsync(!isReady);
    }

    private bool _isStartingGame;

    /// <summary>
    /// ホストがゲームを開始する処理
    /// </summary>
    private void StartGame()
    {
        if (_isStartingGame) return;
        _isStartingGame = true;
        StartGameAsync().Forget();
    }

    private async UniTaskVoid StartGameAsync()
    {
        // EventSystem の OnPointerClick イベント処理ループ中（および UIButtonSound のクリック処理前）に
        // 同一コールスタック上でボタン自身の親パネル非アクティブ化・EventSystem選択解除・LoadScene を
        // 同時実行するとエディタ側で競合・フリーズするため、まず1フレーム待ってイベントループを抜ける
        await UniTask.Yield(PlayerLoopTiming.Update);
        if (this == null) return;

        TransitionTo(LobbyUIState.Connecting, immediate: true);

        if (_sessionManager != null)
        {
            _sessionManager.StartMultiplayGame();
        }
    }

    private void ShowGameStarting()
    {
        ShowGameStartingAsync().Forget();
    }

    private async UniTaskVoid ShowGameStartingAsync()
    {
        await UniTask.Yield(PlayerLoopTiming.Update);
        if (this == null) return;

        TransitionTo(LobbyUIState.Connecting, immediate: true);
    }
}
