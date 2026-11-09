using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ロビー画面の各UIパネル・ダイアログの基底クラス。
/// CanvasGroup によるフェード＆ポップ演出（表示/非表示アニメーション）とSE自動再生機能を提供します。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public abstract class LobbyViewBase : MonoBehaviour
{
    [Header("アニメーション設定")]
    [Tooltip("表示アニメーションの時間（秒）")]
    [SerializeField] protected float _showDuration = 0.16f;

    [Tooltip("非表示アニメーションの時間（秒）")]
    [SerializeField] protected float _hideDuration = 0.12f;

    [Tooltip("表示時にわずかに拡大するポップ演出を行うか")]
    [SerializeField] protected bool _useScalePop = true;

    [Tooltip("ポップ演出の開始スケール")]
    [SerializeField] protected float _startScale = 0.95f;

    [Header("SE設定")]
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

        UISound.Play(_showSound);
        OnShowStarted();

        if (_showDuration > 0f)
        {
            float elapsed = 0f;
            Vector3 targetScale = Vector3.one;
            Vector3 startScale = _useScalePop ? new Vector3(_startScale, _startScale, 1f) : targetScale;

            while (elapsed < _showDuration)
            {
                if (ct.IsCancellationRequested) return;

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _showDuration);
                // EaseOutQuad (1 - (1-t)^2) で滑らかに開く
                float eased = 1f - (1f - t) * (1f - t);

                _canvasGroup.alpha = eased;
                if (_useScalePop)
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

        UISound.Play(_hideSound);
        OnHideStarted();

        if (_hideDuration > 0f && gameObject.activeInHierarchy)
        {
            float elapsed = 0f;
            Vector3 startScale = _rectTransform.localScale;
            Vector3 endScale = _useScalePop ? new Vector3(_startScale, _startScale, 1f) : startScale;
            float startAlpha = _canvasGroup.alpha;

            while (elapsed < _hideDuration)
            {
                if (ct.IsCancellationRequested) return;

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _hideDuration);
                // EaseInQuad (t^2) でスッと消える
                float eased = t * t;

                _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, eased);
                if (_useScalePop)
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
    /// （ボタン名に Cancel / Leave / Close / Back / No が含まれる場合はキャンセルSE、それ以外は決定SEを設定）
    /// </summary>
    public void AutoAttachButtonSounds()
    {
        var buttons = GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            if (btn == null) continue;
            if (btn.GetComponent<UIButtonSound>() != null) continue;

            var soundComp = btn.gameObject.AddComponent<UIButtonSound>();
            string btnName = btn.name.ToLowerInvariant();

            if (btnName.Contains("cancel") ||
                btnName.Contains("leave") ||
                btnName.Contains("close") ||
                btnName.Contains("back") ||
                btnName.Contains("no") ||
                btnName.Contains("exit"))
            {
                soundComp.ClickSound = UISoundType.Cancel;
            }
            else
            {
                soundComp.ClickSound = UISoundType.Enter;
            }
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
