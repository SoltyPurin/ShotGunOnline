using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Unity.Services.Lobbies;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

// ダイアログから返却される結果のデータ
public struct CreateRoomResult
{
    public bool IsCanceled; // キャンセルされたかどうか
    public string RoomName; // 入力されたルーム名
    public int MaxPlayers; // 選択された最大人数
}

public class CreateRoomDialogUI : MonoBehaviour
{
    [Header("ルーム作成用UI")] [SerializeField] private InputField _roomNameInputField;
    [SerializeField] private Dropdown _maxPlayersDropDown;
    [SerializeField] private Button _confirmCreateButton;
    [SerializeField] private Button _cancelDialogButton;

    [SerializeField][Range(2,8)] private List<int> _maxPlayerOptions = new() { 2, 3, 4 };

    private UniTaskCompletionSource<CreateRoomResult> _utcs;

    /// <summary>
    /// ダイアログを表示し、ユーザーが決定またはキャンセルするまで待機
    /// </summary>
    public async UniTask<CreateRoomResult> ShowAsync()
    {
        // ダイアログを表示
        gameObject.SetActive(true);

        // UI初期化
        _roomNameInputField.text = "";

        // ドロップダウン
        SetMaxPlayersDropdownOptions();
        _maxPlayersDropDown.value = 0;

        // イベントの多重登録を防ぐため、一度クリアして再アタッチ
        _confirmCreateButton.onClick.RemoveAllListeners();
        _cancelDialogButton.onClick.RemoveAllListeners();

        _utcs = new UniTaskCompletionSource<CreateRoomResult>();

        // 決定ボタンが押されたとき
        _confirmCreateButton.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
            _utcs.TrySetResult(new CreateRoomResult
            {
                IsCanceled = false,
                RoomName = _roomNameInputField.text,
                MaxPlayers = GetMaxPlayersFromDropdown(_maxPlayersDropDown.value)
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
    /// ドロップダウンの項目に合わせて人数を返す
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    private int GetMaxPlayersFromDropdown(int index)
    {
        return _maxPlayerOptions[index];
    }
}
