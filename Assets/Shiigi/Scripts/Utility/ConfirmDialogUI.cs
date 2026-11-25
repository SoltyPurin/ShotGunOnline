using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 確認・エラー表示用のモーダルダイアログUI。
/// LobbyViewBase を継承し、ModalDialog タイプのポップ演出とSE自動再生機能を提供します。
/// 表示時に全画面ブロック用UIを生成し、背後UIのボタン押下を遮断します。
/// </summary>
public class ConfirmDialogUI : LobbyViewBase
{
    [SerializeField] private Text _messageText;
    [SerializeField] private Button _yesButton;
    [SerializeField] private Button _noButton;
    [SerializeField] private Color _blockerColor = new Color(0f, 0f, 0f, 0.5f);

    private const int TopmostSortingOrder = 500;

    private UniTaskCompletionSource<bool> _utcs;
    private GameObject _blocker;
    private Canvas _overrideCanvas;
    private Text _yesButtonLabel;
    private string _defaultYesButtonText;

    protected override void Awake()
    {
        _viewType = UIViewType.ModalDialog;
        base.Awake();

        if (_messageText != null)
        {
            _messageText.supportRichText = false;
        }

        if (_yesButton != null)
        {
            _yesButtonLabel = _yesButton.GetComponentInChildren<Text>(true);
            if (_yesButtonLabel != null)
            {
                _defaultYesButtonText = _yesButtonLabel.text;
            }
        }

        EnsureButtonSound(_yesButton, UIButtonType.Default);
        EnsureButtonSound(_noButton, UIButtonType.Cancel);

        UIButtonMetallicVisual.Attach(_yesButton);
        UIButtonMetallicVisual.Attach(_noButton);
    }

    protected override void Update()
    {
        base.Update();
        if (_utcs == null || !gameObject.activeInHierarchy || !IsTopActiveView) return;

        bool cancelPressed =
            (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame) ||
            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame);

        if (cancelPressed)
        {
            UISound.Play(UISoundType.Cancel);
            bool hasNo = _noButton != null && _noButton.gameObject.activeSelf;
            CloseAndSetResult(!hasNo).Forget();
        }
    }

    public override GameObject GetDefaultFocusTarget()
    {
        if (_yesButton != null && _yesButton.gameObject.activeInHierarchy)
        {
            return _yesButton.gameObject;
        }
        return _noButton != null && _noButton.gameObject.activeInHierarchy ? _noButton.gameObject : null;
    }

    private static void EnsureButtonSound(Button button, UIButtonType type)
    {
        if (button == null) return;
        var sound = button.GetComponent<UIButtonSound>();
        if (sound == null)
        {
            sound = button.gameObject.AddComponent<UIButtonSound>();
        }
        sound.ButtonType = type;
    }

    public async UniTask<bool> ShowAsync(string message, bool showNoButton = true, string yesButtonText = null)
    {
        _utcs?.TrySetCanceled();

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        EnsureTopmostSorting();
        EnsureBlocker();

        if (_messageText != null)
        {
            _messageText.text = message;
        }

        if (_yesButton != null)
        {
            _yesButton.onClick.RemoveAllListeners();

            if (_yesButtonLabel == null)
            {
                _yesButtonLabel = _yesButton.GetComponentInChildren<Text>(true);
                if (_yesButtonLabel != null && string.IsNullOrEmpty(_defaultYesButtonText))
                {
                    _defaultYesButtonText = _yesButtonLabel.text;
                }
            }
            if (_yesButtonLabel != null)
            {
                _yesButtonLabel.text = !string.IsNullOrEmpty(yesButtonText)
                    ? yesButtonText
                    : _defaultYesButtonText;
            }

            // 背後UIや他ボタンへのキーボード/ゲームパッドのフォーカス移動を防止
            if (showNoButton && _noButton != null)
            {
                var yesNav = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = _noButton,
                    selectOnRight = _noButton
                };
                _yesButton.navigation = yesNav;
            }
            else
            {
                _yesButton.navigation = new Navigation { mode = Navigation.Mode.None };
            }
        }

        if (_noButton != null)
        {
            _noButton.onClick.RemoveAllListeners();
            _noButton.gameObject.SetActive(showNoButton);
            if (showNoButton && _yesButton != null)
            {
                _noButton.navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = _yesButton,
                    selectOnRight = _yesButton
                };
            }
        }

        if (EventSystem.current != null && _yesButton != null)
        {
            EventSystem.current.SetSelectedGameObject(_yesButton.gameObject);
        }

        _utcs = new UniTaskCompletionSource<bool>();
        var utcs = _utcs;

        if (_yesButton != null)
        {
            _yesButton.onClick.AddListener(() =>
            {
                CloseAndSetResult(true).Forget();
            });
        }

        if (_noButton != null)
        {
            _noButton.onClick.AddListener(() =>
            {
                CloseAndSetResult(false).Forget();
            });
        }

        // ポップ演出＆フェードイン（演出がキャンセルされても結果待ちは継続する）
        try
        {
            var showTask = base.ShowAsync();
            EnsureTopmostSorting();
            if (EventSystem.current != null && _yesButton != null && _yesButton.gameObject.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(_yesButton.gameObject);
            }
            await showTask;
        }
        catch (OperationCanceledException)
        {
            // 表示演出のキャンセルは無視し、結果の確定を待つ
        }

        if (EventSystem.current != null && _yesButton != null && _yesButton.gameObject.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(_yesButton.gameObject);
        }

        return await utcs.Task;
    }

    private async UniTaskVoid CloseAndSetResult(bool result)
    {
        if (_utcs == null) return;
        var utcs = _utcs;
        _utcs = null;

        if (_blocker != null)
        {
            _blocker.SetActive(false);
        }

        try
        {
            await HideAsync();
        }
        catch (OperationCanceledException)
        {
            // 非表示演出がキャンセル・破棄された場合でも結果は必ず確定させる
        }
        finally
        {
            // 結果を確定しないと ShowAsync の呼び出し元が永久に待機（デッドロック）するため必ず通知する
            utcs?.TrySetResult(result);
        }
    }

    protected override void OnDestroy()
    {
        // 結果待ちのままダイアログが破棄された場合、呼び出し元が永久に待機しないようキャンセルを通知する
        _utcs?.TrySetCanceled();
        _utcs = null;
        base.OnDestroy();
    }

    private void EnsureTopmostSorting()
    {
        transform.SetAsLastSibling();

        if (_overrideCanvas == null)
        {
            _overrideCanvas = GetComponent<Canvas>();
            if (_overrideCanvas == null)
            {
                _overrideCanvas = gameObject.AddComponent<Canvas>();
            }
        }

        var rootCanvas = transform.parent != null ? transform.parent.GetComponentInParent<Canvas>()?.rootCanvas : null;
        if (rootCanvas != null)
        {
            _overrideCanvas.sortingLayerID = rootCanvas.sortingLayerID;
        }

        // 非アクティブ中に設定するとUnityが無視することがあるため、アクティブ状態で必ず再適用する
        _overrideCanvas.overrideSorting = true;
        _overrideCanvas.sortingOrder = TopmostSortingOrder;

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }
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
