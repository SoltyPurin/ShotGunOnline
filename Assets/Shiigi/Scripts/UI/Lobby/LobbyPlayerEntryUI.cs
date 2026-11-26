using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayerEntryUI : MonoBehaviour
{
    [SerializeField] private Text _playerNameText;
    [SerializeField] private Image _statusImage;
    [SerializeField] private Text _statusLabelText;
    [SerializeField] private Text _roleTagText;
    [SerializeField] private Graphic _leftSpineImage;
    [SerializeField] private Image _portraitImage;
    [SerializeField] private Sprite[] _skinPortraits;

    [SerializeField] private Color _hostColor = new Color(0.98f, 0.83f, 0.43f, 1f);
    [SerializeField] private Color _readyColor = new Color(0.36f, 0.92f, 0.80f, 1f);
    [SerializeField] private Color _waitingColor = new Color(0.83f, 0.62f, 0.36f, 1f);

    public void Setup(IReadOnlyPlayer player, bool isHost, int fallbackSkinId = 0)
    {
        int skinId = SessionPlayerPropertyHelper.GetPlayerSkinId(player, fallbackSkinId);
        ApplyPortraitSprite(skinId);

        if (_roleTagText != null)
        {
            _roleTagText.text = isHost ? "HOST // ホスト" : "MEMBER // 参加メンバー";
            _roleTagText.color = isHost ? _hostColor : new Color(0.74f, 0.62f, 0.40f, 1f);
        }

        // Relay接続が完了しているか確認（ホストは常に接続済み扱い）
        bool isRelayConnected = SessionPlayerPropertyHelper.IsPlayerRelayConnected(player, isHost);

        if (!isRelayConnected)
        {
            if (_playerNameText != null)
            {
                _playerNameText.supportRichText = false;
                _playerNameText.text = "接続中...";
            }
            ApplyStatusVisual("接続中...", _waitingColor);
            return;
        }

        if (_playerNameText != null)
        {
            _playerNameText.supportRichText = false;
            _playerNameText.text = SessionPlayerPropertyHelper.GetPlayerName(player);
        }

        // 準備状況の取得（ドメインヘルパーへ委譲）
        var isReady = SessionPlayerPropertyHelper.IsPlayerReady(player);

        if (isHost)
        {
            ApplyStatusVisual("ホスト", _hostColor);
        }
        else if (isReady)
        {
            ApplyStatusVisual("準備完了", _readyColor);
        }
        else
        {
            ApplyStatusVisual("準備中", _waitingColor);
        }
    }

    private void ApplyPortraitSprite(int skinId)
    {
        if (_portraitImage == null || _skinPortraits == null || _skinPortraits.Length == 0)
        {
            return;
        }

        int index = ((skinId % _skinPortraits.Length) + _skinPortraits.Length) % _skinPortraits.Length;
        Sprite portrait = _skinPortraits[index];
        if (portrait != null)
        {
            _portraitImage.sprite = portrait;
        }
    }

    private void ApplyStatusVisual(string label, Color color)
    {
        if (_statusImage != null)
        {
            _statusImage.color = color;
        }
        if (_leftSpineImage != null)
        {
            _leftSpineImage.color = color;
        }
        if (_statusLabelText != null)
        {
            _statusLabelText.text = label;
            _statusLabelText.color = color;
        }
    }
}