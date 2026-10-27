using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ConfirmDialogUI : MonoBehaviour
{
    [SerializeField] private Text _messageText;
    [SerializeField] private Button _yesButton;
    [SerializeField] private Button _noButton;

    private UniTaskCompletionSource<bool> _utcs;

    public async UniTask<bool> ShowAsync(string message, bool showNoButton = true, string yesButtonText = null)
    {
        _utcs?.TrySetCanceled();

        if (_messageText != null)
        {
            _messageText.text = message;
        }

        gameObject.SetActive(true);

        if (_yesButton != null)
        {
            _yesButton.onClick.RemoveAllListeners();

            if (!string.IsNullOrEmpty(yesButtonText))
            {
                var textComp = _yesButton.GetComponentInChildren<Text>();
                if (textComp != null)
                {
                    textComp.text = yesButtonText;
                }
            }

            // 背後UIや他ボタンへのキーボード/ゲームパッドのフォーカス移動を防止
            var nav = _yesButton.navigation;
            nav.mode = showNoButton ? Navigation.Mode.Automatic : Navigation.Mode.None;
            _yesButton.navigation = nav;
        }

        if (_noButton != null)
        {
            _noButton.onClick.RemoveAllListeners();
            _noButton.gameObject.SetActive(showNoButton);
        }

        if (EventSystem.current != null && _yesButton != null)
        {
            EventSystem.current.SetSelectedGameObject(_yesButton.gameObject);
        }

        _utcs = new UniTaskCompletionSource<bool>();

        if (_yesButton != null)
        {
            _yesButton.onClick.AddListener(() =>
            {
                gameObject.SetActive(false);
                _utcs.TrySetResult(true);
            });
        }

        if (_noButton != null)
        {
            _noButton.onClick.AddListener(() =>
            {
                gameObject.SetActive(false);
                _utcs.TrySetResult(false);
            });
        }

        return await _utcs.Task;
    }
}
