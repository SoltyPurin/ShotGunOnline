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
public class UIButtonSound : MonoBehaviour, ISelectHandler
{
    [Header("UIタイプ設定")]
    [Tooltip("ボタンの役割タイプ（決定・キャンセル・サブ操作など）。タイプに応じたSE・演出が一括適用されます。")]
    [SerializeField] private UIButtonType _buttonType = UIButtonType.Default;

    [Tooltip("true の場合、タイプ設定ではなく下記のカスタム設定を優先します")]
    [SerializeField] private bool _overrideCustom = false;

    [Header("個別カスタム設定（_overrideCustom または Custom 時に有効）")]
    [SerializeField] private ButtonTypeStyle _customStyle = ButtonTypeStyle.CreateDefault(UISoundType.Enter);

    private Button _button;
    private RectTransform _rectTransform;
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

    private void Reset()
    {
        // コンポーネント追加時にボタン名からタイプを自動推論
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

        _button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        CancelAnimation();
        if (_button != null)
        {
            _button.onClick.RemoveListener(HandleClick);
        }
    }

    private void HandleClick()
    {
        if (_button == null || !_button.interactable) return;

        var style = CurrentStyle;
        UISound.Play(style.ClickSound);

        if (style.UseScaleBounce && _rectTransform != null && gameObject.activeInHierarchy)
        {
            PlayBounceAnimationAsync(style).Forget();
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
        _animCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        var ct = _animCts.Token;

        Vector3 pressedScale = _originalScale * style.PressedScale;
        float halfDuration = Mathf.Max(0.01f, style.BounceDuration * 0.5f);

        // 縮小 (1.0 -> pressedScale)
        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            if (ct.IsCancellationRequested) return;
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            _rectTransform.localScale = Vector3.Lerp(_originalScale, pressedScale, t);
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        // 復元 (pressedScale -> 1.0)
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            if (ct.IsCancellationRequested) return;
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            _rectTransform.localScale = Vector3.Lerp(pressedScale, _originalScale, t);
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        _rectTransform.localScale = _originalScale;
    }

    private void CancelAnimation()
    {
        if (_animCts != null)
        {
            _animCts.Cancel();
            _animCts.Dispose();
            _animCts = null;
        }

        if (_rectTransform != null)
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
            lower.Contains("no"))
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
}
