using Cysharp.Threading.Tasks;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// キーボード／マウス操作時に直接テキスト入力（日本語IME対応）を行うコンパクトなモーダルダイアログ。
/// コントローラー操作時は LobbyVirtualKeyboardUI を開き、キーボード／マウス操作時は本ダイアログを開きます。
/// </summary>
public class LobbyTextInputDialogUI : LobbyViewBase
{
    private const int DefaultMaxLength = 20;

    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private Text _counterText;
    [SerializeField] private InputField _inputField;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _cancelButton;
    [SerializeField] private Color _blockerColor = new Color(0.02f, 0.013f, 0.007f, 0.78f);

    private UniTaskCompletionSource<(bool Confirmed, string Text)> _utcs;
    private GameObject _blocker;
    private int _maxLength = DefaultMaxLength;
    private bool _isBound;

    public static async UniTask<(bool Confirmed, string Text)> OpenAsync(
        Canvas parentCanvas,
        string initialText = "",
        string title = "テキストを入力",
        int maxLength = DefaultMaxLength,
        LobbyTextInputDialogUI prefab = null)
    {
        if (parentCanvas == null)
        {
            Debug.LogError("[LobbyTextInputDialogUI] parentCanvas が null のため入力ダイアログを開けません。");
            return (false, initialText ?? string.Empty);
        }

        LobbyTextInputDialogUI instance = parentCanvas.GetComponentInChildren<LobbyTextInputDialogUI>(true);
        if (instance == null)
        {
            if (prefab == null)
            {
                Debug.LogError("[LobbyTextInputDialogUI] 入力ダイアログのプレハブが設定されていません。");
                return (false, initialText ?? string.Empty);
            }

            instance = Instantiate(prefab, parentCanvas.transform, false);
            instance.name = prefab.name;
        }

        return await instance.OpenDialogAsync(initialText, title, maxLength);
    }

    protected override void Awake()
    {
        _viewType = UIViewType.ModalDialog;
        base.Awake();
        BindButtonsIfNeeded();
    }

    private void BindButtonsIfNeeded()
    {
        if (_isBound) return;
        _isBound = true;

        if (_inputField != null)
        {
            if (_inputField.textComponent != null)
            {
                _inputField.textComponent.supportRichText = false;
            }
            _inputField.onValueChanged.AddListener(HandleValueChanged);
        }

        if (_confirmButton != null)
        {
            _confirmButton.onClick.AddListener(ConfirmInput);
            EnsureButtonSound(_confirmButton, UIButtonType.Default);
            UIButtonMetallicVisual.Attach(_confirmButton);
        }

        if (_cancelButton != null)
        {
            _cancelButton.onClick.AddListener(CancelInput);
            EnsureButtonSound(_cancelButton, UIButtonType.Cancel);
            UIButtonMetallicVisual.Attach(_cancelButton);
        }
    }

    public async UniTask<(bool Confirmed, string Text)> OpenDialogAsync(
        string initialText = "",
        string title = "テキストを入力",
        int maxLength = DefaultMaxLength)
    {
        BindButtonsIfNeeded();

        _maxLength = Mathf.Max(1, maxLength);
        string safeInitial = TrimToMaxLength(initialText ?? string.Empty);

        if (_titleText != null && !string.IsNullOrEmpty(title))
        {
            _titleText.text = title;
        }

        if (_inputField != null)
        {
            _inputField.characterLimit = _maxLength;
            _inputField.readOnly = false;
            _inputField.shouldActivateOnSelect = true;
            _inputField.SetTextWithoutNotify(safeInitial);
        }

        UpdateCounter(safeInitial);

        transform.SetAsLastSibling();
        EnsureBlocker();

        _utcs?.TrySetCanceled();
        _utcs = new UniTaskCompletionSource<(bool Confirmed, string Text)>();
        var utcs = _utcs;

        try
        {
            await ShowAsync();
        }
        catch (OperationCanceledException)
        {
            // 表示演出がキャンセルされても結果待ちは継続する
        }

        FocusInputField();
        return await utcs.Task;
    }

    public override GameObject GetDefaultFocusTarget()
    {
        if (_inputField != null && _inputField.gameObject.activeInHierarchy)
        {
            return _inputField.gameObject;
        }
        return _confirmButton != null ? _confirmButton.gameObject : null;
    }

    protected override void Update()
    {
        base.Update();
        if (_utcs == null || !gameObject.activeInHierarchy || !IsTopActiveView) return;
        if (_canvasGroup != null && !_canvasGroup.interactable) return;

        bool cancelPressed =
            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);

        if (cancelPressed)
        {
            UISound.Play(UISoundType.Cancel);
            CancelInput();
            return;
        }

        // IME変換中でないときの Enter / NumpadEnter で確定
        bool enterPressed =
            Keyboard.current != null &&
            (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame) &&
            string.IsNullOrEmpty(Input.compositionString);

        if (enterPressed)
        {
            bool isCancelFocused = EventSystem.current != null &&
                                   _cancelButton != null &&
                                   EventSystem.current.currentSelectedGameObject == _cancelButton.gameObject;
            if (!isCancelFocused)
            {
                UISound.Play(UISoundType.Enter);
                ConfirmInput();
            }
        }
    }

    private void FocusInputField()
    {
        if (_inputField == null || !_inputField.gameObject.activeInHierarchy) return;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(_inputField.gameObject);
        }
        _inputField.ActivateInputField();
        _inputField.MoveTextEnd(false);
    }

    private void HandleValueChanged(string value)
    {
        UpdateCounter(value);
    }

    private void UpdateCounter(string value)
    {
        if (_counterText == null) return;
        int len = string.IsNullOrEmpty(value) ? 0 : value.Length;
        _counterText.text = $"{len} / {_maxLength}";
    }

    private void ConfirmInput()
    {
        string text = _inputField != null ? TrimToMaxLength(_inputField.text) : string.Empty;
        CloseWithResult(confirmed: true, text).Forget();
    }

    private void CancelInput()
    {
        string text = _inputField != null ? _inputField.text : string.Empty;
        CloseWithResult(confirmed: false, text).Forget();
    }

    private async UniTaskVoid CloseWithResult(bool confirmed, string resultText)
    {
        if (_utcs == null) return;
        var tcs = _utcs;
        _utcs = null;

        if (_inputField != null)
        {
            _inputField.DeactivateInputField();
        }
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
            // 非表示演出がキャンセルされても結果は必ず確定させる
        }
        finally
        {
            tcs.TrySetResult((confirmed, resultText ?? string.Empty));
        }
    }

    protected override void OnDestroy()
    {
        _utcs?.TrySetCanceled();
        _utcs = null;
        base.OnDestroy();
    }

    private void EnsureBlocker()
    {
        if (_blocker == null)
        {
            Transform existing = transform.Find("ScreenBlocker");
            if (existing != null)
            {
                _blocker = existing.gameObject;
            }
            else
            {
                _blocker = UIBlockerHelper.EnsureBlocker(transform, _blockerColor);
                return;
            }
        }

        _blocker.SetActive(true);
        _blocker.transform.SetAsFirstSibling();
    }

    private string TrimToMaxLength(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= _maxLength ? value : value.Substring(0, _maxLength);
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
}
