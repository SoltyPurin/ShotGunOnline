using UnityEngine;

using UnityEngine;

public class LoadingUIManager : MonoBehaviour
{
    [SerializeField] private GameObject _loadingUICanvas;

    private MP_GameManager _gameManager;
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

        _gameManager = MP_GameManager.Instance;
        if (_gameManager == null)
            return;

        _gameManager.StateChanged += OnStateChanged;
        _isSubscribed = true;
        ApplyState(_gameManager.CurrentState);
    }

    private void OnDisable()
    {
        if (_isSubscribed && _gameManager != null)
        {
            _gameManager.StateChanged -= OnStateChanged;
            _isSubscribed = false;
        }
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
