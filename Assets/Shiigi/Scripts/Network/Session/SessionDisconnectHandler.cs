using Cysharp.Threading.Tasks;
using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// ホストとの通信切断検知（NGOイベント）および通信エラーダイアログ表示・タイトルへの遷移を担当するクラス (SRP)
/// </summary>
public class SessionDisconnectHandler
{
    private const string DefaultTitleSceneName = "Title";
    private const string DefaultErrorMessage = "通信エラーが発生しました。\nホストとの接続が切断されました。";
    private const int ErrorCanvasSortingOrder = 2000;

    private readonly Transform _persistentRoot;
    private readonly Func<UniTask> _cleanupSessionAsync;
    private readonly string _titleSceneName;
    private readonly string _errorMessage;

    private ConfirmDialogUI _dialogInstance;
    private bool _isMonitoring;
    private bool _isHandlingDisconnect;
    private bool _isApplicationQuitting;

    public void NotifyApplicationQuitting()
    {
        _isApplicationQuitting = true;
        _isMonitoring = false;
    }

    public SessionDisconnectHandler(
        Transform persistentRoot,
        Func<UniTask> cleanupSessionAsync,
        ConfirmDialogUI dialogPrefab = null,
        string titleSceneName = DefaultTitleSceneName,
        string errorMessage = DefaultErrorMessage)
    {
        _persistentRoot = persistentRoot;
        _cleanupSessionAsync = cleanupSessionAsync ?? throw new ArgumentNullException(nameof(cleanupSessionAsync));
        _titleSceneName = string.IsNullOrWhiteSpace(titleSceneName) ? DefaultTitleSceneName : titleSceneName;
        _errorMessage = string.IsNullOrWhiteSpace(errorMessage) ? DefaultErrorMessage : errorMessage;

        if (dialogPrefab != null)
        {
            CreatePersistentDialogInstance(dialogPrefab);
        }
        else
        {
            TryCacheDialogFromScene();
        }
    }

    /// <summary>
    /// シーン内に存在する ConfirmDialogUI を検索し、未キャッシュであれば DontDestroyOnLoad 配下に複製して保持する
    /// </summary>
    public void TryCacheDialogFromScene()
    {
        if (_isApplicationQuitting || !Application.isPlaying || _dialogInstance != null) return;

        var sceneDialog = UnityEngine.Object.FindAnyObjectByType<ConfirmDialogUI>(FindObjectsInactive.Include);
        if (sceneDialog != null)
        {
            CreatePersistentDialogInstance(sceneDialog);
        }
    }

    public void SubscribeEvents(NetworkManager networkManager)
    {
        TryCacheDialogFromScene();
        _isMonitoring = true;

        if (networkManager == null) return;

        networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
        networkManager.OnClientDisconnectCallback += HandleClientDisconnected;

        networkManager.OnTransportFailure -= HandleTransportFailure;
        networkManager.OnTransportFailure += HandleTransportFailure;
    }

    public void UnsubscribeEvents(NetworkManager networkManager)
    {
        _isMonitoring = false;

        if (networkManager == null) return;

        networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
        networkManager.OnTransportFailure -= HandleTransportFailure;
    }

    public void NotifyUnexpectedDisconnect()
    {
        if (_isApplicationQuitting || !Application.isPlaying || !_isMonitoring || _isHandlingDisconnect) return;
        HandleUnexpectedDisconnectAsync().Forget();
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (_isApplicationQuitting || !Application.isPlaying || !_isMonitoring || _isHandlingDisconnect) return;

        var networkManager = NetworkManager.Singleton;
        // クライアント側で切断コールバックが発火した場合はホスト（サーバー）との接続切断を意味する
        if (networkManager != null && !networkManager.IsServer)
        {
            Debug.LogWarning($"[MSM]: ホストとの通信が切断されました (ClientId: {clientId})");
            HandleUnexpectedDisconnectAsync().Forget();
        }
    }

    private void HandleTransportFailure()
    {
        if (_isApplicationQuitting || !Application.isPlaying || !_isMonitoring || _isHandlingDisconnect) return;

        Debug.LogWarning("[MSM]: ネットワークトランスポート障害が発生しました。");
        HandleUnexpectedDisconnectAsync().Forget();
    }

    private async UniTaskVoid HandleUnexpectedDisconnectAsync()
    {
        if (_isHandlingDisconnect) return;
        _isHandlingDisconnect = true;
        _isMonitoring = false;

        try
        {
            // 既にタイトル画面にいる場合は処理しない
            if (SceneManager.GetActiveScene().name == _titleSceneName)
            {
                return;
            }

            TryCacheDialogFromScene();

            // 先にセッションおよびNetworkManagerをクリーンアップして以降の通信処理を停止
            await _cleanupSessionAsync();

            // ロビーと同じ ConfirmDialogUI ポップアップを表示してボタン押下を待機
            if (_dialogInstance != null)
            {
                await _dialogInstance.ShowAsync(_errorMessage);
            }
            else
            {
                Debug.LogWarning("[MSM]: ConfirmDialogUI が見つからないため、そのままタイトルへ遷移します。");
            }

            Time.timeScale = 1.0f;
            if (SceneManager.GetActiveScene().name != _titleSceneName)
            {
                SceneManager.LoadScene(_titleSceneName);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MSM]: 通信エラー切断処理中に例外が発生しました: {ex.Message}");
            Time.timeScale = 1.0f;
            SceneManager.LoadScene(_titleSceneName);
        }
        finally
        {
            _isHandlingDisconnect = false;
        }
    }

    private void CreatePersistentDialogInstance(ConfirmDialogUI sourceDialog)
    {
        if (_isApplicationQuitting || !Application.isPlaying || sourceDialog == null || _persistentRoot == null || _dialogInstance != null) return;

        // 最前面に表示するための専用Canvasを構築
        var canvasObj = new GameObject("DisconnectErrorCanvas");
        canvasObj.transform.SetParent(_persistentRoot, false);

        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = ErrorCanvasSortingOrder;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        canvasObj.AddComponent<GraphicRaycaster>();

        // MultiplayerLobbyScene の LobbyUIManager と同じレイアウト枠 (SizeDelta: -300, -100) を再現
        var frameObj = new GameObject("DialogFrame", typeof(RectTransform));
        var frameRect = frameObj.GetComponent<RectTransform>();
        frameRect.SetParent(canvasObj.transform, false);
        frameRect.anchorMin = Vector2.zero;
        frameRect.anchorMax = Vector2.one;
        frameRect.anchoredPosition = Vector2.zero;
        frameRect.sizeDelta = new Vector2(-300f, -100f);
        frameRect.pivot = new Vector2(0.5f, 0.5f);

        _dialogInstance = UnityEngine.Object.Instantiate(sourceDialog, frameRect, false);
        _dialogInstance.name = "ConfirmDialogOverlay_Persistent";
        _dialogInstance.gameObject.SetActive(false);
    }
}
