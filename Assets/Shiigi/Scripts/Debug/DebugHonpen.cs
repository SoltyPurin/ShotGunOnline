using UnityEngine;
using UnityEngine.UI;

public class DebugHonpen : MonoBehaviour
{
    private MP_GameManager _gameManager;
    [SerializeField] private Text _gameStateText;

    private void Start()
    {
        _gameManager = MP_GameManager.Instance;
    }

    private void Update()
    {
        if (_gameManager != null)
        {
            if (_gameStateText != null)
                _gameStateText.text = $"Game State: {_gameManager.CurrentState}";
        }
    }
}
