using System;
using System.Collections.Generic;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

public class LobbyRoomUI : MonoBehaviour
{
    [Header("ルーム情報")]
    [SerializeField] private Text _roomNameText;
    [SerializeField] private Text _playerCountText;
    [SerializeField] private Transform _playerContainer;
    [SerializeField] private LobbyPlayerEntryUI _playerEntryPrefab;

    [Header("操作")]
    [SerializeField] private Button _readyButton;
    [SerializeField] private Text _readyButtonText;
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _leaveButton;

    public event Action OnLeaveRequested;
    public event Action OnReadyToggleRequested;
    public event Action OnStartGameRequested;

    private readonly List<LobbyPlayerEntryUI> _spawnedEntries = new List<LobbyPlayerEntryUI>();
    private bool _localIsReady;

    private void Awake()
    {
        _leaveButton.onClick.AddListener(() => OnLeaveRequested?.Invoke());
        _readyButton.onClick.AddListener(() => OnReadyToggleRequested?.Invoke());
        _startButton.onClick.AddListener(() => OnStartGameRequested?.Invoke());
    }

    public void Refresh(ISession session)
    {
        if (session == null) return;

        // ルーム名・プレイヤー数表示
        _roomNameText.text = $"{session.Name}";
        _playerCountText.text = $"{session.Players.Count} / {session.MaxPlayers}";

        // リストクリア＆プレイヤー一覧描画
        ClearPlayerList();
        foreach (var player in session.Players)
        {
            var entry = Instantiate(_playerEntryPrefab, _playerContainer);
            var isHost = session.Host == player.Id;

            entry.Setup(player as IPlayer, isHost);
            _spawnedEntries.Add(entry);
        }

        // ローカルプレイヤーがホストかどうかでボタンの出し分け
        var isLocalHost = session.IsHost;
        _startButton.gameObject.SetActive(isLocalHost);
        _readyButton.gameObject.SetActive(!isLocalHost);

        if (isLocalHost)
        {
            // ゲーム開始可能判定はドメインヘルパーへ委譲
            _startButton.interactable = SessionPlayerPropertyHelper.CanHostStartGame(session);
        }
        else
        {
            // ゲストの場合は自身の準備状況を取得してテキスト更新
            _localIsReady = SessionPlayerPropertyHelper.IsPlayerReady(session.CurrentPlayer);
            _readyButtonText.text = _localIsReady ? "取り消し" : "準備完了";
        }
    }

    private void ClearPlayerList()
    {
        foreach (var entry in _spawnedEntries)
        {
            if (entry != null)
            {
                Destroy(entry.gameObject);
            }
        }
        _spawnedEntries.Clear();

        for (int i = 0; i < _playerContainer.childCount; i++)
        {
            Destroy(_playerContainer.GetChild(i).gameObject);
        }
    }
}
