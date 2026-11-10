using UnityEngine;

/// <summary>
/// UI用の効果音（SE）タイプ
/// </summary>
public enum UISoundType
{
    None,
    Enter,      // 決定・選択・ボタン押下
    Cancel,     // キャンセル・閉じる・戻る
    CursorMove  // フォーカス移動・ホバー
}

/// <summary>
/// シーン内の SEManager を通じてUI用の効果音を一元再生する静的ヘルパー
/// </summary>
public static class UISound
{
    private static SEManager _cachedSeManager;

    public static void Play(UISoundType soundType)
    {
        if (soundType == UISoundType.None) return;

        EnsureSeManager();
        if (_cachedSeManager == null) return;

        switch (soundType)
        {
            case UISoundType.Enter:
                _cachedSeManager.PlayEnterSound();
                break;
            case UISoundType.Cancel:
                _cachedSeManager.PlayCancelSound();
                break;
            case UISoundType.CursorMove:
                _cachedSeManager.PlayCursorMoveSound();
                break;
        }
    }

    private static void EnsureSeManager()
    {
        if (_cachedSeManager == null)
        {
            _cachedSeManager = Object.FindAnyObjectByType<SEManager>();
        }
    }
}
