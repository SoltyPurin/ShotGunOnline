using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 通信中・更新中などのローディングオーバーレイ表示を制御するクラス。
/// LobbyViewBase を継承し、LoadingOverlay タイプのフェード演出を適用します。
/// </summary>
public class LoadingOverlayController : LobbyViewBase
{
    [SerializeField] private Text _loadingContext;
    [SerializeField] private Color _blockerColor = new Color(0f, 0f, 0f, 0.5f);
    [SerializeField] private float _dotsStepInterval = 0.36f;

    private GameObject _blocker;
    private string _baseContextText = "通信中";
    private readonly string[] _cachedDotTexts = new string[3];
    private float _dotsTimer;
    private int _dotCount = 3;
    private int _transitionVersion;

    protected override void Awake()
    {
        _viewType = UIViewType.LoadingOverlay;
        base.Awake();
        if (_loadingContext != null && !string.IsNullOrEmpty(_loadingContext.text))
        {
            _baseContextText = StripTrailingDots(_loadingContext.text);
        }
        RebuildDotTextsCache();
    }

    protected override void Update()
    {
        base.Update();
        if (!gameObject.activeInHierarchy || _loadingContext == null || string.IsNullOrEmpty(_baseContextText))
        {
            return;
        }

        _dotsTimer += Time.unscaledDeltaTime;
        float interval = Mathf.Max(0.1f, _dotsStepInterval);
        if (_dotsTimer >= interval)
        {
            _dotsTimer %= interval;
            _dotCount = (_dotCount % 3) + 1;
            ApplyContextText();
        }
    }

    public void SetLoadingState(bool isLoading) => SetLoadingState(isLoading, null, false);
    public void SetLoadingState(bool isLoading, string context) => SetLoadingState(isLoading, context, false);

    public void SetLoadingState(bool isLoading, string context, bool immediate)
    {
        int version = ++_transitionVersion;

        if (_loadingContext != null && (context != null || isLoading))
        {
            if (context != null)
            {
                _baseContextText = StripTrailingDots(context);
                RebuildDotTextsCache();
            }
            _dotsTimer = 0f;
            _dotCount = 1;
            ApplyContextText();
        }

        if (isLoading)
        {
            EnsureBlocker();

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            if (immediate)
            {
                SetVisibleInstant(true);
            }
            else
            {
                ShowAsync().Forget();
            }
        }
        else if (immediate)
        {
            if (_blocker != null)
            {
                _blocker.SetActive(false);
            }
            SetVisibleInstant(false);
        }
        else
        {
            CloseAsync(version).Forget();
        }
    }

    private async UniTaskVoid CloseAsync(int version)
    {
        try
        {
            await HideAsync();
        }
        finally
        {
            if (this != null && version == _transitionVersion && _blocker != null)
            {
                _blocker.SetActive(false);
            }
        }
    }

    private void RebuildDotTextsCache()
    {
        _cachedDotTexts[0] = _baseContextText + ".";
        _cachedDotTexts[1] = _baseContextText + "..";
        _cachedDotTexts[2] = _baseContextText + "...";
    }

    private void ApplyContextText()
    {
        if (_loadingContext == null) return;
        int index = Mathf.Clamp(_dotCount - 1, 0, 2);
        _loadingContext.text = _cachedDotTexts[index];
    }

    private static string StripTrailingDots(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text.TrimEnd('.', '…', '・');
    }

    private void EnsureBlocker()
    {
        if (_blocker == null)
        {
            _blocker = UIBlockerHelper.EnsureBlocker(transform, _blockerColor);
        }
        else
        {
            _blocker.SetActive(true);
            _blocker.transform.SetAsFirstSibling();
        }
    }
}
