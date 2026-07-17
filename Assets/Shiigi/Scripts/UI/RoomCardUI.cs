using System;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

public class RoomCardUI : MonoBehaviour
{
    [Header("テキスト")] [SerializeField] private Text _roomNameText;
    [SerializeField] private Text _playerCountText;

    [Header("ボタン")] [SerializeField] private Button _joinButton;

    private ISessionInfo _sessionInfo;

    // 参加ボタンが押されたことを通知するイベント
    public event Action<ISessionInfo> OnJoinRequested;

    private void Awake()
    {
        _joinButton.onClick.AddListener(() => OnJoinRequested?.Invoke(_sessionInfo));
    }

    /// <summary>
    /// セッション情報を元にカードの表示を更新する
    /// </summary>
    public void Setup(ISessionInfo sessionInfo)
    {
        _sessionInfo = sessionInfo;
        _roomNameText.text = sessionInfo.Name;
        var currentPlayerCount = sessionInfo.MaxPlayers - sessionInfo.AvailableSlots;
        _playerCountText.text = $"{currentPlayerCount} / {sessionInfo.MaxPlayers}";

        // 満員の場合は参加ボタンを押せなくする
        _joinButton.interactable = sessionInfo.AvailableSlots > 0;
    }
}
