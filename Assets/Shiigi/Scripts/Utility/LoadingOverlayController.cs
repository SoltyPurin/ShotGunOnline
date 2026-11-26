using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 通信中・更新中などのローディングオーバーレイ表示を制御するクラス。
/// LobbyViewBase を継承し、LoadingOverlay タイプのフェード演出を適用します。
/// 表示時に全画面ブロック用UIを生成し、背後UIのボタン押下を防止します。
/// </summary>
public class LoadingOverlayController : LobbyViewBase
{
    [SerializeField] private Text _loadingContext;
    [SerializeField] private Color _blockerColor = new Color(0f, 0f, 0f, 0.5f);
    [SerializeField] private float _dotsStepInterval = 0.36f;

    private GameObject _blocker;
    private string _baseContextText = "通信中";
    private float _dotsTimer;
    private int _dotCount = 3;

    protected override void Awake()
    {
        _viewType = UIViewType.LoadingOverlay;
        base.Awake();
        if (_loadingContext != null && !string.IsNullOrEmpty(_loadingContext.text))
        {
            _baseContextText = StripTrailingDots(_loadingContext.text);
        }
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
            _dotsTimer -= interval;
            _dotCount = (_dotCount % 3) + 1;
            ApplyContextText();
        }
    }

    public void SetLoadingState(bool isLoading)
    {
        SetLoadingState(isLoading, null, false);
    }

    public void SetLoadingState(bool isLoading, string context)
    {
        SetLoadingState(isLoading, context, false);
    }

    public void SetLoadingState(bool isLoading, string context, bool immediate)
    {
        if (_loadingContext != null && context != null)
        {
            _baseContextText = StripTrailingDots(context);
            _dotsTimer = 0f;
            _dotCount = 1;
            ApplyContextText();
        }
        else if (isLoading && _loadingContext != null)
        {
            _dotsTimer = 0f;
            _dotCount = 1;
            ApplyContextText();
        }

        if (isLoading)
        {
            EnsureBlocker();

            // キーボードやゲームパッドで背後UIボタンが誤押下されないようフォーカスを解除
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
        else
        {
            if (immediate)
            {
                if (_blocker != null)
                {
                    _blocker.SetActive(false);
                }
                SetVisibleInstant(false);
            }
            else
            {
                CloseAsync().Forget();
            }
        }
    }

    private async UniTaskVoid CloseAsync()
    {
        if (_blocker != null)
        {
            _blocker.SetActive(false);
        }
        await HideAsync();
    }

    private void ApplyContextText()
    {
        if (_loadingContext == null) return;
        string dots = _dotCount switch
        {
            1 => ".",
            2 => "..",
            _ => "..."
        };
        _loadingContext.text = _baseContextText + dots;
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
