using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// ボタン選択（コントローラー/キーボード）およびホバー時の演出コンポーネント。
/// ボタン自身の色（エディタ設定）をベースに、選択時は元のカラーを加算して濃くしつつ、
/// 拡大アニメーション・外枠（白/黒）・金属光沢の斜めシマーを適用。
/// </summary>
[DisallowMultipleComponent]
public class UIButtonMetallicVisual : MonoBehaviour,
    ISelectHandler, IDeselectHandler,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler,
    ISubmitHandler
{
    private static readonly int ShineProgressId = Shader.PropertyToID("_ShineProgress");
    private static readonly int ShineIntensityId = Shader.PropertyToID("_ShineIntensity");
    private static readonly int RimColorId = Shader.PropertyToID("_RimColor");
    private static readonly int RimIntensityId = Shader.PropertyToID("_RimIntensity");
    private static readonly int RimThicknessId = Shader.PropertyToID("_RimThickness");
    private static readonly int FocusAddId = Shader.PropertyToID("_FocusAdd");

    [Header("選択時の外枠・発色設定")]
    [SerializeField] private Color _outlineColor = Color.black;
    [SerializeField, Range(0.005f, 0.12f)] private float _outlineThickness = 0.015f;
    [SerializeField, Range(0f, 1f)] private float _focusSelfAdd = 0f;

    [Header("アニメーション設定")]
    [SerializeField] private float _focusedScale = 1.06f;
    [SerializeField] private float _pressedScale = 0.96f;
    [SerializeField] private float _transitionSpeed = 16f;
    [SerializeField] private float _shineSweepDuration = 0.42f;
    [SerializeField] private float _periodicShineInterval = 2.6f;

    private Selectable _selectable;
    private Image _targetImage;
    private Material _runtimeMaterial;
    private Vector3 _baseScale = Vector3.one;

    private bool _isSelected;
    private bool _isHovered;
    private bool _isPressed;
    private bool _initialized;

    private float _shineTimer = -1f;
    private float _periodicTimer;
    private float _currentRimIntensity;
    private float _currentFocusAdd;

    private bool IsFocused => (_isSelected || _isHovered) && (_selectable == null || _selectable.IsInteractable());

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnEnable()
    {
        EnsureInitialized();
        ResetVisualImmediate();
    }

    private void OnDisable()
    {
        _isSelected = false;
        _isHovered = false;
        _isPressed = false;
        _shineTimer = -1f;
        ResetVisualImmediate();
    }

    private void OnDestroy()
    {
        if (_runtimeMaterial != null)
        {
            Destroy(_runtimeMaterial);
            _runtimeMaterial = null;
        }
    }

    public void ConfigureColors(Color normalColor, Color focusedColor, Color accentColor)
    {
        _outlineColor = accentColor;
        if (_runtimeMaterial != null)
        {
            _runtimeMaterial.SetColor(RimColorId, _outlineColor);
        }
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;

        _selectable = GetComponent<Selectable>();
        _targetImage = _selectable != null && _selectable.targetGraphic is Image img
            ? img
            : GetComponent<Image>();

        _baseScale = transform.localScale;
        if (_baseScale.sqrMagnitude < 0.001f)
        {
            _baseScale = Vector3.one;
        }

        // 金属光沢＆外枠シェーダーのインスタンスマテリアルを設定
        if (_targetImage != null)
        {
            Shader metallicShader = Shader.Find("UI/ModernMetallicButton");
            if (metallicShader != null)
            {
                _runtimeMaterial = new Material(metallicShader)
                {
                    name = $"UI_Metallic_{gameObject.name}"
                };
                _runtimeMaterial.SetColor(RimColorId, _outlineColor);
                _runtimeMaterial.SetFloat(RimThicknessId, _outlineThickness);
                _runtimeMaterial.SetFloat(ShineProgressId, -0.5f);
                _runtimeMaterial.SetFloat(ShineIntensityId, 0f);
                _runtimeMaterial.SetFloat(RimIntensityId, 0f);
                _runtimeMaterial.SetFloat(FocusAddId, 0f);
                _targetImage.material = _runtimeMaterial;
            }
        }
    }

    private void ResetVisualImmediate()
    {
        transform.localScale = _baseScale;
        _currentRimIntensity = 0f;
        _currentFocusAdd = 0f;

        if (_runtimeMaterial != null)
        {
            _runtimeMaterial.SetFloat(ShineIntensityId, 0f);
            _runtimeMaterial.SetFloat(ShineProgressId, -0.5f);
            _runtimeMaterial.SetFloat(RimIntensityId, 0f);
            _runtimeMaterial.SetFloat(FocusAddId, 0f);
        }
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (dt <= 0f) return;

        bool focused = IsFocused;
        bool interactable = _selectable == null || _selectable.IsInteractable();

        // スケール補間（選択時に少し大きくする）
        float targetScaleMul = 1f;
        if (!interactable)
        {
            targetScaleMul = 1f;
        }
        else if (_isPressed)
        {
            targetScaleMul = _pressedScale;
        }
        else if (focused)
        {
            targetScaleMul = _focusedScale;
        }

        Vector3 targetScale = _baseScale * targetScaleMul;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, 1f - Mathf.Exp(-_transitionSpeed * dt));

        // シェーダー演出（外枠・元色加算・金属シマー）の更新
        if (_runtimeMaterial != null)
        {
            float lerpFactor = 1f - Mathf.Exp(-_transitionSpeed * dt);
            _currentRimIntensity = Mathf.Lerp(_currentRimIntensity, focused ? 1f : 0f, lerpFactor);
            _currentFocusAdd = Mathf.Lerp(_currentFocusAdd, focused ? _focusSelfAdd : 0f, lerpFactor);

            if (focused)
            {
                _periodicTimer += dt;
                if (_periodicTimer >= _periodicShineInterval && _shineTimer < 0f)
                {
                    _periodicTimer = 0f;
                    TriggerShine();
                }
            }
            else
            {
                _periodicTimer = 0f;
            }

            float shineIntensity = 0f;
            float shineProgress = -0.5f;
            if (_shineTimer >= 0f)
            {
                _shineTimer += dt;
                float t = Mathf.Clamp01(_shineTimer / Mathf.Max(0.05f, _shineSweepDuration));
                shineProgress = Mathf.Lerp(-0.35f, 1.35f, t);
                shineIntensity = Mathf.Sin(t * Mathf.PI) * 0.9f;

                if (_shineTimer >= _shineSweepDuration)
                {
                    _shineTimer = -1f;
                }
            }

            _runtimeMaterial.SetColor(RimColorId, _outlineColor);
            _runtimeMaterial.SetFloat(RimThicknessId, _outlineThickness);
            _runtimeMaterial.SetFloat(RimIntensityId, _currentRimIntensity);
            _runtimeMaterial.SetFloat(FocusAddId, _currentFocusAdd);
            _runtimeMaterial.SetFloat(ShineProgressId, shineProgress);
            _runtimeMaterial.SetFloat(ShineIntensityId, shineIntensity);
        }
    }

    public void TriggerShine()
    {
        if (_selectable != null && !_selectable.IsInteractable()) return;
        _shineTimer = 0f;
        _periodicTimer = 0f;
    }

    public void OnSelect(BaseEventData eventData)
    {
        _isSelected = true;
        TriggerShine();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        _isSelected = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        TriggerShine();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        _isPressed = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_selectable != null && !_selectable.IsInteractable()) return;
        _isPressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isPressed = false;
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (_selectable != null && !_selectable.IsInteractable()) return;
        TriggerShine();
    }
}
