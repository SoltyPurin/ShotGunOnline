using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// ボタン選択・ホバー時の金属光沢およびスケール演出コンポーネント。
/// </summary>
[DisallowMultipleComponent]
public class UIButtonMetallicVisual : MonoBehaviour,
    IMeshModifier,
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
    private static readonly int RectSizeId = Shader.PropertyToID("_RectSize");
    private static readonly int UseSpriteAlphaId = Shader.PropertyToID("_UseSpriteAlpha");
    private static readonly int SpriteUVBoundsId = Shader.PropertyToID("_SpriteUVBounds");
    private static readonly int FocusAddId = Shader.PropertyToID("_FocusAdd");
    private static readonly int UseLocalUVId = Shader.PropertyToID("_UseLocalUV");

    private static readonly List<IMeshModifier> s_modifierBuffer = new(8);
    private static Shader s_metallicShader;
    private const float RestEpsilon = 0.0005f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        s_modifierBuffer.Clear();
        s_metallicShader = null;
    }

    [Header("選択時の外枠・発色設定")]
    [SerializeField] private bool _useOutline = true;
    [SerializeField] private Color _outlineColor = new Color(1.0f, 0.96f, 0.72f, 1.0f);
    [SerializeField, Range(1f, 16f)] private float _outlineThickness = 5f;
    [SerializeField, Range(0f, 1f)] private float _focusSelfAdd = 0.28f;

    [Header("アニメーション設定")]
    [SerializeField] private float _focusedScale = 1.08f;
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
    private bool _useLocalUV;
    private bool _canvasChannelReady;
    private bool _isVisualAtRest;
    private bool _staticShaderPropsDirty = true;
    private Sprite _lastCachedSprite;
    private Vector2 _lastCachedRectSize;

    private float _shineTimer = -1f;
    private float _periodicTimer;
    private float _currentRimIntensity;
    private float _currentFocusAdd;
    private float _clickBounceTimer = -1f;
    private float _clickBounceDuration = 0.08f;
    private float _clickBouncePressedScale = 0.96f;

    /// <summary>
    /// 指定されたボタンに金属光沢ビジュアルを付与（既存なら取得）する
    /// </summary>
    public static UIButtonMetallicVisual Attach(Button button)
    {
        if (button == null) return null;

        if (!button.TryGetComponent<UIButtonMetallicVisual>(out var visual))
        {
            visual = button.gameObject.AddComponent<UIButtonMetallicVisual>();
        }
        return visual;
    }

    private bool IsFocused
    {
        get
        {
            if (_selectable != null && !_selectable.IsInteractable()) return false;
            bool isEventSystemSelected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
            return _isSelected || _isHovered || isEventSystemSelected;
        }
    }

    public void SetUseOutline(bool useOutline)
    {
        _useOutline = useOutline;
        _isVisualAtRest = false;
    }

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnEnable()
    {
        EnsureInitialized();
        EnsureCanvasTexCoord1();
        _staticShaderPropsDirty = true;
        ResetVisualImmediate();
    }

    private void OnTransformParentChanged()
    {
        _canvasChannelReady = false;
        EnsureCanvasTexCoord1();
    }

    private void OnDisable()
    {
        _isSelected = false;
        _isHovered = false;
        _isPressed = false;
        _shineTimer = -1f;
        _clickBounceTimer = -1f;
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

    /// <summary>
    /// クリック時のスケールバウンスを本コンポーネントのスケール計算へ統合して再生する（UIButtonSound との競合防止）
    /// </summary>
    public void TriggerClickBounce(float pressedScale, float duration)
    {
        _clickBouncePressedScale = pressedScale;
        _clickBounceDuration = Mathf.Max(0.02f, duration);
        _clickBounceTimer = 0f;
        _isVisualAtRest = false;
    }

    private Material GetExtraRenderingMaterial()
    {
        if (_targetImage == null) return null;
        var renderingMat = _targetImage.materialForRendering;
        return renderingMat != null && renderingMat != _runtimeMaterial ? renderingMat : null;
    }

    private void SetMaterialFloat(int propertyId, float value)
    {
        if (_runtimeMaterial != null) _runtimeMaterial.SetFloat(propertyId, value);
        var renderingMat = GetExtraRenderingMaterial();
        if (renderingMat != null) renderingMat.SetFloat(propertyId, value);
    }

    private void SetMaterialVector(int propertyId, Vector4 value)
    {
        if (_runtimeMaterial != null) _runtimeMaterial.SetVector(propertyId, value);
        var renderingMat = GetExtraRenderingMaterial();
        if (renderingMat != null) renderingMat.SetVector(propertyId, value);
    }

    private void SetMaterialColor(int propertyId, Color value)
    {
        if (_runtimeMaterial != null) _runtimeMaterial.SetColor(propertyId, value);
        var renderingMat = GetExtraRenderingMaterial();
        if (renderingMat != null) renderingMat.SetColor(propertyId, value);
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

        if (_targetImage != null)
        {
            _useLocalUV = (_targetImage.gameObject == gameObject);

            if (s_metallicShader == null)
            {
                s_metallicShader = Shader.Find("UI/ModernMetallicButton");
            }
            if (s_metallicShader != null)
            {
                _runtimeMaterial = new Material(s_metallicShader)
                {
                    name = $"UI_Metallic_{gameObject.name}"
                };
                _runtimeMaterial.SetColor(RimColorId, _outlineColor);
                _runtimeMaterial.SetFloat(RimThicknessId, _outlineThickness);
                _runtimeMaterial.SetFloat(ShineProgressId, -0.5f);
                _runtimeMaterial.SetFloat(ShineIntensityId, 0f);
                _runtimeMaterial.SetFloat(RimIntensityId, 0f);
                _runtimeMaterial.SetFloat(FocusAddId, 0f);
                _runtimeMaterial.SetFloat(UseLocalUVId, _useLocalUV ? 1f : 0f);
                _targetImage.material = _runtimeMaterial;
                _staticShaderPropsDirty = true;
            }

            EnsureCanvasTexCoord1();
        }
    }

    private void EnsureCanvasTexCoord1()
    {
        if (!_useLocalUV || _targetImage == null) return;

        Canvas canvas = _targetImage.canvas;
        if (canvas != null)
        {
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
            if (canvas.rootCanvas != null)
            {
                canvas.rootCanvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
            }
            _canvasChannelReady = true;
        }

        _targetImage.SetVerticesDirty();
    }

    public void ModifyMesh(Mesh mesh)
    {
    }

    public void ModifyMesh(VertexHelper vh)
    {
        if (!isActiveAndEnabled) return;

        RectTransform rt = _targetImage != null ? _targetImage.rectTransform : transform as RectTransform;
        if (rt == null) return;

        Rect rect = rt.rect;
        if (rect.width <= 0.0001f || rect.height <= 0.0001f) return;

        float invWidth = 1f / rect.width;
        float invHeight = 1f / rect.height;

        int multiplier = 1;
        s_modifierBuffer.Clear();
        GetComponents(s_modifierBuffer);
        for (int m = 0; m < s_modifierBuffer.Count; m++)
        {
            var mod = s_modifierBuffer[m];
            if (ReferenceEquals(mod, this)) break;
            if (mod is Behaviour behaviour && !behaviour.isActiveAndEnabled) continue;

            if (mod is Outline)
            {
                multiplier *= 5;
            }
            else if (mod is Shadow)
            {
                multiplier *= 2;
            }
        }
        s_modifierBuffer.Clear();

        int count = vh.currentVertCount;
        int mainStartIndex = (multiplier > 1 && count % multiplier == 0)
            ? count - (count / multiplier)
            : 0;

        UIVertex vertex = default;
        for (int i = 0; i < count; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            float u = (vertex.position.x - rect.xMin) * invWidth;
            float v = (vertex.position.y - rect.yMin) * invHeight;
            float effectMask = i >= mainStartIndex ? 1f : 0f;
            vertex.uv1 = new Vector4(u, v, effectMask, 0f);
            vh.SetUIVertex(vertex, i);
        }
    }

    private void ResetVisualImmediate()
    {
        transform.localScale = _baseScale;
        _currentRimIntensity = 0f;
        _currentFocusAdd = 0f;
        _clickBounceTimer = -1f;
        _isVisualAtRest = false;

        if (_runtimeMaterial != null)
        {
            SetMaterialFloat(ShineIntensityId, 0f);
            SetMaterialFloat(ShineProgressId, -0.5f);
            SetMaterialFloat(RimIntensityId, 0f);
            SetMaterialFloat(FocusAddId, 0f);
        }
    }

    private void Update()
    {
        if (_useLocalUV && !_canvasChannelReady && _targetImage != null && _targetImage.canvas != null)
        {
            EnsureCanvasTexCoord1();
        }

        float dt = Time.unscaledDeltaTime;
        if (dt <= 0f) return;

        bool focused = IsFocused;
        bool isIdleTarget = !focused && !_isPressed && _shineTimer < 0f && _clickBounceTimer < 0f;

        // 非フォーカスかつアニメーション完全収束済みなら毎フレームの計算・マテリアル書き込みをスキップ
        if (isIdleTarget && _isVisualAtRest)
        {
            return;
        }
        _isVisualAtRest = false;

        bool interactable = _selectable == null || _selectable.IsInteractable();

        float targetScaleMul = 1f;
        if (interactable)
        {
            if (_isPressed) targetScaleMul = _pressedScale;
            else if (focused) targetScaleMul = _focusedScale;
        }

        if (_clickBounceTimer >= 0f)
        {
            _clickBounceTimer += dt;
            float t = Mathf.Clamp01(_clickBounceTimer / _clickBounceDuration);
            float bounceFactor = Mathf.Sin(t * Mathf.PI);
            float bounceMul = Mathf.Lerp(1f, _clickBouncePressedScale, bounceFactor);
            targetScaleMul *= bounceMul;

            if (_clickBounceTimer >= _clickBounceDuration)
            {
                _clickBounceTimer = -1f;
            }
        }

        float lerpFactor = 1f - Mathf.Exp(-_transitionSpeed * dt);
        Vector3 targetScale = _baseScale * targetScaleMul;
        Vector3 nextScale = Vector3.Lerp(transform.localScale, targetScale, lerpFactor);
        bool scaleSettled = (nextScale - targetScale).sqrMagnitude < RestEpsilon * RestEpsilon;
        transform.localScale = scaleSettled ? targetScale : nextScale;

        if (_runtimeMaterial != null)
        {
            UpdateStaticMaterialPropertiesIfNeeded();

            float targetRim = (_useOutline && focused) ? (0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 5.5f)) : 0f;
            float targetFocusAdd = focused ? _focusSelfAdd : 0f;

            _currentRimIntensity = Mathf.Lerp(_currentRimIntensity, targetRim, lerpFactor);
            _currentFocusAdd = Mathf.Lerp(_currentFocusAdd, targetFocusAdd, lerpFactor);

            if (!focused && _currentRimIntensity < RestEpsilon) _currentRimIntensity = 0f;
            if (!focused && _currentFocusAdd < RestEpsilon) _currentFocusAdd = 0f;

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

            SetMaterialFloat(RimIntensityId, _currentRimIntensity);
            SetMaterialFloat(FocusAddId, _currentFocusAdd);
            SetMaterialFloat(ShineProgressId, shineProgress);
            SetMaterialFloat(ShineIntensityId, shineIntensity);

            if (isIdleTarget && scaleSettled && _currentRimIntensity == 0f && _currentFocusAdd == 0f && _shineTimer < 0f)
            {
                _isVisualAtRest = true;
            }
        }
        else if (isIdleTarget && scaleSettled)
        {
            _isVisualAtRest = true;
        }
    }

    private void UpdateStaticMaterialPropertiesIfNeeded()
    {
        RectTransform rt = _targetImage != null ? _targetImage.rectTransform : transform as RectTransform;
        Vector2 rectSize = rt != null ? rt.rect.size : new Vector2(160f, 60f);
        Sprite activeSprite = _targetImage != null ? _targetImage.sprite : null;

        bool sizeChanged = (rectSize - _lastCachedRectSize).sqrMagnitude > 0.01f;
        bool spriteChanged = activeSprite != _lastCachedSprite;

        if (!_staticShaderPropsDirty && !sizeChanged && !spriteChanged) return;

        _staticShaderPropsDirty = false;
        _lastCachedRectSize = rectSize;
        _lastCachedSprite = activeSprite;

        bool useSpriteAlpha = activeSprite != null && _targetImage.type != Image.Type.Tiled;
        Vector4 uvBounds = activeSprite != null
            ? UnityEngine.Sprites.DataUtility.GetOuterUV(activeSprite)
            : new Vector4(0f, 0f, 1f, 1f);

        SetMaterialColor(RimColorId, _outlineColor);
        SetMaterialFloat(RimThicknessId, _outlineThickness);
        SetMaterialVector(RectSizeId, new Vector4(Mathf.Max(1f, rectSize.x), Mathf.Max(1f, rectSize.y), 0f, 0f));
        SetMaterialFloat(UseSpriteAlphaId, useSpriteAlpha ? 1f : 0f);
        SetMaterialVector(SpriteUVBoundsId, uvBounds);
    }

    public void TriggerShine()
    {
        if (_selectable != null && !_selectable.IsInteractable()) return;
        _shineTimer = 0f;
        _periodicTimer = 0f;
        _isVisualAtRest = false;
    }

    public void OnSelect(BaseEventData eventData)
    {
        _isSelected = true;
        _isVisualAtRest = false;
        TriggerShine();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        _isSelected = false;
        _isVisualAtRest = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        _isVisualAtRest = false;
        TriggerShine();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        _isPressed = false;
        _isVisualAtRest = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_selectable != null && !_selectable.IsInteractable()) return;
        _isPressed = true;
        _isVisualAtRest = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isPressed = false;
        _isVisualAtRest = false;
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (_selectable != null && !_selectable.IsInteractable()) return;
        TriggerShine();
    }
}
