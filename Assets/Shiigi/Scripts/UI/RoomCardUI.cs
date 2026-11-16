using System;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RoomCardUI : MonoBehaviour, ISelectHandler
{
    [Header("テキスト")] [SerializeField] private Text _roomNameText;
    [SerializeField] private Text _playerCountText;

    [Header("ボタン")] [SerializeField] private Button _joinButton;
    [SerializeField] private Button _cardButton;

    private ISessionInfo _sessionInfo;

    // 参加ボタンが押されたことを通知するイベント（旧UI互換）
    public event Action<ISessionInfo> OnJoinRequested;
    // カードが選択状態になったことを通知するイベント
    public event Action<ISessionInfo, RoomCardUI> OnCardSelected;
    // カードが決定（クリック/Submit）されたことを通知するイベント
    public event Action<ISessionInfo, RoomCardUI> OnCardSubmitted;

    public ISessionInfo SessionInfo => _sessionInfo;
    public Button JoinButton => NavigationButton;
    public Button NavigationButton
    {
        get
        {
            if (_joinButton != null && _joinButton.gameObject.activeInHierarchy) return _joinButton;
            return _cardButton;
        }
    }

    private void Awake()
    {
        if (_roomNameText != null)
        {
            _roomNameText.supportRichText = false;
        }

        EnsureCardButton();

        if (_cardButton != null)
        {
            _cardButton.onClick.AddListener(HandleCardClicked);
            UIButtonMetallicVisual.Attach(_cardButton);
            EnsureButtonSound(_cardButton);
        }

        if (_joinButton != null)
        {
            UIButtonMetallicVisual.Attach(_joinButton);
            _joinButton.onClick.AddListener(() => OnJoinRequested?.Invoke(_sessionInfo));
            EnsureButtonSound(_joinButton);
        }
    }

    private void EnsureCardButton()
    {
        if (_cardButton != null) return;

        // 旧UIの参加ボタンが非アクティブな場合はカードルート自体を選択・クリック可能にする
        if (_joinButton == null || !_joinButton.gameObject.activeSelf)
        {
            _cardButton = GetComponent<Button>();
            if (_cardButton == null)
            {
                _cardButton = gameObject.AddComponent<Button>();
            }
            if (_cardButton.targetGraphic == null)
            {
                _cardButton.targetGraphic = GetComponent<Graphic>();
            }
        }
    }

    private static void EnsureButtonSound(Button button)
    {
        if (button != null && button.GetComponent<UIButtonSound>() == null)
        {
            var sound = button.gameObject.AddComponent<UIButtonSound>();
            sound.ButtonType = UIButtonType.Default;
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (_sessionInfo != null)
        {
            OnCardSelected?.Invoke(_sessionInfo, this);
        }
    }

    private void HandleCardClicked()
    {
        if (_sessionInfo == null) return;
        OnCardSelected?.Invoke(_sessionInfo, this);
        OnCardSubmitted?.Invoke(_sessionInfo, this);
    }

    /// <summary>
    /// セッション情報を元にカードの表示を更新する
    /// </summary>
    public void Setup(ISessionInfo sessionInfo)
    {
        _sessionInfo = sessionInfo;
        if (_roomNameText != null)
        {
            _roomNameText.text = sessionInfo.Name;
        }

        if (_playerCountText != null)
        {
            var currentPlayerCount = sessionInfo.MaxPlayers - sessionInfo.AvailableSlots;
            _playerCountText.text = $"{currentPlayerCount} / {sessionInfo.MaxPlayers}";
        }

        // 旧参加ボタンが有効な場合のみ満員時に非活性化する（カード自体の選択は満員でも許可）
        if (_joinButton != null && _joinButton.gameObject.activeSelf)
        {
            _joinButton.interactable = sessionInfo.AvailableSlots > 0;
        }
    }
}
