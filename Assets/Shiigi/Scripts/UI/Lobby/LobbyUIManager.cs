using Cysharp.Threading.Tasks;
using Unity.Services.Multiplayer;
using UnityEngine;

public class LobbyUIManager : MonoBehaviour
{
    [SerializeField] private LobbyDiscoverUI _discoverUI;
    [SerializeField] private CreateRoomDialogUI _createRoomDialogUI;
    [SerializeField] private ConfirmDialogUI _confirmDialogUI; // 汎用の確認用
    [SerializeField] private LobbyRoomUI _roomUI;

    [Header("ローディング表示用オブジェクト")]
    [SerializeField]
    private LoadingOverlayController _loadingOverlay;

    private MultiplayerSessionManager _sessionManager;
    private LobbyUIState _currentState;

    private void Start()
    {
        _sessionManager = MultiplayerSessionManager.Instance;

        // Viewのイベントを購読
        _discoverUI.OnRefreshRequested += RefreshRoomList;
        _discoverUI.OnCreateRoomRequested += OpenCreateRoomDialog;
        _discoverUI.OnJoinRoomRequested += JoinRoom;

        // ルームUIのイベントを購読
        _roomUI.OnLeaveRequested += LeaveRoom;
        _roomUI.OnReadyToggleRequested += ToggleReadyState;
        _roomUI.OnStartGameRequested += StartGame;

        // セッション更新イベントを購読
        _sessionManager.OnSessionChanged += UpdateRoomUI;

        _createRoomDialogUI.gameObject.SetActive(false);

        // 初期状態に遷移
        TransitionTo(LobbyUIState.Discover);
    }

    private void OnDestroy()
    {
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

        if (_sessionManager != null)
        {
            _sessionManager.OnSessionChanged -= UpdateRoomUI;
        }
    }

    /// <summary>
    /// UIの状態を遷移させ、表示・非表示を制御する
    /// </summary>
    private void TransitionTo(LobbyUIState state)
    {
        _currentState = state;

        // 状態ごとのパネル表示切り替え
        _discoverUI.gameObject.SetActive(state == LobbyUIState.Discover);
        _roomUI.gameObject.SetActive(state == LobbyUIState.InRoom);

        // Connectingステートの場合はローディング表示を出す
        if (state == LobbyUIState.Connecting)
        {
            _loadingOverlay.SetLoadingState(true, "通信中...");
        }
        else
        {
            _loadingOverlay.SetLoadingState(false);
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
            var sessions = await _sessionManager.SearchRoomSessionsAsync();
            _discoverUI.UpdateRoomList(sessions);
        }
        finally
        {
            _loadingOverlay.SetLoadingState(false);
        }
    }

    /// <summary>
    /// ルーム作成ダイアログを開き、作成処理を行う
    /// </summary>
    private async void OpenCreateRoomDialog()
    {
        // ダイアログを開いて結果を待機
        var result = await _createRoomDialogUI.ShowAsync();

        if (result.IsCanceled) return;

        // 簡単な入力バリデーション
        while (string.IsNullOrWhiteSpace(result.RoomName))
        {
            if (await _confirmDialogUI.ShowAsync("ルーム名を正しく入力してください"))
            {
                result = await _createRoomDialogUI.ShowAsync();
                if (result.IsCanceled) return;
            }
            else
            {
                return;
            }
        }

        // 決定されたらセッション作成処理実行
        TransitionTo(LobbyUIState.Connecting);
        try
        {
            var success = await _sessionManager.CreateRoomSessionAsync(result.RoomName, result.MaxPlayers);
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
            var success = await _sessionManager.JoinRoomSessionAsync(sessionInfo);
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
        if (!await _confirmDialogUI.ShowAsync("退出しますか？")) return;

        TransitionTo(LobbyUIState.Connecting);
        try
        {
            await _sessionManager.LeaveRoomSessionAsync();
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
        if (_currentState == LobbyUIState.InRoom && _sessionManager.CurrentSession != null)
        {
            _roomUI.Refresh(_sessionManager.CurrentSession);
        }
    }

    /// <summary>
    /// 準備完了状態のトグル切り替え
    /// </summary>
    private async void ToggleReadyState()
    {
        var session = _sessionManager.CurrentSession;
        if (session == null || session.CurrentPlayer == null) return;

        // 現在の準備状況を取得
        var isReady = false;
        if (session.CurrentPlayer.Properties != null && 
            session.CurrentPlayer.Properties.TryGetValue("IsReady", out var prop))
        {
            isReady = prop.Value == "true";
        }

        // 反転させて送信
        var nextReadyState = !isReady;
        await _sessionManager.SetReadyStatusAsync(nextReadyState);
    }

    /// <summary>
    /// ホストがゲームを開始する処理
    /// </summary>
    private void StartGame()
    {
        Debug.Log("ゲームを開始");
        // TODO: ゲーム開始時の処理（NGO等のシーン遷移など）
    }
}
