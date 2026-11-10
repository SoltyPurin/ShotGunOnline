#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 【デバッグ用】ショットガン連射（ラピッドファイア）機能およびデバッグUI
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
    [SerializeField] private bool _isRapidFireMode = false;

    [Tooltip("連射間隔（秒）。0.1なら毎秒10発")]
    [Range(0.03f, 0.5f)]
    [SerializeField] private float _fireInterval = 0.12f;

    [Tooltip("連射時のチャージ威力設定")]
    [SerializeField] private RapidChargeType _chargeType = RapidChargeType.Medium;

    [Tooltip("連射中にオーバーヒートを無効化（即時回復）するか")]
    [SerializeField] private bool _ignoreOverheat = true;

    [Tooltip("連射中にノックバック（反動）を無効化するか（定点で撃ち続けたい場合など）")]
    [SerializeField] private bool _ignoreKnockback = false;

    [Header("ショートカットキー設定")]
    [Tooltip("連射モードON/OFF切り替えキー")]
    [SerializeField] private KeyCode _toggleModeKey = KeyCode.F6;

    [Tooltip("連射専用キー（長押しで即連射）")]
    [SerializeField] private KeyCode _holdFireKey = KeyCode.R;

    [Tooltip("デバッグUIの表示/非表示（最小化）切り替えキー")]
    [SerializeField] private KeyCode _toggleUIKey = KeyCode.F8;

    [Header("UI設定")]
    [Tooltip("デバッグUIを表示するか（デフォルトOFF。F8キーでいつでも表示/非表示を切り替え可能）")]
    [SerializeField] private bool _showUI = false;
    [SerializeField] private bool _isMinimized = false;

    // 内部タイマーとステート
    private float _rapidFireTimer = 0f;
    private bool _wasHoldingFire = false;
    private bool _isUIButtonHolding = false;

    // GUIスタイル
    private GUIStyle _windowStyle;
    private GUIStyle _headerStyle;
    private GUIStyle _toggleActiveStyle;
    private GUIStyle _toggleNormalStyle;
    private GUIStyle _fireButtonStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _miniButtonStyle;
    private bool _stylesInitialized = false;

    private Rect _windowRect = new Rect(0, 0, 310, 390);

    private void Awake()
    {
        // エディタおよびDevelopment Build以外では即座に自身を破棄
        if (!Debug.isDebugBuild && !Application.isEditor)
        {
            Destroy(this);
            return;
        }

        if (_playerShot == null)
        {
            _playerShot = GetComponent<InputPlayerShot>();
        }
    }

    private void Start()
    {
        // 画面右下に初期配置
        float posX = Mathf.Max(10f, Screen.width - _windowRect.width - 20f);
        float posY = Mathf.Max(10f, Screen.height - _windowRect.height - 20f);
        _windowRect = new Rect(posX, posY, _windowRect.width, _windowRect.height);
    }

    private void Update()
    {
        if (_playerShot == null) return;

        // 自身が操作するプレイヤー（Owner）でなければ動作させない
        if (!_playerShot.IsOwner) return;

        // UI表示切替キー (F8)
        if (IsKeyDownLegacyOrNew(_toggleUIKey, Key.F8))
        {
            _showUI = !_showUI;
            Debug.Log($"<color=#00ff88>[ShotgunDebug]</color> デバッグUI: {(_showUI ? "表示 (ON)" : "非表示 (OFF)")}");
        }

        // 連射モード切替キー (F6)
        if (IsKeyDownLegacyOrNew(_toggleModeKey, Key.F6))
        {
            _isRapidFireMode = !_isRapidFireMode;
            Debug.Log($"<color=#00ff88>[ShotgunDebug]</color> 連射モード: {(_isRapidFireMode ? "ON" : "OFF")}");
        }

        // 長押し入力の判定
        // 1. 連射モードON時の通常射撃ボタン長押し
        bool isNormalFireHolding = _isRapidFireMode && _playerShot.IsShootButtonPressed;
        // 2. 専用連射キー（Rキー）の長押し
        bool isDedicatedKeyHolding = IsKeyHeldLegacyOrNew(_holdFireKey, Key.R);
        // 3. UIの連射ボタン長押し
        bool isUIHolding = _isUIButtonHolding;

        bool isHoldingFire = isNormalFireHolding || isDedicatedKeyHolding || isUIHolding;

        // InputPlayerShotに連射中であることを通知（通常チャージの干渉を防止）
        _playerShot.IsRapidFireActive = isHoldingFire;

        // 連射実行処理
        if (isHoldingFire)
        {
            // 押された瞬間（立ち上がり）に初弾を即座に発射
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

        float charge = 0f;
        bool isCrit = false;
        bool isMax = false;

        switch (_chargeType)
        {
            case RapidChargeType.Min:
                charge = 0.0f;
                break;
            case RapidChargeType.Medium:
                charge = 1.0f;
                break;
            case RapidChargeType.Max:
                charge = 2.0f;
                isMax = true;
                break;
            case RapidChargeType.Critical:
                charge = 2.4f;
                isCrit = true;
                break;
        }

        _playerShot.FireShotDebug(charge, isCrit, isMax, _ignoreKnockback, _ignoreOverheat);
    }

    private void InitStyles()
    {
        if (_stylesInitialized) return;

        _windowStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(10, 10, 10, 10)
        };

        Texture2D bgTex = new Texture2D(1, 1);
        bgTex.SetPixel(0, 0, new Color(0.08f, 0.1f, 0.14f, 0.92f));
        bgTex.Apply();
        _windowStyle.normal.background = bgTex;

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
        Texture2D activeTex = new Texture2D(1, 1);
        activeTex.SetPixel(0, 0, new Color(0.2f, 1f, 0.4f, 1f));
        activeTex.Apply();
        _toggleActiveStyle.normal.background = activeTex;

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
        Texture2D fireTex = new Texture2D(1, 1);
        fireTex.SetPixel(0, 0, new Color(0.85f, 0.25f, 0.2f, 0.95f));
        fireTex.Apply();
        _fireButtonStyle.normal.background = fireTex;

        _miniButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 10,
            padding = new RectOffset(4, 4, 2, 2)
        };

        _stylesInitialized = true;
    }

    private void OnGUI()
    {
        if (!_showUI || _playerShot == null || !_playerShot.IsOwner) return;

        InitStyles();

        // 最小化表示の場合
        if (_isMinimized)
        {
            float miniW = 160;
            float miniH = 34;
            float miniX = Mathf.Clamp(_windowRect.x, 10, Screen.width - miniW - 10);
            float miniY = Mathf.Clamp(_windowRect.y, 10, Screen.height - miniH - 10);
            Rect miniRect = new Rect(miniX, miniY, miniW, miniH);

            string miniTitle = _isRapidFireMode ? "連射: [ON]" : "連射: [OFF]";
            GUIStyle miniStyle = _isRapidFireMode ? _toggleActiveStyle : _toggleNormalStyle;
            if (GUI.Button(miniRect, $"{miniTitle} (展開)", miniStyle))
            {
                _isMinimized = false;
            }
            return;
        }

        // ウィンドウ表示
        _windowRect.x = Mathf.Clamp(_windowRect.x, 5, Screen.width - _windowRect.width - 5);
        _windowRect.y = Mathf.Clamp(_windowRect.y, 5, Screen.height - _windowRect.height - 5);
        _windowRect = GUI.Window(987654, _windowRect, DrawWindow, "", _windowStyle);
    }

    private void DrawWindow(int windowId)
    {
        // ヘッダー
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

        // 連射モードトグルボタン
        string toggleText = _isRapidFireMode
            ? "【連射モード: ON】(F6で切替)"
            : "【連射モード: OFF】(F6で切替)";
        GUIStyle currentToggleStyle = _isRapidFireMode ? _toggleActiveStyle : _toggleNormalStyle;

        if (GUILayout.Button(toggleText, currentToggleStyle, GUILayout.Height(30)))
        {
            _isRapidFireMode = !_isRapidFireMode;
        }

        GUILayout.Space(6);

        // UIからの長押し連射ボタン
        // RepeatButtonは押されているフレーム間ずっとtrueを返す
        bool isPressedThisFrame = GUILayout.RepeatButton("連射ボタン(長押し)", _fireButtonStyle, GUILayout.Height(36));
        _isUIButtonHolding = isPressedThisFrame;

        GUILayout.Space(6);

        // 連射間隔設定
        float rps = 1.0f / Mathf.Max(0.01f, _fireInterval);
        GUILayout.Label($"連射間隔: {_fireInterval:F2}秒 (秒間 {rps:F1}発)", _labelStyle);
        _fireInterval = GUILayout.HorizontalSlider(_fireInterval, 0.04f, 0.35f);

        // 間隔プリセット
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("超高速(0.06s)", _miniButtonStyle)) _fireInterval = 0.06f;
        if (GUILayout.Button("標準(0.12s)", _miniButtonStyle)) _fireInterval = 0.12f;
        if (GUILayout.Button("ゆったり(0.20s)", _miniButtonStyle)) _fireInterval = 0.20f;
        GUILayout.EndHorizontal();

        GUILayout.Space(6);

        // チャージ威力プリセット
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

        // デバッグトグル群
        _ignoreOverheat = GUILayout.Toggle(_ignoreOverheat, "オーバーヒート無効化", GUI.skin.toggle);
        _ignoreKnockback = GUILayout.Toggle(_ignoreKnockback, "ノックバック(反動)無効化", GUI.skin.toggle);

        GUILayout.Space(4);

        // オバヒ即時リセットボタン
        if (GUILayout.Button("オーバーヒート即時解除", _miniButtonStyle, GUILayout.Height(20)))
        {
            var overHeat = _playerShot.GetComponent<OverHeat>();
            if (overHeat != null)
            {
                overHeat.ResetOverHeat();
            }
        }

        GUILayout.Space(6);

        // ガイド
        GUIStyle guideStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 9,
            wordWrap = true
        };
        guideStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
        GUILayout.Label("【操作方法】\n・F6: 連射モードON/OFF (ON時は左クリック/Space長押しで連射)\n・Rキー長押し: いつでも即座に連射\n・F8: このUIの表示/非表示", guideStyle);

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
