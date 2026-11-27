using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// ボタンにアタッチしてUIタイプを選択することで、
/// UISettingsConfig に設定されたSEとバウンス演出を自動適用するコンポーネント。
/// </summary>
[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour, ISelectHandler, IPointerClickHandler, ISubmitHandler
{
    [Header("UIタイプ設定")]
    [Tooltip("ボタンの役割タイプ（決定・キャンセル・サブ操作など）。タイプに応じたSE・演出が一括適用されます。")]
    [SerializeField] private UIButtonType _buttonType = UIButtonType.Default;

    [Tooltip("true の場合、タイプ設定ではなく下記のカスタム設定を優先します")]
    [SerializeField] private bool _overrideCustom;

    [Header("個別カスタム設定（_overrideCustom または Custom 時に有効）")]
    [SerializeField] private ButtonTypeStyle _customStyle = ButtonTypeStyle.CreateDefault(UISoundType.Enter);

    private Button _button;
    private RectTransform _rectTransform;
    private UIButtonMetallicVisual _metallicVisual;
    private Vector3 _originalScale = Vector3.one;
    private CancellationTokenSource _animCts;

    public UIButtonType ButtonType
    {
        get => _buttonType;
        set => _buttonType = value;
    }

    public bool OverrideCustom
    {
        get => _overrideCustom;
        set => _overrideCustom = value;
    }

    public UISoundType ClickSound
    {
        get => CurrentStyle.ClickSound;
        set
        {
            _overrideCustom = true;
            _customStyle.ClickSound = value;
        }
    }

    public ButtonTypeStyle CurrentStyle
    {
        get
        {
            if (_overrideCustom || _buttonType == UIButtonType.Custom)
            {
                return _customStyle;
            }
            return UISettingsConfig.Instance.GetButtonStyle(_buttonType);
        }
    }

    /// <summary>
    /// 指定ボタンに UIButtonSound を付与（既存なら取得）してボタン種別を設定する
    /// </summary>
    public static UIButtonSound EnsureAttached(Button button, UIButtonType buttonType = UIButtonType.Default)
    {
        if (button == null) return null;
        if (!button.TryGetComponent<UIButtonSound>(out var sound))
        {
            sound = button.gameObject.AddComponent<UIButtonSound>();
        }
        sound.ButtonType = buttonType;
        return sound;
    }

    /// <summary>
    /// 指定されたボタンに効果音および金属光沢ビジュアルを一括セットアップするヘルパー
    /// </summary>
    public static void ConfigureButton(Button button, UIButtonType buttonType, bool attachMetallicVisual = true)
    {
        if (button == null) return;
        EnsureAttached(button, buttonType);
        if (attachMetallicVisual)
        {
            UIButtonMetallicVisual.Attach(button);
        }
    }

    private void Reset()
    {
        _buttonType = InferTypeFromName(name);
        _customStyle = ButtonTypeStyle.CreateDefault(
            _buttonType == UIButtonType.Cancel ? UISoundType.Cancel : UISoundType.Enter);
    }

    private void Awake()
    {
        _button = GetComponent<Button>();
        _rectTransform = GetComponent<RectTransform>();
        if (_rectTransform != null)
        {
            _originalScale = _rectTransform.localScale;
        }
    }

    private void OnDisable()
    {
        CancelAnimation();
    }

    private void OnDestroy()
    {
        CancelAnimation();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        HandleClick();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        HandleClick();
    }

    private void HandleClick()
    {
        if (_button == null || !_button.interactable) return;

        var style = CurrentStyle;
        UISound.Play(style.ClickSound);

        if (style.UseScaleBounce && _rectTransform != null && gameObject.activeInHierarchy)
        {
            if (_metallicVisual == null)
            {
                TryGetComponent(out _metallicVisual);
            }

            if (_metallicVisual != null && _metallicVisual.isActiveAndEnabled)
            {
                _metallicVisual.TriggerClickBounce(style.PressedScale, style.BounceDuration);
            }
            else
            {
                PlayBounceAnimationAsync(style).Forget();
            }
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (_button == null || !_button.interactable) return;

        var style = CurrentStyle;
        if (style.PlayHoverSound)
        {
            UISound.Play(style.HoverSound);
        }
    }

    private async UniTaskVoid PlayBounceAnimationAsync(ButtonTypeStyle style)
    {
        CancelAnimation();
        var cts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        _animCts = cts;
        var ct = cts.Token;

        Vector3 pressedScale = _originalScale * style.PressedScale;
        float halfDuration = Mathf.Max(0.01f, style.BounceDuration * 0.5f);

        try
        {
            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                if (ct.IsCancellationRequested) return;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                _rectTransform.localScale = Vector3.Lerp(_originalScale, pressedScale, t);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                if (ct.IsCancellationRequested) return;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                _rectTransform.localScale = Vector3.Lerp(pressedScale, _originalScale, t);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            if (_rectTransform != null)
            {
                _rectTransform.localScale = _originalScale;
            }
        }
        catch (System.OperationCanceledException)
        {
        }
        finally
        {
            if (_animCts == cts)
            {
                _animCts = null;
            }
            cts.Dispose();
        }
    }

    private void CancelAnimation()
    {
        bool wasAnimating = _animCts != null;
        if (_animCts != null)
        {
            var cts = _animCts;
            _animCts = null;
            cts.Cancel();
            cts.Dispose();
        }

        if (wasAnimating && _rectTransform != null)
        {
            _rectTransform.localScale = _originalScale;
        }
    }

    /// <summary>
    /// ボタンのオブジェクト名から適切な初期タイプを推論する
    /// </summary>
    public static UIButtonType InferTypeFromName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName)) return UIButtonType.Default;

        string lower = objectName.ToLowerInvariant();
        if (lower.Contains("cancel") ||
            lower.Contains("close") ||
            lower.Contains("back") ||
            lower.Contains("leave") ||
            lower.Contains("exit") ||
            IsNoButtonName(lower))
        {
            return UIButtonType.Cancel;
        }

        if (lower.Contains("refresh") ||
            lower.Contains("reload") ||
            lower.Contains("tab") ||
            lower.Contains("setting"))
        {
            return UIButtonType.SubAction;
        }

        return UIButtonType.Default;
    }

    private static bool IsNoButtonName(string lower)
    {
        return lower == "no" ||
               lower.StartsWith("no_") ||
               lower.StartsWith("no ") ||
               lower.EndsWith("_no") ||
               lower.EndsWith(" no") ||
               lower.Contains("_no_") ||
               lower.Contains("nobutton") ||
               lower.Contains("no_btn");
    }
}
