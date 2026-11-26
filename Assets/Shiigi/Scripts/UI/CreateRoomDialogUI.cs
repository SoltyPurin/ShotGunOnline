using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// ルーム作成時の詳細設定データ
/// </summary>
[Serializable]
public struct RoomAdvancedSettings
{
    public RelayProtocol Protocol;

    public static RoomAdvancedSettings Default => new RoomAdvancedSettings
    {
        Protocol = RelayProtocol.DTLS
    };
}

/// <summary>
/// ダイアログから返却される結果のデータ
/// </summary>
public struct CreateRoomResult
{
    public bool IsCanceled;
    public string RoomName;
    public int MaxPlayers;
    public RoomAdvancedSettings AdvancedSettings;

    public RelayProtocol Protocol => AdvancedSettings.Protocol;
}

public class CreateRoomDialogUI : LobbyModalDialogBase<CreateRoomResult>
{
    [Header("ルーム作成用UI")]
    [SerializeField] private InputField _roomNameInputField;
    [SerializeField] private Dropdown _maxPlayersDropDown;
    [SerializeField] private Button _confirmCreateButton;
    [SerializeField] private Button _cancelDialogButton;
    [SerializeField] private Color _blockerColor = new Color(0f, 0f, 0f, 0.5f);

    [SerializeField][Range(2, 8)] private List<int> _maxPlayerOptions = new() { 2, 3, 4 };

    [Header("詳細設定UI")]
    [SerializeField] private Button _advancedSettingsButton;
    [SerializeField] private GameObject _advancedSettingsPanel;
    [SerializeField] private Dropdown _protocolDropdown;
    [SerializeField] private RectTransform _advancedArrowIndicator;

    [Header("仮想キーボード")]
    [SerializeField] private bool _enableVirtualKeyboard;
    [SerializeField] private LobbyVirtualKeyboardUI _virtualKeyboardPrefab;

    public LobbyVirtualKeyboardUI VirtualKeyboardPrefab => _virtualKeyboardPrefab;

    private static readonly (string Label, RelayProtocol Protocol)[] ProtocolOptions =
    {
        ("DTLS", RelayProtocol.DTLS),
        ("UDP", RelayProtocol.UDP),
        ("WSS(TCP)", RelayProtocol.WSS)
    };

    private bool _isOpeningVirtualKeyboard;
    private Dropdown _lastAutoFollowerAttachedDropdown;

    private bool ShouldUseVirtualKeyboard => _enableVirtualKeyboard && LobbyInputDeviceTracker.ShouldUseVirtualKeyboard;

    protected override void Awake()
    {
        base.Awake();

        LobbyInputDeviceTracker.EnsureInitialized();
        LobbyInputDeviceTracker.OnInputModeChanged += HandleInputModeChanged;

        UIButtonSound.ConfigureButton(_confirmCreateButton, UIButtonType.Default);
        UIButtonSound.ConfigureButton(_cancelDialogButton, UIButtonType.Cancel);
        UIButtonSound.ConfigureButton(_advancedSettingsButton, UIButtonType.SubAction);

        if (_roomNameInputField != null && _roomNameInputField.textComponent != null)
        {
            _roomNameInputField.textComponent.supportRichText = false;
        }

        ApplyRoomNameInputMode();
        RebuildNavigation();
    }

    protected override void Update()
    {
        base.Update();
        if (!IsAwaitingResult || !gameObject.activeInHierarchy || !IsTopActiveView || _isOpeningVirtualKeyboard) return;

        if (ShouldUseVirtualKeyboard && _roomNameInputField != null)
        {
            bool isRoomNameSelected =
                EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject == _roomNameInputField.gameObject;

            bool openRequestedBySubmit =
                isRoomNameSelected &&
                Gamepad.current != null &&
                Gamepad.current.buttonSouth.wasPressedThisFrame;

            if (_roomNameInputField.isFocused || openRequestedBySubmit)
            {
                _roomNameInputField.DeactivateInputField();
                OpenVirtualKeyboardAsync().Forget();
                return;
            }
        }

        // キーボード直接入力中は Escape で入力欄のフォーカス解除のみ行い、ダイアログ自体は閉じない
        if (!ShouldUseVirtualKeyboard && _roomNameInputField != null && _roomNameInputField.isFocused)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                _roomNameInputField.DeactivateInputField();
            }
            return;
        }

        Dropdown openDropdown = GetOpenDropdown();
        if (openDropdown != null)
        {
            if (_lastAutoFollowerAttachedDropdown != openDropdown)
            {
                _lastAutoFollowerAttachedDropdown = openDropdown;
                EnsureDropdownListAutoFollower(openDropdown);
            }

            if (LobbyUIInputHelper.WasCancelPressedThisFrame())
            {
                UISound.Play(UISoundType.Cancel);
                openDropdown.Hide();
                _lastAutoFollowerAttachedDropdown = null;
                if (EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(openDropdown.gameObject);
                }
            }
            return;
        }
        _lastAutoFollowerAttachedDropdown = null;

        if (LobbyUIInputHelper.WasCancelPressedThisFrame())
        {
            UISound.Play(UISoundType.Cancel);
            CloseModalWithResultAsync(new CreateRoomResult { IsCanceled = true }).Forget();
        }
    }

    private Dropdown GetOpenDropdown()
    {
        // Dropdown展開時は子要素数が通常より増えるため、子要素数チェックで不要な名前検索を抑止
        if (_maxPlayersDropDown != null &&
            _maxPlayersDropDown.transform.childCount > 3 &&
            _maxPlayersDropDown.transform.Find("Dropdown List") != null)
        {
            return _maxPlayersDropDown;
        }
        if (_protocolDropdown != null &&
            _protocolDropdown.gameObject.activeInHierarchy &&
            _protocolDropdown.transform.childCount > 3 &&
            _protocolDropdown.transform.Find("Dropdown List") != null)
        {
            return _protocolDropdown;
        }
        return null;
    }

    private static void EnsureDropdownListAutoFollower(Dropdown dropdown)
    {
        if (dropdown == null) return;
        Transform listTransform = dropdown.transform.Find("Dropdown List");
        if (listTransform == null) return;

        var scrollRect = listTransform.GetComponentInChildren<ScrollRect>(true);
        if (scrollRect != null && !scrollRect.TryGetComponent<ScrollRectAutoFollower>(out _))
        {
            scrollRect.gameObject.AddComponent<ScrollRectAutoFollower>();
        }
    }

    public override GameObject GetDefaultFocusTarget()
    {
        if (LobbyInputDeviceTracker.ShouldUseVirtualKeyboard && _confirmCreateButton != null && _confirmCreateButton.gameObject.activeInHierarchy)
        {
            return _confirmCreateButton.gameObject;
        }
        if (_roomNameInputField != null && _roomNameInputField.gameObject.activeInHierarchy)
        {
            return _roomNameInputField.gameObject;
        }
        return _confirmCreateButton != null ? _confirmCreateButton.gameObject : null;
    }

    /// <summary>
    /// ダイアログを表示し、ユーザーが決定またはキャンセルするまで待機
    /// </summary>
    public async UniTask<CreateRoomResult> ShowAsync()
    {
        EnsureModalBlocker(_blockerColor);

        _roomNameInputField.text = string.Empty;
        ApplyRoomNameInputMode();

        SetMaxPlayersDropdownOptions();
        _maxPlayersDropDown.value = 0;

        SetProtocolDropdownOptions();
        if (_protocolDropdown != null)
        {
            _protocolDropdown.value = 0;
        }

        if (_advancedSettingsPanel != null)
        {
            _advancedSettingsPanel.SetActive(false);
            UpdateAdvancedArrowVisual(false);
            if (_advancedSettingsPanel.transform.parent is RectTransform parentRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
            }
        }
        RebuildNavigation();

        _confirmCreateButton.onClick.RemoveAllListeners();
        _cancelDialogButton.onClick.RemoveAllListeners();
        if (_advancedSettingsButton != null)
        {
            _advancedSettingsButton.onClick.RemoveAllListeners();
            _advancedSettingsButton.onClick.AddListener(ToggleAdvancedSettings);
        }

        var utcs = BeginResultWait();

        _confirmCreateButton.onClick.AddListener(() =>
        {
            CloseModalWithResultAsync(new CreateRoomResult
            {
                IsCanceled = false,
                RoomName = _roomNameInputField.text,
                MaxPlayers = GetMaxPlayersFromDropdown(_maxPlayersDropDown.value),
                AdvancedSettings = new RoomAdvancedSettings
                {
                    Protocol = GetProtocolFromDropdown(_protocolDropdown != null ? _protocolDropdown.value : 0)
                }
            }).Forget();
        });

        _cancelDialogButton.onClick.AddListener(() =>
        {
            CloseModalWithResultAsync(new CreateRoomResult { IsCanceled = true }).Forget();
        });

        await ShowModalAnimatedSafelyAsync();

        if (EventSystem.current != null)
        {
            GameObject initialFocus = GetDefaultFocusTarget();
            if (initialFocus != null)
            {
                EventSystem.current.SetSelectedGameObject(initialFocus);
            }
        }

        return await utcs.Task;
    }

    protected override void OnDestroy()
    {
        LobbyInputDeviceTracker.OnInputModeChanged -= HandleInputModeChanged;
        base.OnDestroy();
    }

    private void HandleInputModeChanged(bool usingGamepad)
    {
        ApplyRoomNameInputMode();
    }

    private void ToggleAdvancedSettings()
    {
        if (_advancedSettingsPanel == null) return;

        bool nextActive = !_advancedSettingsPanel.activeSelf;
        _advancedSettingsPanel.SetActive(nextActive);
        UpdateAdvancedArrowVisual(nextActive);
        if (_advancedSettingsPanel.transform.parent is RectTransform parentRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        }
        RebuildNavigation();
    }

    private void UpdateAdvancedArrowVisual(bool isExpanded)
    {
        if (_advancedArrowIndicator == null) return;
        _advancedArrowIndicator.localEulerAngles = new Vector3(0f, 0f, isExpanded ? 180f : 0f);
    }

    private void RebuildNavigation()
    {
        bool protocolVisible = _advancedSettingsPanel != null && _advancedSettingsPanel.activeSelf && _protocolDropdown != null;
        Selectable bottomPrimary = _confirmCreateButton != null ? (Selectable)_confirmCreateButton : _cancelDialogButton;
        Selectable afterAdvanced = protocolVisible ? (Selectable)_protocolDropdown : bottomPrimary;

        if (_roomNameInputField != null)
        {
            _roomNameInputField.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnDown = _maxPlayersDropDown != null ? (Selectable)_maxPlayersDropDown : _advancedSettingsButton,
                selectOnUp = bottomPrimary
            };
        }

        if (_maxPlayersDropDown != null)
        {
            _maxPlayersDropDown.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = _roomNameInputField,
                selectOnDown = _advancedSettingsButton != null ? (Selectable)_advancedSettingsButton : bottomPrimary
            };
        }

        if (_advancedSettingsButton != null)
        {
            _advancedSettingsButton.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = _maxPlayersDropDown != null ? (Selectable)_maxPlayersDropDown : _roomNameInputField,
                selectOnDown = afterAdvanced
            };
        }

        if (protocolVisible)
        {
            _protocolDropdown.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = _advancedSettingsButton,
                selectOnDown = bottomPrimary
            };
        }

        Selectable aboveBottom = protocolVisible
            ? (Selectable)_protocolDropdown
            : (_advancedSettingsButton != null ? (Selectable)_advancedSettingsButton : _maxPlayersDropDown);

        if (_confirmCreateButton != null)
        {
            _confirmCreateButton.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = aboveBottom,
                selectOnDown = _roomNameInputField,
                selectOnLeft = _cancelDialogButton,
                selectOnRight = _cancelDialogButton
            };
        }

        if (_cancelDialogButton != null)
        {
            _cancelDialogButton.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = aboveBottom,
                selectOnDown = _roomNameInputField,
                selectOnLeft = _confirmCreateButton,
                selectOnRight = _confirmCreateButton
            };
        }
    }

    private void ApplyRoomNameInputMode()
    {
        if (_roomNameInputField == null) return;

        bool useVirtualKeyboard = ShouldUseVirtualKeyboard;
        _roomNameInputField.shouldActivateOnSelect = !useVirtualKeyboard;
        _roomNameInputField.readOnly = useVirtualKeyboard;
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            ApplyRoomNameInputMode();
        }
    }

    private async UniTaskVoid OpenVirtualKeyboardAsync()
    {
        if (!_enableVirtualKeyboard || _isOpeningVirtualKeyboard || _roomNameInputField == null) return;

        _isOpeningVirtualKeyboard = true;
        try
        {
            _roomNameInputField.DeactivateInputField();
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            int maxLength = _roomNameInputField.characterLimit > 0 ? _roomNameInputField.characterLimit : 20;

            var (confirmed, text) = await LobbyVirtualKeyboardUI.OpenAsync(
                parentCanvas,
                initialText: _roomNameInputField.text,
                title: "ルーム名を入力",
                maxLength: maxLength,
                prefab: _virtualKeyboardPrefab,
                showPresets: true);

            if (confirmed && _roomNameInputField != null)
            {
                _roomNameInputField.text = text;
            }
        }
        finally
        {
            _isOpeningVirtualKeyboard = false;
            if (_roomNameInputField != null && EventSystem.current != null && gameObject.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(_roomNameInputField.gameObject);
            }
        }
    }

    private void SetMaxPlayersDropdownOptions()
    {
        _maxPlayersDropDown.ClearOptions();
        var options = new List<string>(_maxPlayerOptions.Count);
        for (int i = 0; i < _maxPlayerOptions.Count; i++)
        {
            options.Add(_maxPlayerOptions[i].ToString());
        }
        _maxPlayersDropDown.AddOptions(options);
    }

    private void SetProtocolDropdownOptions()
    {
        if (_protocolDropdown == null) return;

        _protocolDropdown.ClearOptions();
        var options = new List<string>(ProtocolOptions.Length);
        for (int i = 0; i < ProtocolOptions.Length; i++)
        {
            options.Add(ProtocolOptions[i].Label);
        }
        _protocolDropdown.AddOptions(options);
    }

    private int GetMaxPlayersFromDropdown(int index)
    {
        if (_maxPlayerOptions == null || _maxPlayerOptions.Count == 0)
        {
            return 4;
        }
        if (index < 0 || index >= _maxPlayerOptions.Count)
        {
            return _maxPlayerOptions[0];
        }
        return _maxPlayerOptions[index];
    }

    private static RelayProtocol GetProtocolFromDropdown(int index)
    {
        if (index < 0 || index >= ProtocolOptions.Length)
        {
            return RelayProtocol.DTLS;
        }
        return ProtocolOptions[index].Protocol;
    }
}
