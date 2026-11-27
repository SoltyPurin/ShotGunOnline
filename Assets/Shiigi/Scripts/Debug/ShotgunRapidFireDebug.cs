#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 【デバッグ用】ショットガン連射（ラピッドファイア）機能およびデバッグUI。
/// UnityエディタまたはDevelopment Buildの場合のみ動作。
/// リリースビルドでは自動的に無効化・破棄されます。
/// </summary>
[DisallowMultipleComponent]
public class ShotgunRapidFireDebug : MonoBehaviour
{
    public enum RapidChargeType
    {
        Min = 0,        // 最小チャージ (0.0f) 即射・拡散大
        Medium = 1,     // 中チャージ (1.0f)
        Max = 2,        // 最大チャージ (2.0f) 高威力・大反動
        Critical = 3    // クリティカル (2.4f) 特殊演出付き
    }

    [Header("コンポーネント参照")]
    [SerializeField] private InputPlayerShot _playerShot;

    [Header("連射設定")]
    [Tooltip("連射モードのON/OFF（ONの場合、通常射撃ボタン長押しで連射）")]
    [SerializeField] private bool _isRapidFireMode;

    [Tooltip("連射間隔（秒）。0.1なら毎秒10発")]
    [Range(0.03f, 0.5f)]
    [SerializeField] private float _fireInterval = 0.12f;

    [Tooltip("連射時のチャージ威力設定")]
    [SerializeField] private RapidChargeType _chargeType = RapidChargeType.Medium;

    [Tooltip("連射中にオーバーヒートを無効化（即時回復）するか")]
    [SerializeField] private bool _ignoreOverheat = true;

    [Tooltip("連射中にノックバック（反動）を無効化するか（定点で撃ち続けたい場合など）")]
    [SerializeField] private bool _ignoreKnockback;

    [Header("ショートカットキー設定")]
    [Tooltip("連射モードON/OFF切り替えキー")]
    [SerializeField] private KeyCode _toggleModeKey = KeyCode.F6;

    [Tooltip("連射専用キー（長押しで即連射）")]
    [SerializeField] private KeyCode _holdFireKey = KeyCode.R;

    [Tooltip("デバッグUIの表示/非表示（最小化）切り替えキー")]
    [SerializeField] private KeyCode _toggleUIKey = KeyCode.F8;

    [Header("UI設定")]
    [Tooltip("デバッグUIを表示するか（デフォルトOFF。F8キーでいつでも表示/非表示を切り替え可能）")]
    [SerializeField] private bool _showUI;
    [SerializeField] private bool _isMinimized;

    private OverHeat _cachedOverHeat;
    private float _rapidFireTimer;
    private bool _wasHoldingFire;
    private bool _isUIButtonHolding;

    // GUIスタイルと動的生成テクスチャ（OnDestroyで破棄してリークを防止）
    private GUIStyle _windowStyle;
    private GUIStyle _headerStyle;
    private GUIStyle _toggleActiveStyle;
    private GUIStyle _toggleNormalStyle;
    private GUIStyle _fireButtonStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _miniButtonStyle;
    private GUIStyle _guideStyle;
    private Texture2D _bgTex;
    private Texture2D _activeTex;
    private Texture2D _fireTex;
    private bool _stylesInitialized;

    private Rect _windowRect = new Rect(0, 0, 310, 390);

    private void Awake()
    {
        if (!Debug.isDebugBuild && !Application.isEditor)
        {
            Destroy(this);
            return;
        }

        if (_playerShot == null)
        {
            _playerShot = GetComponent<InputPlayerShot>();
        }
        if (_playerShot != null)
        {
            _cachedOverHeat = _playerShot.GetComponent<OverHeat>();
        }
    }

    private void Start()
    {
        float posX = Mathf.Max(10f, Screen.width - _windowRect.width - 20f);
        float posY = Mathf.Max(10f, Screen.height - _windowRect.height - 20f);
        _windowRect = new Rect(posX, posY, _windowRect.width, _windowRect.height);
    }

    private void OnDestroy()
    {
        if (_bgTex != null) Destroy(_bgTex);
        if (_activeTex != null) Destroy(_activeTex);
        if (_fireTex != null) Destroy(_fireTex);
    }

    private void Update()
    {
        if (_playerShot == null || !_playerShot.IsOwner) return;

        if (IsKeyDownLegacyOrNew(_toggleUIKey, Key.F8))
        {
            _showUI = !_showUI;
            Debug.Log($"<color=#00ff88>[ShotgunDebug]</color> デバッグUI: {(_showUI ? "表示 (ON)" : "非表示 (OFF)")}");
        }

        if (IsKeyDownLegacyOrNew(_toggleModeKey, Key.F6))
        {
            _isRapidFireMode = !_isRapidFireMode;
            Debug.Log($"<color=#00ff88>[ShotgunDebug]</color> 連射モード: {(_isRapidFireMode ? "ON" : "OFF")}");
        }

        bool isNormalFireHolding = _isRapidFireMode && _playerShot.IsShootButtonPressed;
        bool isDedicatedKeyHolding = IsKeyHeldLegacyOrNew(_holdFireKey, Key.R);
        bool isHoldingFire = isNormalFireHolding || isDedicatedKeyHolding || _isUIButtonHolding;

        _playerShot.IsRapidFireActive = isHoldingFire;

        if (isHoldingFire)
        {
            if (!_wasHoldingFire)
            {
                TriggerRapidShot();
                _rapidFireTimer = 0f;
            }
            else
            {
                _rapidFireTimer += Time.deltaTime;
                if (_rapidFireTimer >= _fireInterval)
                {
                    TriggerRapidShot();
                    _rapidFireTimer = 0f;
                }
            }
        }
        else
        {
            _rapidFireTimer = 0f;
        }

        _wasHoldingFire = isHoldingFire;
    }

    /// <summary>
    /// 連射ショットを発射
    /// </summary>
    public void TriggerRapidShot()
    {
        if (_playerShot == null) return;

        (float charge, bool isCrit, bool isMax) = _chargeType switch
        {
            RapidChargeType.Min => (0.0f, false, false),
            RapidChargeType.Medium => (1.0f, false, false),
            RapidChargeType.Max => (2.0f, false, true),
            RapidChargeType.Critical => (2.4f, true, false),
            _ => (1.0f, false, false)
        };

        _playerShot.FireShotDebug(charge, isCrit, isMax, _ignoreKnockback, _ignoreOverheat);
    }

    private static Texture2D CreateSolidTexture(Color color)
    {
        var tex = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }

    private void InitStyles()
    {
        if (_stylesInitialized) return;

        _bgTex = CreateSolidTexture(new Color(0.08f, 0.1f, 0.14f, 0.92f));
        _activeTex = CreateSolidTexture(new Color(0.2f, 1f, 0.4f, 1f));
        _fireTex = CreateSolidTexture(new Color(0.85f, 0.25f, 0.2f, 0.95f));

        _windowStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(10, 10, 10, 10)
        };
        _windowStyle.normal.background = _bgTex;

        _headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        _headerStyle.normal.textColor = new Color(0.3f, 1f, 0.6f);

        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleLeft
        };
        _labelStyle.normal.textColor = Color.white;

        _toggleActiveStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold
        };
        _toggleActiveStyle.normal.textColor = Color.black;
        _toggleActiveStyle.normal.background = _activeTex;

        _toggleNormalStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12
        };
        _toggleNormalStyle.normal.textColor = Color.white;

        _fireButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold
        };
        _fireButtonStyle.normal.textColor = Color.white;
        _fireButtonStyle.normal.background = _fireTex;

        _miniButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 10,
            padding = new RectOffset(4, 4, 2, 2)
        };

        _guideStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 9,
            wordWrap = true
        };
        _guideStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

        _stylesInitialized = true;
    }

    private void OnGUI()
    {
        if (!_showUI || _playerShot == null || !_playerShot.IsOwner) return;

        InitStyles();

        if (_isMinimized)
        {
            const float miniW = 160f;
            const float miniH = 34f;
            float miniX = Mathf.Clamp(_windowRect.x, 10f, Screen.width - miniW - 10f);
            float miniY = Mathf.Clamp(_windowRect.y, 10f, Screen.height - miniH - 10f);
            Rect miniRect = new Rect(miniX, miniY, miniW, miniH);

            string miniTitle = _isRapidFireMode ? "連射: [ON] (展開)" : "連射: [OFF] (展開)";
            GUIStyle miniStyle = _isRapidFireMode ? _toggleActiveStyle : _toggleNormalStyle;
            if (GUI.Button(miniRect, miniTitle, miniStyle))
            {
                _isMinimized = false;
            }
            return;
        }

        _windowRect.x = Mathf.Clamp(_windowRect.x, 5f, Screen.width - _windowRect.width - 5f);
        _windowRect.y = Mathf.Clamp(_windowRect.y, 5f, Screen.height - _windowRect.height - 5f);
        _windowRect = GUI.Window(987654, _windowRect, DrawWindow, string.Empty, _windowStyle);
    }

    private void DrawWindow(int windowId)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("[デバッグ] ショットガン連射", _headerStyle);
        if (GUILayout.Button("━", _miniButtonStyle, GUILayout.Width(22), GUILayout.Height(18)))
        {
            _isMinimized = true;
        }
        if (GUILayout.Button("✕", _miniButtonStyle, GUILayout.Width(22), GUILayout.Height(18)))
        {
            _showUI = false;
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(6);

        string toggleText = _isRapidFireMode
            ? "【連射モード: ON】(F6で切替)"
            : "【連射モード: OFF】(F6で切替)";
        GUIStyle currentToggleStyle = _isRapidFireMode ? _toggleActiveStyle : _toggleNormalStyle;

        if (GUILayout.Button(toggleText, currentToggleStyle, GUILayout.Height(30)))
        {
            _isRapidFireMode = !_isRapidFireMode;
        }

        GUILayout.Space(6);

        _isUIButtonHolding = GUILayout.RepeatButton("連射ボタン(長押し)", _fireButtonStyle, GUILayout.Height(36));

        GUILayout.Space(6);

        float rps = 1.0f / Mathf.Max(0.01f, _fireInterval);
        GUILayout.Label($"連射間隔: {_fireInterval:F2}秒 (秒間 {rps:F1}発)", _labelStyle);
        _fireInterval = GUILayout.HorizontalSlider(_fireInterval, 0.04f, 0.35f);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("超高速(0.06s)", _miniButtonStyle)) _fireInterval = 0.06f;
        if (GUILayout.Button("標準(0.12s)", _miniButtonStyle)) _fireInterval = 0.12f;
        if (GUILayout.Button("ゆったり(0.20s)", _miniButtonStyle)) _fireInterval = 0.20f;
        GUILayout.EndHorizontal();

        GUILayout.Space(6);

        GUILayout.Label($"チャージ威力: {_chargeType}", _labelStyle);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("最小(0.0)", _chargeType == RapidChargeType.Min ? _toggleActiveStyle : _miniButtonStyle))
            _chargeType = RapidChargeType.Min;
        if (GUILayout.Button("中(1.0)", _chargeType == RapidChargeType.Medium ? _toggleActiveStyle : _miniButtonStyle))
            _chargeType = RapidChargeType.Medium;
        if (GUILayout.Button("最大(2.0)", _chargeType == RapidChargeType.Max ? _toggleActiveStyle : _miniButtonStyle))
            _chargeType = RapidChargeType.Max;
        if (GUILayout.Button("会心(2.4)", _chargeType == RapidChargeType.Critical ? _toggleActiveStyle : _miniButtonStyle))
            _chargeType = RapidChargeType.Critical;
        GUILayout.EndHorizontal();

        GUILayout.Space(6);

        _ignoreOverheat = GUILayout.Toggle(_ignoreOverheat, "オーバーヒート無効化", GUI.skin.toggle);
        _ignoreKnockback = GUILayout.Toggle(_ignoreKnockback, "ノックバック(反動)無効化", GUI.skin.toggle);

        GUILayout.Space(4);

        if (GUILayout.Button("オーバーヒート即時解除", _miniButtonStyle, GUILayout.Height(20)))
        {
            if (_cachedOverHeat == null && _playerShot != null)
            {
                _cachedOverHeat = _playerShot.GetComponent<OverHeat>();
            }
            _cachedOverHeat?.ResetOverHeat();
        }

        GUILayout.Space(6);

        GUILayout.Label("【操作方法】\n・F6: 連射モードON/OFF (ON時は左クリック/Space長押しで連射)\n・Rキー長押し: いつでも即座に連射\n・F8: このUIの表示/非表示", _guideStyle);

        GUI.DragWindow(new Rect(0, 0, _windowRect.width, 24));
    }

    private static bool IsKeyDownLegacyOrNew(KeyCode legacyKey, Key newKey)
    {
        if (Keyboard.current != null && Keyboard.current[newKey].wasPressedThisFrame)
        {
            return true;
        }
        return Input.GetKeyDown(legacyKey);
    }

    private static bool IsKeyHeldLegacyOrNew(KeyCode legacyKey, Key newKey)
    {
        if (Keyboard.current != null && Keyboard.current[newKey].isPressed)
        {
            return true;
        }
        return Input.GetKey(legacyKey);
    }
}
#endif
