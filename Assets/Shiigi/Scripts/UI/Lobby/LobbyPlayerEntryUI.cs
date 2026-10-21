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

    public void Setup(IReadOnlyPlayer player, bool isHost)
    {
        // Relay接続が完了しているか確認（ホストは常に接続済み扱い）
        bool isRelayConnected = SessionPlayerPropertyHelper.IsPlayerRelayConnected(player, isHost);

        if (!isRelayConnected)
        {
            _playerNameText.text = "接続中...";
            _statusImage.color = _waitingColor;
            return;
        }

        // プレイヤーIDの簡易表示(名前システム作るまでのプレースホルダ)
        string playerName = player.Id;
        if (playerName.Length > 8)
        {
            playerName = playerName.Substring(0, 8) + "...";
        }
        _playerNameText.text = playerName;

        // 準備状況の取得（ドメインヘルパーへ委譲）
        var isReady = SessionPlayerPropertyHelper.IsPlayerReady(player);

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