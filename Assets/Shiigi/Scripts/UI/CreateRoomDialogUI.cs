using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.Multiplayer;
using UnityEngine;
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

public class CreateRoomDialogUI : MonoBehaviour
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

    /// <summary>
    /// ダイアログを表示し、ユーザーが決定またはキャンセルするまで待機
    /// </summary>
    public async UniTask<CreateRoomResult> ShowAsync()
    {
        EnsureBlocker();

        // ダイアログを表示
        gameObject.SetActive(true);

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
            gameObject.SetActive(false);
            _utcs.TrySetResult(new CreateRoomResult
            {
                IsCanceled = false,
                RoomName = _roomNameInputField.text,
                MaxPlayers = GetMaxPlayersFromDropdown(_maxPlayersDropDown.value),
                AdvancedSettings = new RoomAdvancedSettings
                {
                    Protocol = GetProtocolFromDropdown(_protocolDropdown != null ? _protocolDropdown.value : 0)
                }
            });
        });

        // キャンセルボタンが押されたとき
        _cancelDialogButton.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
            _utcs.TrySetResult(new CreateRoomResult { IsCanceled = true });
        });

        return await _utcs.Task;
    }

    private void ToggleAdvancedSettings()
    {
        if (_advancedSettingsPanel == null) return;

        _advancedSettingsPanel.SetActive(!_advancedSettingsPanel.activeSelf);
        if (_advancedSettingsPanel.transform.parent is RectTransform parentRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
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
