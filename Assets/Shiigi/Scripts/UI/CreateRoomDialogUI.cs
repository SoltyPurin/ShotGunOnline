using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
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
    /// ドロップダウンの項目に合わせて人数を返す
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    private int GetMaxPlayersFromDropdown(int index)
    {
        return index switch
        {
            0 => 2,
            1 => 3,
            2 => 4,
            _ => 4
        };
    }
}
