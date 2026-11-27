using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// プレイヤー頭上にプレイヤー名を表示するワールド空間ネームプレート。
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

    private static Font s_cachedFont;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        s_cachedFont = null;
    }

    private IPlayerNameSource _nameSource;
    private GameObject _canvasRoot;
    private RectTransform _backgroundRect;
    private Text _nameText;
    private string _displayedName = string.Empty;
    private Vector3 _lastParentLossyScale;
    private float _lastAppliedCanvasScale;
    private bool _hasAppliedScale;
    private bool _lastLocalPlayerState;
    private bool _isInitialized;

    /// <summary>
    /// 対象プレイヤーオブジェクトに PlayerNameplateUI が未アタッチなら追加して返す
    /// </summary>
    public static PlayerNameplateUI EnsureAttached(GameObject target)
    {
        if (target == null) return null;

        if (!target.TryGetComponent<PlayerNameplateUI>(out var nameplate))
        {
            nameplate = target.AddComponent<PlayerNameplateUI>();
        }
        return nameplate;
    }

    /// <summary>
    /// 外部から任意の IPlayerNameSource を注入する（テストや拡張プレイヤー種別用）
    /// </summary>
    public void SetNameSource(IPlayerNameSource source)
    {
        if (ReferenceEquals(_nameSource, source)) return;

        UnsubscribeEvents();
        if (_nameSource is IDisposable previousDisposable)
        {
            previousDisposable.Dispose();
        }
        _nameSource = source;
        if (_isInitialized)
        {
            SubscribeEvents();
            RefreshName(_nameSource != null ? _nameSource.PlayerName : PlayerProfileStore.PlayerName);
        }
    }

    private void Awake()
    {
        _nameSource = ResolveNameSource();
    }

    private IPlayerNameSource ResolveNameSource()
    {
        if (TryGetComponent<IPlayerNameSource>(out var directSource))
        {
            return directSource;
        }

        if (TryGetComponent<PlayerMove>(out var playerMove))
        {
            TryGetComponent<PlayerStateManager>(out var stateManager);
            return new PlayerMoveNameSourceAdapter(playerMove, stateManager);
        }

        return new FallbackLocalProfileNameSource();
    }

    private void Start()
    {
        EnsureVisualsCreated();
        SubscribeEvents();
        RefreshName(_nameSource != null ? _nameSource.PlayerName : PlayerProfileStore.PlayerName);
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
        if (_nameSource is IDisposable disposable)
        {
            disposable.Dispose();
        }
        if (_canvasRoot != null)
        {
            Destroy(_canvasRoot);
        }
    }

    private void LateUpdate()
    {
        if (!_isInitialized || _canvasRoot == null) return;

        bool isLocal = _nameSource == null || _nameSource.IsLocalControl;
        if (isLocal != _lastLocalPlayerState)
        {
            RefreshName(_nameSource != null ? _nameSource.PlayerName : PlayerProfileStore.PlayerName);
        }

        bool shouldShow = (_nameSource == null || _nameSource.IsNameplateVisible) && !string.IsNullOrEmpty(_displayedName);
        if (_canvasRoot.activeSelf != shouldShow)
        {
            _canvasRoot.SetActive(shouldShow);
        }
        if (!shouldShow) return;

        _canvasRoot.transform.SetPositionAndRotation(transform.position + _worldOffset, Quaternion.identity);
        UpdateCanvasScaleIfNeeded();
    }

    private void UpdateCanvasScaleIfNeeded()
    {
        Vector3 parentLossy = transform.lossyScale;
        if (_hasAppliedScale &&
            Mathf.Approximately(_lastAppliedCanvasScale, _canvasScale) &&
            (parentLossy - _lastParentLossyScale).sqrMagnitude <= 0.000001f)
        {
            return;
        }

        _hasAppliedScale = true;
        _lastAppliedCanvasScale = _canvasScale;
        _lastParentLossyScale = parentLossy;

        float invX = Mathf.Abs(parentLossy.x) > 0.0001f ? 1f / Mathf.Abs(parentLossy.x) : 1f;
        float invY = Mathf.Abs(parentLossy.y) > 0.0001f ? 1f / Mathf.Abs(parentLossy.y) : 1f;
        float invZ = Mathf.Abs(parentLossy.z) > 0.0001f ? 1f / Mathf.Abs(parentLossy.z) : 1f;
        _canvasRoot.transform.localScale = new Vector3(
            invX * _canvasScale,
            invY * _canvasScale,
            invZ * _canvasScale);
    }

    private void SubscribeEvents()
    {
        if (_nameSource != null)
        {
            _nameSource.OnPlayerNameChanged += HandleNameChanged;
        }
        PlayerProfileStore.OnProfileChanged += HandleLocalProfileChanged;
    }

    private void UnsubscribeEvents()
    {
        if (_nameSource != null)
        {
            _nameSource.OnPlayerNameChanged -= HandleNameChanged;
        }
        PlayerProfileStore.OnProfileChanged -= HandleLocalProfileChanged;
    }

    private void HandleNameChanged(string newName)
    {
        RefreshName(newName);
    }

    private void HandleLocalProfileChanged(PlayerProfile profile)
    {
        if (profile != null && (_nameSource == null || _nameSource.IsLocalControl))
        {
            RefreshName(profile.PlayerName);
        }
    }

    private void RefreshName(string playerName)
    {
        _displayedName = playerName ?? string.Empty;
        _lastLocalPlayerState = _nameSource == null || _nameSource.IsLocalControl;

        if (_nameText == null) return;

        _nameText.text = _displayedName;
        _nameText.color = _lastLocalPlayerState ? _localPlayerTextColor : _remotePlayerTextColor;

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
        _canvasRoot.transform.SetPositionAndRotation(transform.position + _worldOffset, Quaternion.identity);
        UpdateCanvasScaleIfNeeded();

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
        _nameText.font = ResolveFontCached();
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

    private static Font ResolveFontCached()
    {
        if (s_cachedFont != null) return s_cachedFont;

        var existingText = FindAnyObjectByType<Text>();
        if (existingText != null && existingText.font != null)
        {
            s_cachedFont = existingText.font;
            return s_cachedFont;
        }

        s_cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return s_cachedFont;
    }

    /// <summary>
    /// PlayerMove を IPlayerNameSource として扱うアダプター。
    /// </summary>
    private sealed class PlayerMoveNameSourceAdapter : IPlayerNameSource, IDisposable
    {
        private readonly PlayerMove _playerMove;
        private readonly PlayerStateManager _stateManager;
        private string _cachedName;

        public string PlayerName => _playerMove != null && _playerMove.IsLocalControl
            ? PlayerProfileStore.PlayerName
            : _cachedName;

        public bool IsLocalControl => _playerMove == null || _playerMove.IsLocalControl;
        public bool IsNameplateVisible => _stateManager == null || _stateManager.PlayerState != PlayerState.Movie;

        public event Action<string> OnPlayerNameChanged;

        public PlayerMoveNameSourceAdapter(PlayerMove playerMove, PlayerStateManager stateManager)
        {
            _playerMove = playerMove;
            _stateManager = stateManager;
            _cachedName = _playerMove != null ? _playerMove.PlayerName : PlayerProfileStore.PlayerName;
            if (_playerMove != null)
            {
                _playerMove.OnPlayerNameChanged += ForwardNameChanged;
            }
        }

        private void ForwardNameChanged(string newName)
        {
            _cachedName = newName ?? string.Empty;
            OnPlayerNameChanged?.Invoke(_cachedName);
        }

        public void Dispose()
        {
            if (_playerMove != null)
            {
                _playerMove.OnPlayerNameChanged -= ForwardNameChanged;
            }
        }
    }

    private sealed class FallbackLocalProfileNameSource : IPlayerNameSource
    {
        public string PlayerName => PlayerProfileStore.PlayerName;
        public bool IsLocalControl => true;
        public bool IsNameplateVisible => true;
        public event Action<string> OnPlayerNameChanged { add { } remove { } }
    }
}
