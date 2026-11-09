using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ロビー画面の各UIパネル・ダイアログの基底クラス。
/// UISettingsConfig に基づくフェード＆ポップアニメーションとSE自動再生機能を提供します。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public abstract class LobbyViewBase : MonoBehaviour
{
    [Header("UIタイプ設定")]
    [Tooltip("パネルの表示タイプ（標準ウィンドウ・モーダルダイアログなど）。UISettingsConfig の設定が一括適用されます。")]
    [SerializeField] protected UIViewType _viewType = UIViewType.DefaultWindow;

    [Tooltip("true の場合、タイプ設定ではなく下記の個別カスタム設定を優先します")]
    [SerializeField] protected bool _overrideCustom = false;

    [Header("個別カスタム設定（_overrideCustom または Custom 時に有効）")]
    [Tooltip("表示アニメーションの時間（秒）")]
    [SerializeField] protected float _showDuration = 0.16f;

    [Tooltip("非表示アニメーションの時間（秒）")]
    [SerializeField] protected float _hideDuration = 0.12f;

    [Tooltip("表示時にわずかに拡大するポップ演出を行うか")]
    [SerializeField] protected bool _useScalePop = true;

    [Tooltip("ポップ演出の開始スケール")]
    [SerializeField] protected float _startScale = 0.95f;

    [Tooltip("表示開始時に鳴らすSE")]
    [SerializeField] protected UISoundType _showSound = UISoundType.None;

    [Tooltip("非表示開始時に鳴らすSE")]
    [SerializeField] protected UISoundType _hideSound = UISoundType.None;

    [Header("自動化設定")]
    [Tooltip("子階層のButtonにUIButtonSoundがなければ自動付与する")]
    [SerializeField] protected bool _autoAttachButtonSounds = true;

    protected CanvasGroup _canvasGroup;
    protected RectTransform _rectTransform;

    private CancellationTokenSource _animCts;
    private bool _isVisible;

    public bool IsVisible => _isVisible;

    public UIViewType ViewType
    {
        get => _viewType;
        set => _viewType = value;
    }

    public bool OverrideCustom
    {
        get => _overrideCustom;
        set => _overrideCustom = value;
    }

    public ViewTypeStyle CurrentStyle
    {
        get
        {
            if (_overrideCustom || _viewType == UIViewType.Custom)
            {
                return new ViewTypeStyle
                {
                    ShowDuration = _showDuration,
                    HideDuration = _hideDuration,
                    UseScalePop = _useScalePop,
                    StartScale = _startScale,
                    ShowSound = _showSound,
                    HideSound = _hideSound
                };
            }
            return UISettingsConfig.Instance.GetViewStyle(_viewType);
        }
    }

    protected virtual void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (_autoAttachButtonSounds)
        {
            AutoAttachButtonSounds();
        }
    }

    protected virtual void OnDestroy()
    {
        CancelCurrentAnimation();
    }

    /// <summary>
    /// パネルをアニメーション（フェードイン＋ポップ）付きで表示する
    /// </summary>
    public async UniTask ShowAsync(CancellationToken cancellationToken = default)
    {
        CancelCurrentAnimation();
        _animCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            this.GetCancellationTokenOnDestroy());

        var ct = _animCts.Token;

        _isVisible = true;
        gameObject.SetActive(true);

        // 表示アニメーション中は誤操作を防ぐため入力を遮断
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = true;

        var style = CurrentStyle;
        UISound.Play(style.ShowSound);
        OnShowStarted();

        if (style.ShowDuration > 0f)
        {
            float elapsed = 0f;
            Vector3 targetScale = Vector3.one;
            Vector3 startScale = style.UseScalePop ? new Vector3(style.StartScale, style.StartScale, 1f) : targetScale;

            while (elapsed < style.ShowDuration)
            {
                if (ct.IsCancellationRequested) return;

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / style.ShowDuration);
                // EaseOutQuad (1 - (1-t)^2) で滑らかに開く
                float eased = 1f - (1f - t) * (1f - t);

                _canvasGroup.alpha = eased;
                if (style.UseScalePop)
                {
                    _rectTransform.localScale = Vector3.LerpUnclamped(startScale, targetScale, eased);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }

        _canvasGroup.alpha = 1f;
        _rectTransform.localScale = Vector3.one;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;

        OnShowCompleted();
    }

    /// <summary>
    /// パネルをアニメーション（フェードアウト）付きで非表示にする
    /// </summary>
    public async UniTask HideAsync(CancellationToken cancellationToken = default)
    {
        if (!_isVisible && !gameObject.activeSelf) return;

        CancelCurrentAnimation();
        _animCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            this.GetCancellationTokenOnDestroy());

        var ct = _animCts.Token;

        _isVisible = false;
        // 非表示開始時点で直ちに入力受付を遮断
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        var style = CurrentStyle;
        UISound.Play(style.HideSound);
        OnHideStarted();

        if (style.HideDuration > 0f && gameObject.activeInHierarchy)
        {
            float elapsed = 0f;
            Vector3 startScale = _rectTransform.localScale;
            Vector3 endScale = style.UseScalePop ? new Vector3(style.StartScale, style.StartScale, 1f) : startScale;
            float startAlpha = _canvasGroup.alpha;

            while (elapsed < style.HideDuration)
            {
                if (ct.IsCancellationRequested) return;

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / style.HideDuration);
                // EaseInQuad (t^2) でスッと消える
                float eased = t * t;

                _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, eased);
                if (style.UseScalePop)
                {
                    _rectTransform.localScale = Vector3.LerpUnclamped(startScale, endScale, eased);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }

        _canvasGroup.alpha = 0f;
        _rectTransform.localScale = Vector3.one;
        gameObject.SetActive(false);

        OnHideCompleted();
    }

    /// <summary>
    /// アニメーションを介さず即座に表示/非表示を切り替える（初期化時用）
    /// </summary>
    public void SetVisibleInstant(bool visible)
    {
        CancelCurrentAnimation();
        _isVisible = visible;

        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        }
        if (_rectTransform == null)
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
        _rectTransform.localScale = Vector3.one;
        gameObject.SetActive(visible);
    }

    /// <summary>
    /// 子階層にある全Buttonを検索し、UIButtonSoundが未アタッチなら自動付与する
    /// （ボタン名からUIButtonTypeを自動設定）
    /// </summary>
    public void AutoAttachButtonSounds()
    {
        var buttons = GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            if (btn == null) continue;
            if (btn.GetComponent<UIButtonSound>() != null) continue;

            var soundComp = btn.gameObject.AddComponent<UIButtonSound>();
            soundComp.ButtonType = UIButtonSound.InferTypeFromName(btn.name);
        }
    }

    private void CancelCurrentAnimation()
    {
        if (_animCts != null)
        {
            _animCts.Cancel();
            _animCts.Dispose();
            _animCts = null;
        }
    }

    protected virtual void OnShowStarted() { }
    protected virtual void OnShowCompleted() { }
    protected virtual void OnHideStarted() { }
    protected virtual void OnHideCompleted() { }
}
