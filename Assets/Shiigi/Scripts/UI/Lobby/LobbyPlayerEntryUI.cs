using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayerEntryUI : MonoBehaviour
{
    [SerializeField] private Text _playerNameText;
    [SerializeField] private Image _statusImage;

    [SerializeField] private Color _hostColor;
    [SerializeField] private Color _readyColor;
    [SerializeField] private Color _waitingColor;

    public void Setup(IPlayer player, bool isHost)
    {
        // プレイヤーIDの簡易表示(名前システム作るまでのプレースホルダ)
        string playerName = player.Id;
        if (playerName.Length > 8)
        {
            playerName = playerName.Substring(0, 8) + "...";
        }
        _playerNameText.text = playerName;

        // 準備状況の取得
        var isReady = false;
        if (player.Properties != null && player.Properties.TryGetValue("IsReady", out var prop))
        {
            isReady = prop.Value == "true";
        }

        if (isHost)
        {
            _statusImage.color = _hostColor;
        }
        else
        {
            _statusImage.color = isReady ? _readyColor : _waitingColor;
        }
    }
}
