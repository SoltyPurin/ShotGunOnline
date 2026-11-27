using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 古代アストロラーベ風ローディング演出を制御するアニメーションコンポーネント。
/// 8つの軌道ダイヤの追従明滅・回転と、背景リング／中央コアの呼吸演出を
/// Time.unscaledDeltaTime ベースで滑らかに再生します。
/// </summary>
public class LoadingAnimation : MonoBehaviour
{
    [Header("軌道ダイヤ設定")]
    [SerializeField] private float _orbitRadius = 50f;
    [SerializeField] private float _chasePeriod = 1.05f;
    [SerializeField] private float _orbitRotateSpeed = -24f;
    [SerializeField] private float _minRuneAlpha = 0.16f;
    [SerializeField] private float _minRuneScale = 0.78f;
    [SerializeField] private float _maxRuneScale = 1.24f;

    [Header("アストロラーベ装飾連動（任意）")]
    [SerializeField] private RectTransform _outerRingA;
    [SerializeField] private RectTransform _outerRingB;
    [SerializeField] private RectTransform _centerOuter;
    [SerializeField] private RectTransform _centerCore;
    [SerializeField] private float _outerRingASpeed = -18f;
    [SerializeField] private float _outerRingBSpeed = 12f;
    [SerializeField] private float _centerOuterSpeed = 30f;
    [SerializeField] private float _corePulseSpeed = 3.2f;

    private RectTransform _selfRect;
    private Image[] _runes;
    private RectTransform[] _runeRects;
    private Color[] _baseColors;
    private Image _centerCoreImage;
    private Color _centerCoreBaseColor = Color.white;
    private float _outerRingAStartZ;
    private float _outerRingBStartZ;
    private float _centerOuterStartZ;
    private float _elapsed;
    private bool _initialized;

    private void Awake()
    {
        InitializeIfNeeded();
    }

    private void OnEnable()
    {
        InitializeIfNeeded();
        _elapsed = 0f;
        ApplyVisualState(0f);
    }

    private void Update()
    {
        if (!_initialized) return;
        _elapsed += Time.unscaledDeltaTime;
        ApplyVisualState(_elapsed);
    }

    private void InitializeIfNeeded()
    {
        if (_initialized) return;

        _selfRect = GetComponent<RectTransform>();
        Image[] allImages = GetComponentsInChildren<Image>(true);
        var filteredRunes = new System.Collections.Generic.List<Image>(allImages.Length);
        for (int i = 0; i < allImages.Length; i++)
        {
            Image img = allImages[i];
            if (img == null || img.gameObject == gameObject) continue;
            RectTransform rt = img.rectTransform;
            if (rt == _outerRingA || rt == _outerRingB || rt == _centerOuter || rt == _centerCore) continue;
            filteredRunes.Add(img);
        }

        _runes = filteredRunes.ToArray();
        _runeRects = new RectTransform[_runes.Length];
        _baseColors = new Color[_runes.Length];

        for (int i = 0; i < _runes.Length; i++)
        {
            if (_runes[i] == null) continue;
            _runeRects[i] = _runes[i].rectTransform;
            float angle = -2f * Mathf.PI * i / Mathf.Max(1, _runes.Length);
            _runeRects[i].anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _orbitRadius;
            Color c = _runes[i].color;
            _baseColors[i] = new Color(c.r, c.g, c.b, 1f);
        }

        if (_outerRingA != null)
        {
            _outerRingAStartZ = _outerRingA.localEulerAngles.z;
        }
        if (_outerRingB != null)
        {
            _outerRingBStartZ = _outerRingB.localEulerAngles.z;
        }
        if (_centerOuter != null)
        {
            _centerOuterStartZ = _centerOuter.localEulerAngles.z;
        }
        if (_centerCore != null)
        {
            _centerCoreImage = _centerCore.GetComponent<Image>();
            if (_centerCoreImage != null)
            {
                Color c = _centerCoreImage.color;
                _centerCoreBaseColor = new Color(c.r, c.g, c.b, 1f);
            }
        }

        _initialized = true;
    }

    private void ApplyVisualState(float t)
    {
        if (_selfRect != null)
        {
            _selfRect.localEulerAngles = new Vector3(0f, 0f, _orbitRotateSpeed * t);
        }

        if (_runes != null && _runes.Length > 0)
        {
            float period = Mathf.Max(0.2f, _chasePeriod);
            float phase = (t / period) % 1f;
            int count = _runes.Length;

            for (int i = 0; i < count; i++)
            {
                Image rune = _runes[i];
                if (rune == null) continue;

                // 時計回りに先頭が光り、尾を引くチェイス波形（0..1）
                float slot = (float)i / count;
                float dist = (phase - slot + 1f) % 1f;
                float intensity = Mathf.Pow(1f - dist, 2.2f);

                Color c = _baseColors[i];
                c.a = Mathf.Lerp(_minRuneAlpha, 1f, intensity);
                rune.color = c;

                float scale = Mathf.Lerp(_minRuneScale, _maxRuneScale, intensity);
                _runeRects[i].localScale = new Vector3(scale, scale, 1f);
            }
        }

        if (_outerRingA != null)
        {
            _outerRingA.localEulerAngles = new Vector3(0f, 0f, _outerRingAStartZ + _outerRingASpeed * t);
        }
        if (_outerRingB != null)
        {
            _outerRingB.localEulerAngles = new Vector3(0f, 0f, _outerRingBStartZ + _outerRingBSpeed * t);
        }
        if (_centerOuter != null)
        {
            _centerOuter.localEulerAngles = new Vector3(0f, 0f, _centerOuterStartZ + _centerOuterSpeed * t);
        }
        if (_centerCore != null)
        {
            float breath = 0.5f + 0.5f * Mathf.Sin(t * _corePulseSpeed);
            float s = Mathf.Lerp(0.84f, 1.22f, breath);
            _centerCore.localScale = new Vector3(s, s, 1f);
            if (_centerCoreImage != null)
            {
                Color c = _centerCoreBaseColor;
                c.a = Mathf.Lerp(0.68f, 1f, breath);
                _centerCoreImage.color = c;
            }
        }
    }
}
