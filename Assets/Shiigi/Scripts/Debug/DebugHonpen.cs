using UnityEngine;
using UnityEngine.UI;

public class DebugHonpen : MonoBehaviour
{
    private IGameStateProvider _gameStateProvider;
    [SerializeField] private Text _gameStateText;

    private void Start()
    {
        _gameStateProvider = MP_GameManager.Instance;
    }

    private void Update()
    {
        if (_gameStateProvider != null && _gameStateText != null)
        {
            _gameStateText.text = $"Game State: {_gameStateProvider.CurrentState}";
        }
    }
}
