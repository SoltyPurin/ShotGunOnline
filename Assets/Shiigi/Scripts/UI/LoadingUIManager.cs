using UnityEngine;

/// <summary>
/// ゲーム進行状態に応じてローディングUI Canvasの表示を切り替える。
/// </summary>
public class LoadingUIManager : MonoBehaviour
{
    [SerializeField] private GameObject _loadingUICanvas;

    private IGameStateProvider _gameStateProvider;
    private bool _isSubscribed;

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void TrySubscribe()
    {
        // 購読済みならスキップ
        if (_isSubscribed)
            return;

        _gameStateProvider = MP_GameManager.Instance;
        if (_gameStateProvider == null)
        {
            Debug.LogWarning("[LoadingUIManager] MP_GameManager が見つかりません。");
            return;
        }

        _gameStateProvider.StateChanged += OnStateChanged;
        _isSubscribed = true;
        ApplyState(_gameStateProvider.CurrentState);
    }

    private void OnDisable()
    {
        if (_isSubscribed && _gameStateProvider != null)
        {
            _gameStateProvider.StateChanged -= OnStateChanged;
        }
        _isSubscribed = false;
        _gameStateProvider = null;
    }

    private void OnStateChanged(MP_GameManager.GameState previous, MP_GameManager.GameState current)
    {
        ApplyState(current);
    }

    private void ApplyState(MP_GameManager.GameState state)
    {
        if (_loadingUICanvas == null)
            return;

        _loadingUICanvas.SetActive(
            state == MP_GameManager.GameState.WaitingForPlayers
        );
    }
}
