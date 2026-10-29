using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 確認・エラー表示用のモーダルダイアログUI
/// 表示時に全画面ブロック用UIを生成し、背後UIのボタン押下を遮断します。
/// </summary>
public class ConfirmDialogUI : MonoBehaviour
{
    [SerializeField] private Text _messageText;
    [SerializeField] private Button _yesButton;
    [SerializeField] private Button _noButton;
    [SerializeField] private Color _blockerColor = new Color(0f, 0f, 0f, 0.5f);

    private UniTaskCompletionSource<bool> _utcs;
    private GameObject _blocker;

    public async UniTask<bool> ShowAsync(string message, bool showNoButton = true, string yesButtonText = null)
    {
        _utcs?.TrySetCanceled();

        EnsureBlocker();

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

    private void EnsureBlocker()
    {
        if (_blocker == null)
        {
            _blocker = UIBlockerHelper.EnsureBlocker(transform, _blockerColor);
        }
        else
        {
            _blocker.SetActive(true);
            _blocker.transform.SetAsFirstSibling();
        }
    }
}
