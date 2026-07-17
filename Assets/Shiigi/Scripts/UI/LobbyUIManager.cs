using Cysharp.Threading.Tasks;
using Unity.Services.Multiplayer;
using UnityEngine;

public class LobbyUIManager : MonoBehaviour
{
    [SerializeField] private LobbyDiscoverUI _discoverUI;
    [SerializeField] private CreateRoomDialogUI _createRoomDialogUI;
    [SerializeField] private ConfirmDialogUI _confirmDialogUI; // 汎用の確認用

    private MultiplayerSessionManager _sessionManager;

    private void Start()
    {
        _sessionManager = MultiplayerSessionManager.Instance;

        // Viewのイベントを購読
        _discoverUI.OnRefreshRequested += RefreshRoomList;
        _discoverUI.OnCreateRoomRequested += OpenCreateRoomDialog;
        _discoverUI.OnJoinRoomRequested += JoinRoom;

        // 初回ロード
        RefreshRoomList();
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
    }

    /// <summary>
    /// ルーム一覧の更新処理
    /// </summary>
    private async void RefreshRoomList()
    {
        _discoverUI.SetLoadingState(true);
        try
        {
            var sessions = await _sessionManager.SearchRoomSessionsAsync();
            _discoverUI.UpdateRoomList(sessions);
        }
        finally
        {
            _discoverUI.SetLoadingState(false);
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
        if (string.IsNullOrWhiteSpace(result.RoomName))
        {
            await _confirmDialogUI.ShowAsync("ルーム名を入力してください。");
            return;
        }

        // 決定されたらセッション作成処理実行
        _discoverUI.SetLoadingState(true);
        try
        {
            var success = await _sessionManager.CreateRoomSessionAsync(result.RoomName, result.MaxPlayers);
            if (success)
            {
                Debug.Log("ルーム作成に成功しました。");
                // TODO: ルーム画面UIへ遷移するなどの処理を行う
            }
            else
            {
                await _confirmDialogUI.ShowAsync("ルームの作成に失敗しました。");
            }
        }
        finally
        {
            _discoverUI.SetLoadingState(false);
            RefreshRoomList(); // リストを最新にする
        }
    }

    /// <summary>
    /// ルーム参加処理
    /// </summary>
    private async void JoinRoom(ISessionInfo sessionInfo)
    {
        _discoverUI.SetLoadingState(true);
        try
        {
            await _sessionManager.JoinRoomSessionAsync(sessionInfo);
        }
        finally
        {
            _discoverUI.SetLoadingState(false);
        }
    }
}
