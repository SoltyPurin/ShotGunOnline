using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// コントローラー操作に対応したソフトウェアキーボード（モーダルダイアログ）。
/// </summary>
public class LobbyVirtualKeyboardUI : LobbyModalDialogBase<(bool Confirmed, string Text)>
{
    private const int DefaultMaxLength = 20;
    private const float CaretBlinkInterval = 0.5f;
    private static readonly Color DefaultBlockerColor = new Color(0f, 0f, 0f, 0.68f);

    private readonly List<List<Button>> _navigationRows = new();
    private readonly List<(Button Button, Text Label, string LowerChar)> _letterButtons = new();
    private readonly List<(Button Button, Text Label, string DefaultText)> _presetButtons = new();

    private GameObject _presetRowObj;
    private Canvas _cachedRootCanvas;
    private Text _titleText;
    private Text _previewText;
    private Text _counterText;
    private Text _shiftButtonLabel;
    private Button _initialFocusButton;

    private Color _previewBaseColor = Color.white;
    private Color _shiftBaseColor = Color.white;
    private string _emptyCaretVisibleMarkup = "|（未入力）";
    private string _emptyCaretHiddenMarkup = " （未入力）";
    private string _currentText = string.Empty;
    private string _lastCompositionString = string.Empty;
    private int _maxLength = DefaultMaxLength;
    private int _lastDisplayLen = -1;
    private int _lastCounterMaxLength = -1;
    private bool _isShiftActive = true;
    private bool _isInitialized;
    private float _caretTimer;
    private bool _caretVisible = true;
    private bool _imeEnabledByDialog;
    private IMECompositionMode _previousImeMode = IMECompositionMode.Auto;
    private bool _wasComposingLastFrame;
    private Func<string> _randomTextGenerator;

    /// <summary>
    /// 指定されたCanvas配下に仮想キーボードプレハブを表示し、入力結果（確定有無と文字列）を返す汎用メソッド
    /// </summary>
    public static async UniTask<(bool Confirmed, string Text)> OpenAsync(
        Canvas parentCanvas,
        string initialText = "",
        string title = "テキストを入力",
        int maxLength = DefaultMaxLength,
        LobbyVirtualKeyboardUI prefab = null,
        bool showPresets = true,
        string[] customPresets = null,
        Func<string> randomTextGenerator = null)
    {
        if (parentCanvas == null)
        {
            Debug.LogError("[LobbyVirtualKeyboardUI] parentCanvas が null のため仮想キーボードを開けません。");
            return (false, initialText ?? string.Empty);
        }

        LobbyVirtualKeyboardUI instance = parentCanvas.GetComponentInChildren<LobbyVirtualKeyboardUI>(true);
        if (instance == null)
        {
            if (prefab == null)
            {
                Debug.LogError("[LobbyVirtualKeyboardUI] 仮想キーボードのプレハブが設定されていません。");
                return (false, initialText ?? string.Empty);
            }

            instance = Instantiate(prefab, parentCanvas.transform, false);
            instance.name = prefab.name;
        }

        return await instance.OpenDialogAsync(initialText, title, maxLength, showPresets, customPresets, randomTextGenerator);
    }

    /// <summary>
    /// インスタンスに対して直接パラメータを指定して仮想キーボードを開く
    /// </summary>
    public async UniTask<(bool Confirmed, string Text)> OpenDialogAsync(
        string initialText = "",
        string title = "テキストを入力",
        int maxLength = DefaultMaxLength,
        bool showPresets = true,
        string[] customPresets = null,
        Func<string> randomTextGenerator = null)
    {
        _maxLength = Mathf.Max(1, maxLength);
        _currentText = TrimToMaxLength(initialText ?? string.Empty);
        _randomTextGenerator = randomTextGenerator;

        if (!_isInitialized)
        {
            BindExistingUI();
            _isInitialized = true;
        }

        ApplyTitle(title);
        ApplyPresetOptions(showPresets, customPresets);
        BuildExplicitNavigation();

        transform.SetAsLastSibling();
        EnsureModalBlocker(DefaultBlockerColor);
        EnableImeForPhysicalKeyboard();
        _caretTimer = 0f;
        _caretVisible = true;
        _lastCompositionString = string.Empty;
        _wasComposingLastFrame = false;
        RefreshPreview();
        RefreshShiftLabels();

        var utcs = BeginResultWait();
        await ShowModalAnimatedSafelyAsync();

        if (EventSystem.current != null && _initialFocusButton != null)
        {
            EventSystem.current.SetSelectedGameObject(_initialFocusButton.gameObject);
        }

        return await utcs.Task;
    }

    protected override void Awake()
    {
        _autoAttachButtonSounds = false;
        base.Awake();
    }

    public override GameObject GetDefaultFocusTarget()
    {
        return _initialFocusButton != null ? _initialFocusButton.gameObject : null;
    }

    protected override void Update()
    {
        base.Update();
        if (!IsAwaitingResult || !gameObject.activeInHierarchy || !IsTopActiveView) return;

        UpdateCaretBlink();

        if (!CanProcessModalInput) return;

        string composition = Input.compositionString ?? string.Empty;
        bool isComposing = composition.Length > 0;
        if (isComposing)
        {
            UpdateImeCursorPosition();
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }
        if (composition != _lastCompositionString)
        {
            _lastCompositionString = composition;
            ResetCaretBlink();
            RefreshPreview();
        }

        bool suppressDialogControlKeys = isComposing || _wasComposingLastFrame;
        _wasComposingLastFrame = isComposing;

        if (LobbyUIInputHelper.WasCancelPressedThisFrame(allowKeyboardEscape: !suppressDialogControlKeys))
        {
            UISound.Play(UISoundType.Cancel);
            CloseDialog(confirmed: false);
            return;
        }

        if (HandlePhysicalKeyboardInput(suppressDialogControlKeys) || isComposing)
        {
            return;
        }

        RestoreFocusOnDirectionalInputIfNeeded();

        var gamepad = Gamepad.current;
        if (gamepad != null)
        {
            if (gamepad.buttonWest.wasPressedThisFrame)
            {
                UISound.Play(UISoundType.Enter);
                Backspace();
                return;
            }

            if (gamepad.buttonNorth.wasPressedThisFrame)
            {
                UISound.Play(UISoundType.Enter);
                AppendChar(" ");
                return;
            }

            if (gamepad.leftTrigger.wasPressedThisFrame)
            {
                UISound.Play(UISoundType.Enter);
                ToggleShift();
                return;
            }

            if (gamepad.rightTrigger.wasPressedThisFrame || gamepad.startButton.wasPressedThisFrame)
            {
                UISound.Play(UISoundType.Enter);
                CloseDialog(confirmed: true);
            }
        }
    }

    private bool HandlePhysicalKeyboardInput(bool suppressControlKeys)
    {
        string input = Input.inputString;
        if (string.IsNullOrEmpty(input))
        {
            return false;
        }

        bool hasSelection = EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null;

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (c == '\b')
            {
                if (suppressControlKeys) continue;
                if (hasSelection && EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                    hasSelection = false;
                }
                UISound.Play(UISoundType.Enter);
                Backspace();
                continue;
            }

            if (c == '\n' || c == '\r')
            {
                if (suppressControlKeys) continue;
                if (!hasSelection)
                {
                    UISound.Play(UISoundType.Enter);
                    CloseDialog(confirmed: true);
                    return true;
                }
                continue;
            }

            if (!char.IsControl(c))
            {
                if (hasSelection && EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                    hasSelection = false;
                }
                UISound.Play(UISoundType.Enter);
                AppendChar(c.ToString());
            }
        }

        return true;
    }

    private void RestoreFocusOnDirectionalInputIfNeeded()
    {
        if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject != null || _initialFocusButton == null)
        {
            return;
        }

        if (LobbyUIInputHelper.WasDirectionalInputPressedThisFrame())
        {
            EventSystem.current.SetSelectedGameObject(_initialFocusButton.gameObject);
        }
    }

    private void CloseDialog(bool confirmed)
    {
        CloseModalWithResultAsync((confirmed, _currentText), onBeforeHide: RestoreImeMode).Forget();
    }

    protected override void OnDestroy()
    {
        RestoreImeMode();
        base.OnDestroy();
    }

    private void EnableImeForPhysicalKeyboard()
    {
        if (!_imeEnabledByDialog)
        {
            _previousImeMode = Input.imeCompositionMode;
            _imeEnabledByDialog = true;
        }
        Input.imeCompositionMode = IMECompositionMode.On;
        UpdateImeCursorPosition();
    }

    private void RestoreImeMode()
    {
        if (!_imeEnabledByDialog) return;
        Input.imeCompositionMode = _previousImeMode;
        _imeEnabledByDialog = false;
        _lastCompositionString = string.Empty;
        _wasComposingLastFrame = false;
    }

    private void UpdateImeCursorPosition()
    {
        if (_previewText == null) return;
        Vector3 worldPos = _previewText.rectTransform.position;
        if (_cachedRootCanvas == null)
        {
            _cachedRootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
        }
        Camera cam = (_cachedRootCanvas != null && _cachedRootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? _cachedRootCanvas.worldCamera
            : null;
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
        Input.compositionCursorPos = new Vector2(screenPos.x, Screen.height - screenPos.y + 32f);
    }

    #region プレハブUIのバインド（見た目維持）

    private void BindExistingUI()
    {
        _letterButtons.Clear();
        _presetButtons.Clear();
        _initialFocusButton = null;

        Transform panel = transform.Find("KeyboardPanel");
        if (panel == null)
        {
            Debug.LogError("[LobbyVirtualKeyboardUI] プレハブ内に KeyboardPanel が見つかりません。");
            return;
        }

        Transform header = panel.Find("HeaderRow");
        if (header != null)
        {
            _titleText = header.Find("TitleText")?.GetComponent<Text>();
            _counterText = header.Find("CounterText")?.GetComponent<Text>();
        }

        Transform previewBox = panel.Find("PreviewBox");
        if (previewBox != null)
        {
            _previewText = previewBox.Find("PreviewText")?.GetComponent<Text>();
            if (_previewText != null)
            {
                _previewText.supportRichText = true;
                _previewBaseColor = _previewText.color;

                // 点滅のたびに ColorUtility.ToHtmlStringRGBA を生成しないよう事前キャッシュ
                string caretHex = ColorUtility.ToHtmlStringRGBA(_previewBaseColor);
                Color placeholderColor = new Color(_previewBaseColor.r, _previewBaseColor.g, _previewBaseColor.b, 0.55f);
                string placeholderHex = ColorUtility.ToHtmlStringRGBA(placeholderColor);
                _emptyCaretVisibleMarkup = $"<color=#{caretHex}>|</color><color=#{placeholderHex}>（未入力）</color>";
                _emptyCaretHiddenMarkup = $"<color=#00000000>|</color><color=#{placeholderHex}>（未入力）</color>";
            }
        }

        Transform presetRow = panel.Find("PresetRow");
        if (presetRow != null)
        {
            _presetRowObj = presetRow.gameObject;
            for (int i = 0; i < presetRow.childCount; i++)
            {
                Button btn = presetRow.GetChild(i).GetComponent<Button>();
                if (btn == null) continue;

                if (btn.name == "Preset_Random")
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(SetRandomRoomName);
                }
                else
                {
                    Text label = btn.GetComponentInChildren<Text>();
                    string defaultText = label != null ? label.text : btn.name.Replace("Preset_", string.Empty);
                    _presetButtons.Add((btn, label, defaultText));
                    BindPresetButton(btn, label, defaultText);
                }
            }
        }

        for (int rowIndex = 1; rowIndex <= 5; rowIndex++)
        {
            Transform rowTr = panel.Find($"KeyRow_{rowIndex}");
            if (rowTr == null) continue;

            for (int i = 0; i < rowTr.childCount; i++)
            {
                Button btn = rowTr.GetChild(i).GetComponent<Button>();
                if (btn == null) continue;

                BindKeyButton(btn);
                if (rowIndex == 2 && _initialFocusButton == null)
                {
                    _initialFocusButton = btn;
                }
            }
        }
    }

    private void BindPresetButton(Button btn, Text label, string presetText)
    {
        if (label != null)
        {
            label.text = presetText;
        }
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => SetPresetText(presetText));
    }

    private void BindKeyButton(Button btn)
    {
        btn.onClick.RemoveAllListeners();

        switch (btn.name)
        {
            case "Key_Shift":
                _shiftButtonLabel = btn.GetComponentInChildren<Text>();
                if (_shiftButtonLabel != null)
                {
                    _shiftBaseColor = _shiftButtonLabel.color;
                }
                btn.onClick.AddListener(ToggleShift);
                return;
            case "Key_Backspace":
                btn.onClick.AddListener(Backspace);
                return;
            case "Key_Clear":
                btn.onClick.AddListener(ClearAll);
                return;
            case "Key_Space":
                btn.onClick.AddListener(() => AppendChar(" "));
                return;
            case "Key_Cancel":
                btn.onClick.AddListener(() => CloseDialog(confirmed: false));
                return;
            case "Key_Confirm":
                btn.onClick.AddListener(() => CloseDialog(confirmed: true));
                return;
        }

        if (btn.name.StartsWith("Key_", StringComparison.Ordinal))
        {
            string key = btn.name.Substring("Key_".Length);
            if (key.Length == 1 && char.IsLetter(key[0]))
            {
                string lower = key.ToLowerInvariant();
                Text label = btn.GetComponentInChildren<Text>();
                _letterButtons.Add((btn, label, lower));
                btn.onClick.AddListener(() => AppendChar(_isShiftActive ? lower.ToUpperInvariant() : lower));
            }
            else if (!string.IsNullOrEmpty(key))
            {
                btn.onClick.AddListener(() => AppendChar(key));
            }
        }
    }

    #endregion

    #region パラメータ適用・ナビゲーション構築

    private void ApplyTitle(string title)
    {
        if (_titleText != null && !string.IsNullOrEmpty(title))
        {
            _titleText.text = title;
        }
    }

    private void ApplyPresetOptions(bool showPresets, string[] customPresets)
    {
        if (_presetRowObj == null) return;

        _presetRowObj.SetActive(showPresets);
        if (!showPresets) return;

        if (customPresets == null)
        {
            for (int i = 0; i < _presetButtons.Count; i++)
            {
                var (btn, label, defaultText) = _presetButtons[i];
                if (btn == null) continue;
                btn.gameObject.SetActive(true);
                BindPresetButton(btn, label, defaultText);
            }
            return;
        }

        for (int i = 0; i < _presetButtons.Count; i++)
        {
            var (btn, label, _) = _presetButtons[i];
            if (btn == null) continue;
            if (i < customPresets.Length && !string.IsNullOrEmpty(customPresets[i]))
            {
                string preset = customPresets[i];
                btn.gameObject.SetActive(true);
                BindPresetButton(btn, label, preset);
            }
            else
            {
                btn.gameObject.SetActive(false);
            }
        }
    }

    private void BuildExplicitNavigation()
    {
        _navigationRows.Clear();

        Transform panel = transform.Find("KeyboardPanel");
        if (panel == null) return;

        TryCollectRow(panel.Find("PresetRow"));
        for (int r = 1; r <= 5; r++)
        {
            TryCollectRow(panel.Find($"KeyRow_{r}"));
        }

        int rowCount = _navigationRows.Count;
        for (int r = 0; r < rowCount; r++)
        {
            List<Button> row = _navigationRows[r];
            int colCount = row.Count;
            if (colCount == 0) continue;

            List<Button> upRow = _navigationRows[(r - 1 + rowCount) % rowCount];
            List<Button> downRow = _navigationRows[(r + 1) % rowCount];

            for (int c = 0; c < colCount; c++)
            {
                float ratio = colCount > 1 ? (float)c / (colCount - 1) : 0f;
                int upIdx = upRow.Count > 1 ? Mathf.RoundToInt(ratio * (upRow.Count - 1)) : 0;
                int downIdx = downRow.Count > 1 ? Mathf.RoundToInt(ratio * (downRow.Count - 1)) : 0;

                row[c].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = row[(c - 1 + colCount) % colCount],
                    selectOnRight = row[(c + 1) % colCount],
                    selectOnUp = upRow[upIdx],
                    selectOnDown = downRow[downIdx]
                };
            }
        }
    }

    private void TryCollectRow(Transform rowTr)
    {
        if (rowTr == null || !rowTr.gameObject.activeSelf) return;

        var buttons = new List<Button>();
        for (int i = 0; i < rowTr.childCount; i++)
        {
            Transform child = rowTr.GetChild(i);
            if (!child.gameObject.activeSelf) continue;

            if (child.TryGetComponent<Button>(out var btn) && btn.interactable)
            {
                buttons.Add(btn);
            }
        }

        if (buttons.Count > 0)
        {
            _navigationRows.Add(buttons);
        }
    }

    #endregion

    #region 文字入力操作

    private void AppendChar(string ch)
    {
        if (string.IsNullOrEmpty(ch) || _currentText.Length >= _maxLength) return;

        _currentText += ch;
        ResetCaretBlink();
        RefreshPreview();
    }

    private void Backspace()
    {
        if (_currentText.Length == 0) return;

        _currentText = _currentText.Substring(0, _currentText.Length - 1);
        ResetCaretBlink();
        RefreshPreview();
    }

    private void ClearAll()
    {
        _currentText = string.Empty;
        ResetCaretBlink();
        RefreshPreview();
    }

    private void SetPresetText(string preset)
    {
        _currentText = TrimToMaxLength(preset);
        ResetCaretBlink();
        RefreshPreview();
    }

    private void SetRandomRoomName()
    {
        string generated = _randomTextGenerator != null
            ? _randomTextGenerator()
            : $"Room-{UnityEngine.Random.Range(1000, 10000)}";
        _currentText = TrimToMaxLength(generated);
        ResetCaretBlink();
        RefreshPreview();
    }

    private void ToggleShift()
    {
        _isShiftActive = !_isShiftActive;
        RefreshShiftLabels();
    }

    private void RefreshShiftLabels()
    {
        for (int i = 0; i < _letterButtons.Count; i++)
        {
            var (_, label, lowerChar) = _letterButtons[i];
            if (label != null)
            {
                label.text = _isShiftActive ? lowerChar.ToUpperInvariant() : lowerChar;
            }
        }

        if (_shiftButtonLabel != null)
        {
            _shiftButtonLabel.text = _isShiftActive ? "SHIFT ON" : "shift";
            _shiftButtonLabel.color = _isShiftActive ? _shiftBaseColor : new Color(_shiftBaseColor.r, _shiftBaseColor.g, _shiftBaseColor.b, 0.7f);
        }
    }

    private void UpdateCaretBlink()
    {
        _caretTimer += Time.unscaledDeltaTime;
        if (_caretTimer < CaretBlinkInterval) return;

        _caretTimer -= CaretBlinkInterval;
        _caretVisible = !_caretVisible;
        RefreshPreview();
    }

    private void ResetCaretBlink()
    {
        _caretTimer = 0f;
        _caretVisible = true;
    }

    private void RefreshPreview()
    {
        string composition = Input.compositionString ?? string.Empty;
        bool hasComposition = composition.Length > 0;

        if (_previewText != null)
        {
            if (string.IsNullOrEmpty(_currentText) && !hasComposition)
            {
                _previewText.supportRichText = true;
                _previewText.color = _previewBaseColor;
                _previewText.text = _caretVisible ? _emptyCaretVisibleMarkup : _emptyCaretHiddenMarkup;
            }
            else if (hasComposition)
            {
                string caret = _caretVisible ? "|" : string.Empty;
                if (!_currentText.Contains("<") && !composition.Contains("<"))
                {
                    _previewText.supportRichText = true;
                    _previewText.color = _previewBaseColor;
                    _previewText.text = $"{_currentText}<color=#5CEACCFF>{composition}</color>{caret}";
                }
                else
                {
                    _previewText.supportRichText = false;
                    _previewText.color = _previewBaseColor;
                    _previewText.text = $"{_currentText}[{composition}]{caret}";
                }
            }
            else
            {
                _previewText.supportRichText = false;
                _previewText.color = _previewBaseColor;
                _previewText.text = _caretVisible ? $"{_currentText}|" : _currentText;
            }
        }

        if (_counterText != null)
        {
            int displayLen = _currentText.Length + composition.Length;
            if (displayLen != _lastDisplayLen || _maxLength != _lastCounterMaxLength)
            {
                _lastDisplayLen = displayLen;
                _lastCounterMaxLength = _maxLength;
                _counterText.text = $"{displayLen} / {_maxLength}";
            }
        }
    }

    private string TrimToMaxLength(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= _maxLength ? value : value.Substring(0, _maxLength);
    }

    #endregion
}
