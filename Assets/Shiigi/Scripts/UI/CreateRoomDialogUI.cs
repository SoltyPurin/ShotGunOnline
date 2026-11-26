using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
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

// ダイアログから返却される結果のデータ
public struct CreateRoomResult
{
    public bool IsCanceled; // キャンセルされたかどうか
    public string RoomName; // 入力されたルーム名
    public int MaxPlayers; // 選択された最大人数
    public RoomAdvancedSettings AdvancedSettings; // 詳細設定（通信プロトコルなど）

    public RelayProtocol Protocol => AdvancedSettings.Protocol;
}

public class CreateRoomDialogUI : LobbyViewBase
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

    private static readonly (string Label, RelayProtocol Protocol)[] _protocolOptions =
    {
        ("DTLS", RelayProtocol.DTLS),
        ("UDP", RelayProtocol.UDP),
        ("WSS(TCP)", RelayProtocol.WSS)
    };

    private UniTaskCompletionSource<CreateRoomResult> _utcs;
    private GameObject _blocker;
    private bool _isOpeningVirtualKeyboard;

    private bool ShouldUseVirtualKeyboard => _enableVirtualKeyboard && LobbyInputDeviceTracker.ShouldUseVirtualKeyboard;

    protected override void Awake()
    {
        _viewType = UIViewType.ModalDialog;
        base.Awake();

        LobbyInputDeviceTracker.EnsureInitialized();
        LobbyInputDeviceTracker.OnInputModeChanged += HandleInputModeChanged;

        EnsureButtonSound(_confirmCreateButton, UIButtonType.Default);
        EnsureButtonSound(_cancelDialogButton, UIButtonType.Cancel);
        EnsureButtonSound(_advancedSettingsButton, UIButtonType.SubAction);

        UIButtonMetallicVisual.Attach(_confirmCreateButton);
        UIButtonMetallicVisual.Attach(_cancelDialogButton);
        UIButtonMetallicVisual.Attach(_advancedSettingsButton);

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
        if (_utcs == null || !gameObject.activeInHierarchy || !IsTopActiveView || _isOpeningVirtualKeyboard) return;

        // コントローラー操作時かつ仮想キーボード有効時のみ、ルーム名欄の選択＋決定で仮想キーボードを開く
        if (ShouldUseVirtualKeyboard && _roomNameInputField != null)
        {
            bool isRoomNameSelected =
                EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject == _roomNameInputField.gameObject;

            bool openRequestedBySubmit =
                isRoomNameSelected &&
                (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);

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
            EnsureDropdownListAutoFollower(openDropdown);

            bool closeDropdown =
                (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame) ||
                (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame);

            if (closeDropdown)
            {
                UISound.Play(UISoundType.Cancel);
                openDropdown.Hide();
                if (EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(openDropdown.gameObject);
                }
            }
            return;
        }

        bool cancelPressed =
            (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame) ||
            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame);

        if (cancelPressed)
        {
            UISound.Play(UISoundType.Cancel);
            CloseAndSetResult(new CreateRoomResult { IsCanceled = true }).Forget();
        }
    }

    private Dropdown GetOpenDropdown()
    {
        if (_maxPlayersDropDown != null && _maxPlayersDropDown.transform.Find("Dropdown List") != null)
        {
            return _maxPlayersDropDown;
        }
        if (_protocolDropdown != null && _protocolDropdown.transform.Find("Dropdown List") != null)
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
        if (scrollRect != null && scrollRect.GetComponent<ScrollRectAutoFollower>() == null)
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

    private static void EnsureButtonSound(Button button, UIButtonType buttonType)
    {
        if (button == null) return;
        var sound = button.GetComponent<UIButtonSound>();
        if (sound == null)
        {
            sound = button.gameObject.AddComponent<UIButtonSound>();
        }
        sound.ButtonType = buttonType;
    }

    /// <summary>
    /// ダイアログを表示し、ユーザーが決定またはキャンセルするまで待機
    /// </summary>
    public async UniTask<CreateRoomResult> ShowAsync()
    {
        EnsureBlocker();

        // UI初期化
        _roomNameInputField.text = "";
        ApplyRoomNameInputMode();

        // ドロップダウン初期化
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

        // イベントの多重登録を防ぐため、一度クリアして再アタッチ
        _confirmCreateButton.onClick.RemoveAllListeners();
        _cancelDialogButton.onClick.RemoveAllListeners();
        if (_advancedSettingsButton != null)
        {
            _advancedSettingsButton.onClick.RemoveAllListeners();
            _advancedSettingsButton.onClick.AddListener(ToggleAdvancedSettings);
        }

        _utcs?.TrySetCanceled();
        _utcs = new UniTaskCompletionSource<CreateRoomResult>();
        var utcs = _utcs;

        // 決定ボタンが押されたとき
        _confirmCreateButton.onClick.AddListener(() =>
        {
            CloseAndSetResult(new CreateRoomResult
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

        // キャンセルボタンが押されたとき
        _cancelDialogButton.onClick.AddListener(() =>
        {
            CloseAndSetResult(new CreateRoomResult { IsCanceled = true }).Forget();
        });

        // フェード＆ポップアニメーションを表示（演出がキャンセルされても結果待ちは継続する）
        try
        {
            await base.ShowAsync();
        }
        catch (OperationCanceledException)
        {
            // 表示演出のキャンセルは無視し、結果の確定を待つ
        }

        if (EventSystem.current != null)
        {
            GameObject initialFocus = LobbyInputDeviceTracker.ShouldUseVirtualKeyboard && _confirmCreateButton != null
                ? _confirmCreateButton.gameObject
                : (_roomNameInputField != null ? _roomNameInputField.gameObject : _confirmCreateButton?.gameObject);
            if (initialFocus != null)
            {
                EventSystem.current.SetSelectedGameObject(initialFocus);
            }
        }

        return await utcs.Task;
    }

    private async UniTaskVoid CloseAndSetResult(CreateRoomResult result)
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
            utcs.TrySetResult(result);
        }
    }

    protected override void OnDestroy()
    {
        LobbyInputDeviceTracker.OnInputModeChanged -= HandleInputModeChanged;
        _utcs?.TrySetCanceled();
        _utcs = null;
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
            var nav = new Navigation { mode = Navigation.Mode.Explicit };
            nav.selectOnDown = _maxPlayersDropDown != null ? (Selectable)_maxPlayersDropDown : _advancedSettingsButton;
            nav.selectOnUp = bottomPrimary;
            _roomNameInputField.navigation = nav;
        }

        if (_maxPlayersDropDown != null)
        {
            var nav = new Navigation { mode = Navigation.Mode.Explicit };
            nav.selectOnUp = _roomNameInputField;
            nav.selectOnDown = _advancedSettingsButton != null ? (Selectable)_advancedSettingsButton : bottomPrimary;
            _maxPlayersDropDown.navigation = nav;
        }

        if (_advancedSettingsButton != null)
        {
            var nav = new Navigation { mode = Navigation.Mode.Explicit };
            nav.selectOnUp = _maxPlayersDropDown != null ? (Selectable)_maxPlayersDropDown : _roomNameInputField;
            nav.selectOnDown = afterAdvanced;
            _advancedSettingsButton.navigation = nav;
        }

        if (protocolVisible)
        {
            var nav = new Navigation { mode = Navigation.Mode.Explicit };
            nav.selectOnUp = _advancedSettingsButton;
            nav.selectOnDown = bottomPrimary;
            _protocolDropdown.navigation = nav;
        }

        Selectable aboveBottom = protocolVisible
            ? (Selectable)_protocolDropdown
            : (_advancedSettingsButton != null ? (Selectable)_advancedSettingsButton : _maxPlayersDropDown);

        if (_confirmCreateButton != null)
        {
            var nav = new Navigation { mode = Navigation.Mode.Explicit };
            nav.selectOnUp = aboveBottom;
            nav.selectOnDown = _roomNameInputField;
            nav.selectOnLeft = _cancelDialogButton;
            nav.selectOnRight = _cancelDialogButton;
            _confirmCreateButton.navigation = nav;
        }

        if (_cancelDialogButton != null)
        {
            var nav = new Navigation { mode = Navigation.Mode.Explicit };
            nav.selectOnUp = aboveBottom;
            nav.selectOnDown = _roomNameInputField;
            nav.selectOnLeft = _confirmCreateButton;
            nav.selectOnRight = _confirmCreateButton;
            _cancelDialogButton.navigation = nav;
        }
    }

    private void ApplyRoomNameInputMode()
    {
        if (_roomNameInputField == null) return;

        // コントローラー操作時（仮想キーボード有効）のみ直接入力を抑制し、キーボード／マウス操作時は直接入力を許可する
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
            Font font = _roomNameInputField.textComponent != null ? _roomNameInputField.textComponent.font : null;
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

    /// <summary>
    /// 最大人数ドロップダウンの選択肢を設定する
    /// </summary>
    private void SetMaxPlayersDropdownOptions()
    {
        _maxPlayersDropDown.ClearOptions();

        // ドロップダウンの選択肢を最大人数リストから作成
        var options = _maxPlayerOptions.Select(maxPlayers => maxPlayers.ToString()).ToList();
        _maxPlayersDropDown.AddOptions(options);
    }

    /// <summary>
    /// 通信プロトコルドロップダウンの選択肢を設定する
    /// </summary>
    private void SetProtocolDropdownOptions()
    {
        if (_protocolDropdown == null) return;

        _protocolDropdown.ClearOptions();
        var options = _protocolOptions.Select(option => option.Label).ToList();
        _protocolDropdown.AddOptions(options);
    }

    /// <summary>
    /// ドロップダウンの項目に合わせて人数を返す
    /// </summary>
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

    /// <summary>
    /// ドロップダウンの項目に合わせて通信プロトコルを返す
    /// </summary>
    private RelayProtocol GetProtocolFromDropdown(int index)
    {
        if (index < 0 || index >= _protocolOptions.Length)
        {
            return RelayProtocol.DTLS;
        }
        return _protocolOptions[index].Protocol;
    }
}
