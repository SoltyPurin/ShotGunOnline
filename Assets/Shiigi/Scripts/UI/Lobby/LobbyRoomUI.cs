using System;
using System.Collections.Generic;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LobbyRoomUI : LobbyViewBase
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

    private readonly List<LobbyPlayerEntryUI> _spawnedEntries = new();
    private bool _localIsReady;
    private bool _isLocalHost;
    private bool _canHostStartGame;
    private bool _isStartRequested;

    protected override void Awake()
    {
        _viewType = UIViewType.DefaultWindow;
        base.Awake();

        if (_roomNameText != null)
        {
            _roomNameText.supportRichText = false;
        }

        if (_leaveButton != null)
        {
            _leaveButton.onClick.AddListener(() => OnLeaveRequested?.Invoke());
            UIButtonSound.ConfigureButton(_leaveButton, UIButtonType.Cancel);
        }
        if (_readyButton != null)
        {
            _readyButton.onClick.AddListener(() => OnReadyToggleRequested?.Invoke());
            UIButtonSound.ConfigureButton(_readyButton, UIButtonType.Default);
        }
        if (_startButton != null)
        {
            _startButton.onClick.AddListener(() =>
            {
                if (_isStartRequested) return;
                _isStartRequested = true;
                if (_startButton != null)
                {
                    _startButton.interactable = false;
                }
                OnStartGameRequested?.Invoke();
            });
            UIButtonSound.ConfigureButton(_startButton, UIButtonType.Default);
        }
    }

    private void OnDisable()
    {
        _isStartRequested = false;
    }

    public override void SetVisibleInstant(bool visible)
    {
        if (!visible)
        {
            _isStartRequested = false;
        }
        base.SetVisibleInstant(visible);
    }

    protected override void OnShowStarted()
    {
        _isStartRequested = false;
    }

    protected override void OnHideCompleted()
    {
        _isStartRequested = false;
    }

    public void Refresh(ISession session)
    {
        if (session == null) return;

        if (_roomNameText != null) _roomNameText.text = session.Name;
        if (_playerCountText != null) _playerCountText.text = $"{session.Players.Count} / {session.MaxPlayers}";

        ClearPlayerList();
        if (_playerEntryPrefab != null && _playerContainer != null)
        {
            var players = session.Players;
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                var entry = Instantiate(_playerEntryPrefab, _playerContainer);
                bool isHost = session.Host == player.Id;
                int fallbackSkinId = isHost ? 0 : i;

                entry.Setup(player, isHost, fallbackSkinId);
                _spawnedEntries.Add(entry);
            }
        }

        _isLocalHost = session.IsHost;
        if (_startButton != null) _startButton.gameObject.SetActive(_isLocalHost);
        if (_readyButton != null) _readyButton.gameObject.SetActive(!_isLocalHost);

        if (_isLocalHost)
        {
            _canHostStartGame = SessionPlayerPropertyHelper.CanHostStartGame(session);
            if (_startButton != null)
            {
                _startButton.interactable = !_isStartRequested && _canHostStartGame && (_canvasGroup == null || _canvasGroup.interactable);
            }
        }
        else
        {
            _canHostStartGame = false;
            _localIsReady = SessionPlayerPropertyHelper.IsPlayerReady(session.CurrentPlayer);
            if (_readyButtonText != null)
            {
                _readyButtonText.text = _localIsReady ? "取り消し" : "準備完了";
            }
        }

        RebuildControllerNavigation(_isLocalHost);
    }

    protected override void OnShowCompleted()
    {
        RebuildControllerNavigation(_isLocalHost);
    }

    protected override void OnActiveInteractableChanged(bool interactable)
    {
        if (_leaveButton != null) _leaveButton.interactable = interactable;
        if (_readyButton != null) _readyButton.interactable = interactable;
        if (_startButton != null) _startButton.interactable = interactable && _canHostStartGame && !_isStartRequested;
        if (interactable)
        {
            RebuildControllerNavigation(_isLocalHost);
        }
    }

    public override GameObject GetDefaultFocusTarget()
    {
        if (_startButton != null && _startButton.gameObject.activeInHierarchy && _startButton.IsInteractable())
        {
            return _startButton.gameObject;
        }
        if (_readyButton != null && _readyButton.gameObject.activeInHierarchy && _readyButton.IsInteractable())
        {
            return _readyButton.gameObject;
        }
        if (_leaveButton != null && _leaveButton.gameObject.activeInHierarchy && _leaveButton.IsInteractable())
        {
            return _leaveButton.gameObject;
        }
        return null;
    }

    private void RebuildControllerNavigation(bool isLocalHost)
    {
        Button primary = isLocalHost ? _startButton : _readyButton;
        if (primary == null || _leaveButton == null) return;

        primary.navigation = new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnUp = _leaveButton,
            selectOnDown = _leaveButton,
            selectOnLeft = _leaveButton,
            selectOnRight = _leaveButton
        };

        _leaveButton.navigation = new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnUp = primary,
            selectOnDown = primary,
            selectOnLeft = primary,
            selectOnRight = primary
        };
    }

    private void ClearPlayerList()
    {
        for (int i = 0; i < _spawnedEntries.Count; i++)
        {
            if (_spawnedEntries[i] != null)
            {
                Destroy(_spawnedEntries[i].gameObject);
            }
        }
        _spawnedEntries.Clear();
    }
}
