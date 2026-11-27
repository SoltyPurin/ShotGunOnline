using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// キーボード／マウス操作時に直接テキスト入力（日本語IME対応）を行うコンパクトなモーダルダイアログ。
/// LobbyModalDialogBase を継承し、ブロッカー・結果待機処理を共通化しています。
/// </summary>
public class LobbyTextInputDialogUI : LobbyModalDialogBase<(bool Confirmed, string Text)>
{
    private const int DefaultMaxLength = 20;

    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private Text _counterText;
    [SerializeField] private InputField _inputField;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _cancelButton;
    [SerializeField] private Color _blockerColor = new Color(0.02f, 0.013f, 0.007f, 0.78f);

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
            UIButtonSound.ConfigureButton(_confirmButton, UIButtonType.Default);
        }

        if (_cancelButton != null)
        {
            _cancelButton.onClick.AddListener(CancelInput);
            UIButtonSound.ConfigureButton(_cancelButton, UIButtonType.Cancel);
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
        EnsureModalBlocker(_blockerColor);

        var utcs = BeginResultWait();
        await ShowModalAnimatedSafelyAsync();

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
        if (!CanProcessModalInput) return;

        if (LobbyUIInputHelper.WasCancelPressedThisFrame())
        {
            UISound.Play(UISoundType.Cancel);
            CancelInput();
            return;
        }

        if (LobbyUIInputHelper.WasImeSafeEnterPressedThisFrame())
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
        CloseDialog(confirmed: true, text);
    }

    private void CancelInput()
    {
        string text = _inputField != null ? _inputField.text : string.Empty;
        CloseDialog(confirmed: false, text);
    }

    private void CloseDialog(bool confirmed, string resultText)
    {
        CloseModalWithResultAsync(
            (confirmed, resultText ?? string.Empty),
            onBeforeHide: () => _inputField?.DeactivateInputField()).Forget();
    }

    private string TrimToMaxLength(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= _maxLength ? value : value.Substring(0, _maxLength);
    }
}
