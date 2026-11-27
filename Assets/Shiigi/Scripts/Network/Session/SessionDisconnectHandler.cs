using Cysharp.Threading.Tasks;
using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// ホストとの通信切断検知（NGOイベント）および通信エラーダイアログ表示・タイトルへの遷移を担当するクラス (SRP / DIP)。
/// インゲームの入力遮断処理は IGameInputBlocker へ委譲します。
/// </summary>
public class SessionDisconnectHandler
{
    private const string DefaultTitleSceneName = "Title";
    private const string DefaultErrorMessage = "通信エラーが発生しました。\nホストとの接続が切断されました。";
    private const int ErrorCanvasSortingOrder = 2000;

    private readonly Transform _persistentRoot;
    private readonly Func<UniTask> _cleanupSessionAsync;
    private readonly IGameInputBlocker _gameInputBlocker;
    private readonly string _titleSceneName;
    private readonly string _errorMessage;

    private GameObject _canvasObj;
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
        string errorMessage = DefaultErrorMessage,
        IGameInputBlocker gameInputBlocker = null)
    {
        _persistentRoot = persistentRoot;
        _cleanupSessionAsync = cleanupSessionAsync ?? throw new ArgumentNullException(nameof(cleanupSessionAsync));
        _gameInputBlocker = gameInputBlocker ?? new SceneGameInputBlocker();
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
        if (networkManager != null &&
            !networkManager.IsServer &&
            (clientId == networkManager.LocalClientId || clientId == NetworkManager.ServerClientId))
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
            if (SceneManager.GetActiveScene().name == _titleSceneName)
            {
                return;
            }

            _gameInputBlocker.BlockAllGameInputs();
            Time.timeScale = 0f;
            TryCacheDialogFromScene();
            await _cleanupSessionAsync();

            if (_isApplicationQuitting || SceneManager.GetActiveScene().name == _titleSceneName)
            {
                return;
            }

            if (_canvasObj != null)
            {
                _canvasObj.SetActive(true);
            }

            if (_dialogInstance != null)
            {
                await _dialogInstance.ShowAsync(_errorMessage, showNoButton: false, yesButtonText: "OK");
            }
            else
            {
                Debug.LogWarning("[MSM]: ConfirmDialogUI が見つからないため、そのままタイトルへ遷移します。");
            }

            ReturnToTitleSceneSafely();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MSM]: 通信エラー切断処理中に例外が発生しました: {ex.Message}");
            ReturnToTitleSceneSafely();
        }
        finally
        {
            Time.timeScale = 1.0f;
            _isHandlingDisconnect = false;
        }
    }

    private void ReturnToTitleSceneSafely()
    {
        if (_canvasObj != null)
        {
            _canvasObj.SetActive(false);
        }
        Time.timeScale = 1.0f;

        if (!_isApplicationQuitting && SceneManager.GetActiveScene().name != _titleSceneName)
        {
            SceneManager.LoadScene(_titleSceneName);
        }
    }

    private void CreatePersistentDialogInstance(ConfirmDialogUI sourceDialog)
    {
        if (_isApplicationQuitting || !Application.isPlaying || sourceDialog == null || _persistentRoot == null || _dialogInstance != null) return;

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

        _canvasObj = canvasObj;
        _canvasObj.SetActive(false);
    }
}
