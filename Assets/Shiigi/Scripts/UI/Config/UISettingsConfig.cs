using System;
using UnityEngine;

/// <summary>
/// ボタンUIの役割タイプ
/// </summary>
public enum UIButtonType
{
    Default,    // 決定・標準アクション（Enter SE）
    Cancel,     // キャンセル・戻る・閉じる（Cancel SE）
    SubAction,  // 更新・サブ操作（Enter SE / 軽量演出）
    Custom      // ボタン個別のカスタム設定を使用
}

/// <summary>
/// パネル・ウィンドウUIの表示タイプ
/// </summary>
public enum UIViewType
{
    DefaultWindow,  // 標準ウィンドウ・ロビー画面
    ModalDialog,    // モーダル確認ダイアログ・設定ダイアログ
    OverlayDrawer,  // HUD・オーバーレイ通知
    LoadingOverlay, // 通信中・処理中ローディング表示
    Custom          // ビュー個別のカスタム設定を使用
}

/// <summary>
/// ボタンタイプごとのスタイル定義（アニメーション・SE設定）
/// </summary>
[Serializable]
public struct ButtonTypeStyle
{
    [Tooltip("クリック時に鳴らすSE")]
    public UISoundType ClickSound;

    [Tooltip("フォーカス（選択）時にSEを鳴らすか")]
    public bool PlayHoverSound;

    [Tooltip("フォーカス時に鳴らすSE")]
    public UISoundType HoverSound;

    [Tooltip("クリック時にスケールバウンス演出を行うか")]
    public bool UseScaleBounce;

    [Tooltip("クリック時の縮小スケール")]
    public float PressedScale;

    [Tooltip("スケール演出の時間（秒）")]
    public float BounceDuration;

    public static ButtonTypeStyle CreateDefault(UISoundType clickSound, bool bounce = true)
    {
        return new ButtonTypeStyle
        {
            ClickSound = clickSound,
            PlayHoverSound = false,
            HoverSound = UISoundType.CursorMove,
            UseScaleBounce = bounce,
            PressedScale = 0.95f,
            BounceDuration = 0.08f
        };
    }
}

/// <summary>
/// ビュータイプごとのスタイル定義（表示/非表示アニメーション・SE設定）
/// </summary>
[Serializable]
public struct ViewTypeStyle
{
    [Tooltip("表示アニメーションの時間（秒）")]
    public float ShowDuration;

    [Tooltip("非表示アニメーションの時間（秒）")]
    public float HideDuration;

    [Tooltip("表示時に拡大するポップ演出を行うか")]
    public bool UseScalePop;

    [Tooltip("ポップ開始時のスケール")]
    public float StartScale;

    [Tooltip("表示開始時のSE")]
    public UISoundType ShowSound;

    [Tooltip("非表示開始時のSE")]
    public UISoundType HideSound;

    public static ViewTypeStyle CreateDefault(float showDuration = 0.16f, float hideDuration = 0.12f, float startScale = 0.95f)
    {
        return new ViewTypeStyle
        {
            ShowDuration = showDuration,
            HideDuration = hideDuration,
            UseScalePop = true,
            StartScale = startScale,
            ShowSound = UISoundType.None,
            HideSound = UISoundType.None
        };
    }
}

/// <summary>
/// プロジェクト全体のUIアニメーションやSEを一元管理する設定アセット。
/// Resources/UISettingsConfig から自動ロードされます。
/// </summary>
[CreateAssetMenu(fileName = "UISettingsConfig", menuName = "UI/Settings Config")]
public class UISettingsConfig : ScriptableObject
{
    private const string ResourcePath = "UISettingsConfig";
    private static UISettingsConfig _instance;

    [Header("ボタンタイプ別スタイル設定")]
    [SerializeField]
    private ButtonTypeStyle _defaultButtonStyle = ButtonTypeStyle.CreateDefault(UISoundType.Enter, true);

    [SerializeField]
    private ButtonTypeStyle _cancelButtonStyle = ButtonTypeStyle.CreateDefault(UISoundType.Cancel, true);

    [SerializeField]
    private ButtonTypeStyle _subActionButtonStyle = ButtonTypeStyle.CreateDefault(UISoundType.Enter, false);

    [Header("ビュータイプ別スタイル設定")]
    [SerializeField]
    private ViewTypeStyle _defaultWindowStyle = ViewTypeStyle.CreateDefault(0.16f, 0.12f, 0.95f);

    [SerializeField]
    private ViewTypeStyle _modalDialogStyle = ViewTypeStyle.CreateDefault(0.20f, 0.14f, 0.90f);

    [SerializeField]
    private ViewTypeStyle _overlayDrawerStyle = ViewTypeStyle.CreateDefault(0.14f, 0.10f, 0.98f);

    [SerializeField]
    private ViewTypeStyle _loadingOverlayStyle = new ViewTypeStyle
    {
        ShowDuration = 0.12f,
        HideDuration = 0.12f,
        UseScalePop = false,
        StartScale = 1.0f,
        ShowSound = UISoundType.None,
        HideSound = UISoundType.None
    };

    public static UISettingsConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<UISettingsConfig>(ResourcePath);
                if (_instance == null)
                {
                    // Resources に未配置の場合でも NullReferenceException を防ぐため
                    // デフォルト値を持つインスタンスをインメモリで生成
                    _instance = CreateInstance<UISettingsConfig>();
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// ボタンタイプに応じたスタイルを取得
    /// </summary>
    public ButtonTypeStyle GetButtonStyle(UIButtonType type)
    {
        return type switch
        {
            UIButtonType.Cancel => _cancelButtonStyle,
            UIButtonType.SubAction => _subActionButtonStyle,
            _ => _defaultButtonStyle
        };
    }

    /// <summary>
    /// ビュータイプに応じたスタイルを取得
    /// </summary>
    public ViewTypeStyle GetViewStyle(UIViewType type)
    {
        return type switch
        {
            UIViewType.ModalDialog => _modalDialogStyle,
            UIViewType.OverlayDrawer => _overlayDrawerStyle,
            UIViewType.LoadingOverlay => _loadingOverlayStyle,
            _ => _defaultWindowStyle
        };
    }
}
