using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// ロビー画面の各UIパネル・ダイアログの基底クラス (SRP / OCP)。
/// UISettingsConfig に基づくフェード＆ポップアニメーション、アクティブビュースタック管理、フォーカス保護を提供します。
/// InputModule設定やD-Padリピート制御は単一責務クラス（LobbyUIInputModuleConfigurator / DPadRepeatNavigator）へ委譲しています。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public abstract class LobbyViewBase : MonoBehaviour
{
    [Header("UIタイプ設定")]
    [Tooltip("パネルの表示タイプ（標準ウィンドウ・モーダルダイアログなど）。UISettingsConfig の設定が一括適用されます。")]
    [SerializeField] protected UIViewType _viewType = UIViewType.DefaultWindow;

    [Tooltip("true の場合、タイプ設定ではなく下記の個別カスタム設定を優先します")]
    [SerializeField] protected bool _overrideCustom;

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

    private static readonly List<LobbyViewBase> _activeViews = new();

    public static LobbyViewBase TopActiveView
    {
        get
        {
            PruneDestroyedViews();
            return _activeViews.Count > 0 ? _activeViews[_activeViews.Count - 1] : null;
        }
    }

    public static bool IsModalOpen
    {
        get
        {
            var top = TopActiveView;
            return top != null && top._viewType == UIViewType.ModalDialog;
        }
    }

    public static event Action OnActiveViewStackChanged;

    private DPadRepeatNavigator _dpadNavigator;
    private Func<Transform, bool> _cachedOwnershipPredicate;
    private Func<GameObject> _cachedDefaultFocusFunc;
    private CancellationTokenSource _animCts;
    private bool _isVisible;
    private bool _hasAwakened;

    public bool IsVisible => _isVisible;
    public bool IsTopActiveView => TopActiveView == this;

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
        if (!_hasAwakened)
        {
            _hasAwakened = true;
            LobbyUIInputModuleConfigurator.RegisterViewInstance();
        }
        else
        {
            LobbyUIInputModuleConfigurator.ConfigureCurrentEventSystem();
        }

        _cachedOwnershipPredicate ??= IsOwnedByThisView;
        _cachedDefaultFocusFunc ??= GetDefaultFocusTarget;

        EnsureComponents();

        if (_autoAttachButtonSounds)
        {
            AutoAttachButtonSounds();
        }
    }

    private void EnsureComponents()
    {
        if (_rectTransform == null)
        {
            _rectTransform = GetComponent<RectTransform>();
        }
        if (_canvasGroup == null && !TryGetComponent(out _canvasGroup))
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    protected virtual void OnDestroy()
    {
        CancelCurrentAnimation();
        RemoveActiveView(this);

        if (_hasAwakened)
        {
            _hasAwakened = false;
            LobbyUIInputModuleConfigurator.UnregisterViewInstance();
        }
    }

    protected virtual void Update()
    {
        ProcessDPadNavigation();
    }

    protected void ProcessDPadNavigation()
    {
        bool canNavigate = _isVisible && IsTopActiveView && _canvasGroup != null && _canvasGroup.interactable;
        _cachedOwnershipPredicate ??= IsOwnedByThisView;
        _cachedDefaultFocusFunc ??= GetDefaultFocusTarget;
        _dpadNavigator.Process(canNavigate, _cachedOwnershipPredicate, _cachedDefaultFocusFunc);
    }

    protected virtual void LateUpdate()
    {
        if (!_isVisible || !IsTopActiveView || _canvasGroup == null || !_canvasGroup.interactable) return;
        if (EventSystem.current == null) return;

        var selected = EventSystem.current.currentSelectedGameObject;
        if (selected != null && !IsOwnedByThisView(selected.transform))
        {
            EventSystem.current.SetSelectedGameObject(GetDefaultFocusTarget());
        }
    }

    protected virtual bool IsOwnedByThisView(Transform target)
    {
        return target != null && target.IsChildOf(transform);
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
        PushActiveView(this);

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
        bool isTop = IsTopActiveView;
        _canvasGroup.interactable = isTop;
        _canvasGroup.blocksRaycasts = true;
        OnActiveInteractableChanged(isTop);

        OnShowCompleted();
        if (isTop)
        {
            RestoreDefaultFocus();
        }
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
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        OnActiveInteractableChanged(false);
        RemoveActiveView(this);

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

        EnsureComponents();

        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.blocksRaycasts = visible;
        _rectTransform.localScale = Vector3.one;
        gameObject.SetActive(visible);

        if (visible)
        {
            PushActiveView(this);
            bool isTop = IsTopActiveView;
            _canvasGroup.interactable = isTop;
            OnActiveInteractableChanged(isTop);
        }
        else
        {
            _canvasGroup.interactable = false;
            OnActiveInteractableChanged(false);
            RemoveActiveView(this);
        }
    }

    /// <summary>
    /// 子階層にある全Buttonを検索し、UIButtonSoundが未アタッチなら自動付与する
    /// </summary>
    public void AutoAttachButtonSounds()
    {
        var buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            var btn = buttons[i];
            if (btn == null || btn.TryGetComponent<UIButtonSound>(out _)) continue;
            UIButtonSound.EnsureAttached(btn, UIButtonSound.InferTypeFromName(btn.name));
        }
    }

    private void CancelCurrentAnimation()
    {
        if (_animCts == null) return;
        _animCts.Cancel();
        _animCts.Dispose();
        _animCts = null;
    }

    public void RestoreDefaultFocus()
    {
        if (!IsTopActiveView || !_isVisible || EventSystem.current == null) return;
        var target = GetDefaultFocusTarget();
        if (target != null && target.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(target);
        }
    }

    public virtual GameObject GetDefaultFocusTarget() => null;
    protected virtual void OnActiveInteractableChanged(bool interactable) { }

    private static void PruneDestroyedViews()
    {
        _activeViews.RemoveAll(static v => v == null);
    }

    private static void PushActiveView(LobbyViewBase view)
    {
        if (view == null) return;
        PruneDestroyedViews();
        _activeViews.Remove(view);
        _activeViews.Add(view);
        RefreshActiveViewInteractability(restoreFocusOnTop: false);
    }

    private static void RemoveActiveView(LobbyViewBase view)
    {
        PruneDestroyedViews();
        if (_activeViews.Remove(view))
        {
            RefreshActiveViewInteractability(restoreFocusOnTop: true);
        }
    }

    private static void RefreshActiveViewInteractability(bool restoreFocusOnTop)
    {
        PruneDestroyedViews();
        var top = _activeViews.Count > 0 ? _activeViews[_activeViews.Count - 1] : null;
        for (int i = 0; i < _activeViews.Count; i++)
        {
            var v = _activeViews[i];
            bool isTop = (v == top);
            v.EnsureComponents();
            v._canvasGroup.interactable = isTop && v._isVisible;
            v.OnActiveInteractableChanged(isTop && v._isVisible);
        }

        if (restoreFocusOnTop && top != null)
        {
            top.RestoreDefaultFocus();
        }

        OnActiveViewStackChanged?.Invoke();
    }

    protected virtual void OnShowStarted() { }
    protected virtual void OnShowCompleted() { }
    protected virtual void OnHideStarted() { }
    protected virtual void OnHideCompleted() { }
}
