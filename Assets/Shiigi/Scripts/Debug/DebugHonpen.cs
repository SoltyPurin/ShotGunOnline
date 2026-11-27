using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// インゲームの進行状態（IGameStateProvider）をイベント駆動で購読してテキスト表示するデバッグUI (DIP / ゼロGCポーリング廃止)。
/// </summary>
public class DebugHonpen : MonoBehaviour
{
    [SerializeField] private Text _gameStateText;

    private IGameStateProvider _gameStateProvider;
    private bool _isSubscribed;

    public void SetGameStateProvider(IGameStateProvider provider)
    {
        Unsubscribe();
        _gameStateProvider = provider;
        TrySubscribe();
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void TrySubscribe()
    {
        if (_isSubscribed) return;

        _gameStateProvider ??= MP_GameManager.Instance;
        if (_gameStateProvider == null) return;

        _gameStateProvider.StateChanged += HandleStateChanged;
        _isSubscribed = true;
        RefreshStateText(_gameStateProvider.CurrentState);
    }

    private void Unsubscribe()
    {
        if (!_isSubscribed) return;
        if (_gameStateProvider != null)
        {
            _gameStateProvider.StateChanged -= HandleStateChanged;
        }
        _isSubscribed = false;
    }

    private void HandleStateChanged(MP_GameManager.GameState previous, MP_GameManager.GameState current)
    {
        RefreshStateText(current);
    }

    private void RefreshStateText(MP_GameManager.GameState state)
    {
        if (_gameStateText != null)
        {
            _gameStateText.text = $"Game State: {state}";
        }
    }
}
