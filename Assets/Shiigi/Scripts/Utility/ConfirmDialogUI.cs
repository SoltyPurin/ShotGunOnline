using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmDialogUI : MonoBehaviour
{
    [SerializeField] private Text _messageText;
    [SerializeField] private Button _yesButton;
    [SerializeField] private Button _noButton;

    private UniTaskCompletionSource<bool> _utcs;

    public async UniTask<bool> ShowAsync(string message)
    {
        _messageText.text = message;
        gameObject.SetActive(true);

        _yesButton.onClick.RemoveAllListeners();
        _noButton.onClick.RemoveAllListeners();

        _utcs = new UniTaskCompletionSource<bool>();

        _yesButton.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
            _utcs.TrySetResult(true);
        });

        _noButton.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
            _utcs.TrySetResult(false);
        });

        return await _utcs.Task;
    }
}
