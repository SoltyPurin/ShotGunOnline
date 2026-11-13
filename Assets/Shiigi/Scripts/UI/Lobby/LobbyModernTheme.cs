using UnityEngine;
using UnityEngine.UI;

public enum LobbyButtonStyle
{
    Primary,    // メイン行動（ルーム作成・開始・参加・決定）
    Secondary,  // サブ行動（更新・詳細設定）
    Success,    // 準備完了（READY）
    Danger      // 離脱・キャンセル
}

/// <summary>
/// ShotGunOnline のロビーUIに現代的なダーク・タクティカルテーマと
/// 金属光沢ボタン演出（UIButtonMetallicVisual）を適用する共通ヘルパー。
/// </summary>
public static class LobbyModernTheme
{
    public static readonly Color PanelBackground = new Color(0.085f, 0.098f, 0.130f, 0.90f);
    public static readonly Color CardBackground = new Color(0.13f, 0.15f, 0.20f, 0.94f);
    public static readonly Color PanelBorder = new Color(0.91f, 0.66f, 0.22f, 0.32f);
    public static readonly Color SubtleBorder = new Color(1f, 1f, 1f, 0.12f);

    public static readonly Color AccentAmber = new Color(0.91f, 0.66f, 0.22f, 1f);
    public static readonly Color AccentCyan = new Color(0.28f, 0.78f, 0.92f, 1f);
    public static readonly Color AccentGreen = new Color(0.24f, 0.78f, 0.45f, 1f);
    public static readonly Color AccentRed = new Color(0.86f, 0.28f, 0.30f, 1f);

    public static readonly Color TextPrimary = new Color(0.95f, 0.96f, 0.98f, 1f);
    public static readonly Color TextMuted = new Color(0.65f, 0.70f, 0.78f, 1f);

    /// <summary>
    /// パネルの背景Imageにダーク半透明＋極細ボーダーを適用する
    /// </summary>
    public static void ApplyPanelStyle(GameObject root, bool isModal = false)
    {
        if (root == null) return;

        var bg = root.GetComponent<Image>();
        if (bg != null)
        {
            bg.color = PanelBackground;
        }

        var outline = root.GetComponent<Outline>();
        if (outline == null)
        {
            outline = root.AddComponent<Outline>();
        }
        outline.effectColor = isModal ? AccentAmber * new Color(1f, 1f, 1f, 0.55f) : PanelBorder;
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        outline.useGraphicAlpha = true;
    }

    /// <summary>
    /// カードUI（RoomCard / PlayerEntry）にモダンなダークカードスタイルを適用する
    /// </summary>
    public static void ApplyCardStyle(GameObject cardRoot)
    {
        if (cardRoot == null) return;

        var bg = cardRoot.GetComponent<Image>();
        if (bg != null)
        {
            bg.color = CardBackground;
        }

        var outline = cardRoot.GetComponent<Outline>();
        if (outline == null)
        {
            outline = cardRoot.AddComponent<Outline>();
        }
        outline.effectColor = SubtleBorder;
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = true;
    }

    /// <summary>
    /// ボタンに金属光沢ビジュアル（UIButtonMetallicVisual）のみを適用する（色や配置はエディタ設定を維持）
    /// </summary>
    public static UIButtonMetallicVisual AttachMetallicVisual(Button button)
    {
        if (button == null) return null;

        var visual = button.GetComponent<UIButtonMetallicVisual>();
        if (visual == null)
        {
            visual = button.gameObject.AddComponent<UIButtonMetallicVisual>();
        }
        return visual;
    }

    /// <summary>
    /// ボタンに金属光沢ビジュアル（UIButtonMetallicVisual）とテーマカラーを適用する
    /// </summary>
    public static UIButtonMetallicVisual ApplyButtonStyle(Button button, LobbyButtonStyle style = LobbyButtonStyle.Primary)
    {
        if (button == null) return null;

        // Unity標準のColorTint遷移がシェーダー/補間と競合しないようNoneにする
        button.transition = Selectable.Transition.None;

        var visual = button.GetComponent<UIButtonMetallicVisual>();
        if (visual == null)
        {
            visual = button.gameObject.AddComponent<UIButtonMetallicVisual>();
        }

        switch (style)
        {
            case LobbyButtonStyle.Primary:
                visual.ConfigureColors(
                    normalColor: new Color(0.18f, 0.21f, 0.28f, 0.96f),
                    focusedColor: new Color(0.27f, 0.31f, 0.40f, 1.00f),
                    accentColor: AccentAmber);
                break;

            case LobbyButtonStyle.Secondary:
                visual.ConfigureColors(
                    normalColor: new Color(0.14f, 0.17f, 0.23f, 0.94f),
                    focusedColor: new Color(0.22f, 0.26f, 0.35f, 1.00f),
                    accentColor: AccentCyan);
                break;

            case LobbyButtonStyle.Success:
                visual.ConfigureColors(
                    normalColor: new Color(0.13f, 0.25f, 0.19f, 0.96f),
                    focusedColor: new Color(0.19f, 0.37f, 0.27f, 1.00f),
                    accentColor: AccentGreen);
                break;

            case LobbyButtonStyle.Danger:
                visual.ConfigureColors(
                    normalColor: new Color(0.24f, 0.14f, 0.16f, 0.96f),
                    focusedColor: new Color(0.36f, 0.19f, 0.22f, 1.00f),
                    accentColor: AccentRed);
                break;
        }

        foreach (var txt in button.GetComponentsInChildren<Text>(true))
        {
            if (txt != null)
            {
                txt.color = TextPrimary;
            }
        }

        return visual;
    }
}
