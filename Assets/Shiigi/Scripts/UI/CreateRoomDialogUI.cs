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

    private static readonly (string Label, RelayProtocol Protocol)[] _protocolOptions =
    {
        ("DTLS", RelayProtocol.DTLS),
        ("UDP", RelayProtocol.UDP),
        ("WSS(TCP)", RelayProtocol.WSS)
    };

    private UniTaskCompletionSource<CreateRoomResult> _utcs;
    private GameObject _blocker;

    protected override void Awake()
    {
        _viewType = UIViewType.ModalDialog;
        base.Awake();

        EnsureButtonSound(_confirmCreateButton, UIButtonType.Default);
        EnsureButtonSound(_cancelDialogButton, UIButtonType.Cancel);
        EnsureButtonSound(_advancedSettingsButton, UIButtonType.SubAction);

        LobbyModernTheme.AttachMetallicVisual(_confirmCreateButton);
        LobbyModernTheme.AttachMetallicVisual(_cancelDialogButton);
        LobbyModernTheme.AttachMetallicVisual(_advancedSettingsButton);

        if (_roomNameInputField != null)
        {
            // フォーカスしただけでは入力開始せず、決定(Enter/A/クリック)で入力開始にする
            _roomNameInputField.shouldActivateOnSelect = false;
        }

        RebuildNavigation();
    }

    private void Update()
    {
        if (_utcs == null || !gameObject.activeInHierarchy || !IsTopActiveView) return;

        // ルーム名「編集中」のときだけ、Esc/Bで編集終了、Tabで次項目へ移動
        // ※日本語IME変換中の Enter / Esc で入力欄が閉じないよう、Enterでは終了せず IME確定文字列も確認する
        if (_roomNameInputField != null && _roomNameInputField.isFocused)
        {
            if (!string.IsNullOrEmpty(Input.compositionString))
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                _roomNameInputField.DeactivateInputField();
                GameObject nextTarget = _maxPlayersDropDown != null && _maxPlayersDropDown.gameObject.activeInHierarchy
                    ? _maxPlayersDropDown.gameObject
                    : (_confirmCreateButton != null ? _confirmCreateButton.gameObject : null);
                if (nextTarget != null && EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(nextTarget);
                }
                return;
            }

            bool finishEdit =
                (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);

            if (finishEdit)
            {
                _roomNameInputField.DeactivateInputField();
                return;
            }

            // 編集中はダイアログのキャンセル判定へ進まない
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

    public override GameObject GetDefaultFocusTarget()
    {
        if (Gamepad.current != null && _confirmCreateButton != null && _confirmCreateButton.gameObject.activeInHierarchy)
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

        _utcs = new UniTaskCompletionSource<CreateRoomResult>();

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

        // フェード＆ポップアニメーションを表示
        await base.ShowAsync();

        if (EventSystem.current != null)
        {
            GameObject initialFocus = Gamepad.current != null && _confirmCreateButton != null
                ? _confirmCreateButton.gameObject
                : (_roomNameInputField != null ? _roomNameInputField.gameObject : _confirmCreateButton?.gameObject);
            if (initialFocus != null)
            {
                EventSystem.current.SetSelectedGameObject(initialFocus);
            }
        }

        return await _utcs.Task;
    }

    private async UniTaskVoid CloseAndSetResult(CreateRoomResult result)
    {
        if (_blocker != null)
        {
            _blocker.SetActive(false);
        }
        await HideAsync();
        _utcs?.TrySetResult(result);
    }

    private void ToggleAdvancedSettings()
    {
        if (_advancedSettingsPanel == null) return;

        _advancedSettingsPanel.SetActive(!_advancedSettingsPanel.activeSelf);
        if (_advancedSettingsPanel.transform.parent is RectTransform parentRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        }
        RebuildNavigation();
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
