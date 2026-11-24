using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// プレイヤーキャラクターの頭上にプレイヤー名を表示するワールド空間ネームプレート。
/// PlayerMove / LobbyNetworkPlayer / LobbyLocalAvatar のいずれにも自動で紐づいて動作します。
/// </summary>
public class PlayerNameplateUI : MonoBehaviour
{
    [Header("表示位置・見た目設定")]
    [SerializeField] private Vector3 _worldOffset = new Vector3(0f, 1.35f, 0f);
    [SerializeField] private float _canvasScale = 0.012f;
    [SerializeField] private int _sortingOrder = 50;
    [SerializeField] private Color _localPlayerTextColor = new Color(1f, 0.94f, 0.58f, 1f);
    [SerializeField] private Color _remotePlayerTextColor = Color.white;
    [SerializeField] private Color _backgroundColor = new Color(0.06f, 0.07f, 0.1f, 0.68f);

    private const float MinPaddingX = 24f;
    private const float BackgroundHeight = 40f;
    private const float MinBackgroundWidth = 100f;
    private const float MaxBackgroundWidth = 340f;

    private PlayerMove _playerMove;
    private LobbyNetworkPlayer _lobbyNetworkPlayer;
    private LobbyLocalAvatar _lobbyLocalAvatar;
    private PlayerStateManager _playerStateManager;

    private GameObject _canvasRoot;
    private RectTransform _backgroundRect;
    private Text _nameText;
    private string _displayedName;
    private bool _isInitialized;

    /// <summary>
    /// 対象プレイヤーオブジェクトに PlayerNameplateUI が未アタッチなら追加して返す
    /// </summary>
    public static PlayerNameplateUI EnsureAttached(GameObject target)
    {
        if (target == null) return null;

        var nameplate = target.GetComponent<PlayerNameplateUI>();
        if (nameplate == null)
        {
            nameplate = target.AddComponent<PlayerNameplateUI>();
        }
        return nameplate;
    }

    private void Awake()
    {
        _playerMove = GetComponent<PlayerMove>();
        _lobbyNetworkPlayer = GetComponent<LobbyNetworkPlayer>();
        _lobbyLocalAvatar = GetComponent<LobbyLocalAvatar>();
        _playerStateManager = GetComponent<PlayerStateManager>();
    }

    private void Start()
    {
        EnsureVisualsCreated();
        SubscribeEvents();
        RefreshName(ResolveCurrentName());
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
        if (_canvasRoot != null)
        {
            Destroy(_canvasRoot);
        }
    }

    private void LateUpdate()
    {
        if (!_isInitialized || _canvasRoot == null) return;

        bool shouldShow = ShouldShowNameplate();
        if (_canvasRoot.activeSelf != shouldShow)
        {
            _canvasRoot.SetActive(shouldShow);
        }
        if (!shouldShow) return;

        // キャラクター本体の回転・左右反転の影響を受けないようワールド基準で位置と向きを維持する
        _canvasRoot.transform.position = transform.position + _worldOffset;
        _canvasRoot.transform.rotation = Quaternion.identity;

        Vector3 parentLossy = transform.lossyScale;
        float invX = Mathf.Abs(parentLossy.x) > 0.0001f ? 1f / parentLossy.x : 1f;
        float invY = Mathf.Abs(parentLossy.y) > 0.0001f ? 1f / parentLossy.y : 1f;
        float invZ = Mathf.Abs(parentLossy.z) > 0.0001f ? 1f / parentLossy.z : 1f;
        _canvasRoot.transform.localScale = new Vector3(
            Mathf.Abs(invX) * _canvasScale,
            Mathf.Abs(invY) * _canvasScale,
            Mathf.Abs(invZ) * _canvasScale);

        string latestName = ResolveCurrentName();
        if (latestName != _displayedName)
        {
            RefreshName(latestName);
        }
    }

    private bool ShouldShowNameplate()
    {
        if (_playerStateManager != null && _playerStateManager.PlayerState == PlayerState.Movie)
        {
            return false;
        }
        return !string.IsNullOrEmpty(ResolveCurrentName());
    }

    private bool IsLocalPlayer()
    {
        if (_playerMove != null) return _playerMove.IsLocalControl;
        if (_lobbyNetworkPlayer != null) return _lobbyNetworkPlayer.IsOwner || !_lobbyNetworkPlayer.IsSpawned;
        if (_lobbyLocalAvatar != null) return true;
        return true;
    }

    private string ResolveCurrentName()
    {
        if (_playerMove != null) return _playerMove.PlayerName;
        if (_lobbyNetworkPlayer != null) return _lobbyNetworkPlayer.PlayerName;
        if (_lobbyLocalAvatar != null) return PlayerProfileStore.PlayerName;
        return PlayerProfileStore.PlayerName;
    }

    private void SubscribeEvents()
    {
        if (_playerMove != null)
        {
            _playerMove.OnPlayerNameChanged += HandleNameChanged;
        }
        if (_lobbyNetworkPlayer != null)
        {
            _lobbyNetworkPlayer.OnPlayerNameChanged += HandleNameChanged;
        }
        PlayerProfileStore.OnProfileChanged += HandleLocalProfileChanged;
    }

    private void UnsubscribeEvents()
    {
        if (_playerMove != null)
        {
            _playerMove.OnPlayerNameChanged -= HandleNameChanged;
        }
        if (_lobbyNetworkPlayer != null)
        {
            _lobbyNetworkPlayer.OnPlayerNameChanged -= HandleNameChanged;
        }
        PlayerProfileStore.OnProfileChanged -= HandleLocalProfileChanged;
    }

    private void HandleNameChanged(string newName)
    {
        RefreshName(newName);
    }

    private void HandleLocalProfileChanged(PlayerProfile profile)
    {
        if (IsLocalPlayer() && profile != null)
        {
            RefreshName(profile.PlayerName);
        }
    }

    private void RefreshName(string playerName)
    {
        _displayedName = playerName ?? string.Empty;
        if (_nameText == null) return;

        _nameText.text = _displayedName;
        _nameText.color = IsLocalPlayer() ? _localPlayerTextColor : _remotePlayerTextColor;

        if (_backgroundRect != null)
        {
            float preferredWidth = _nameText.preferredWidth + MinPaddingX;
            float width = Mathf.Clamp(preferredWidth, MinBackgroundWidth, MaxBackgroundWidth);
            _backgroundRect.sizeDelta = new Vector2(width, BackgroundHeight);
        }
    }

    private void EnsureVisualsCreated()
    {
        if (_isInitialized) return;

        _canvasRoot = new GameObject("PlayerNameplateCanvas", typeof(RectTransform), typeof(Canvas));
        _canvasRoot.transform.SetParent(transform, false);
        _canvasRoot.transform.position = transform.position + _worldOffset;
        _canvasRoot.transform.rotation = Quaternion.identity;

        var canvas = _canvasRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = _sortingOrder;

        var canvasRect = _canvasRoot.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(MaxBackgroundWidth, BackgroundHeight);

        var bgGo = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bgGo.transform.SetParent(_canvasRoot.transform, false);
        _backgroundRect = bgGo.GetComponent<RectTransform>();
        _backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
        _backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
        _backgroundRect.pivot = new Vector2(0.5f, 0.5f);
        _backgroundRect.sizeDelta = new Vector2(180f, BackgroundHeight);

        var bgImage = bgGo.GetComponent<Image>();
        bgImage.color = _backgroundColor;
        bgImage.raycastTarget = false;

        var txtGo = new GameObject("NameText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
        txtGo.transform.SetParent(bgGo.transform, false);

        var txtRect = txtGo.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = new Vector2(8f, 2f);
        txtRect.offsetMax = new Vector2(-8f, -2f);

        _nameText = txtGo.GetComponent<Text>();
        _nameText.font = ResolveFont();
        _nameText.fontSize = 24;
        _nameText.fontStyle = FontStyle.Bold;
        _nameText.alignment = TextAnchor.MiddleCenter;
        _nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _nameText.verticalOverflow = VerticalWrapMode.Overflow;
        _nameText.supportRichText = false;
        _nameText.raycastTarget = false;

        var outline = txtGo.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        _isInitialized = true;
    }

    private static Font ResolveFont()
    {
        var existingText = FindAnyObjectByType<Text>();
        if (existingText != null && existingText.font != null)
        {
            return existingText.font;
        }
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}
